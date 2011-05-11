using System;
using System.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Web;
using EmergeTk;
using EmergeTk.Model;
using ServiceStack.Client;
using ServiceStack.Redis;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ProtobufSerializer;
using System.IO;
using System.Data;
using System.Text;

namespace EmergeTk.Model
{
	
	public class RedisCacheClient : ICacheProvider
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(RedisCacheClient));
		protected static string redisHost = ConfigurationManager.AppSettings["RedisServer"] ?? "localhost";
		protected static string redisPort = ConfigurationManager.AppSettings["RedisPort"] ?? "6379";

		PooledRedisClientManager redisPool = null;
		
		private string prepareKey(string key)
		{
			//return HttpUtility.UrlEncode( key ).ToLower();
			return key.Replace(" ", "-").ToLower();
		}	

		Dictionary<RecordDefinition,AbstractRecord> localRecords = new Dictionary<RecordDefinition, AbstractRecord>();
		Dictionary<string, RecordDefinition> localRecordKeyMap = new Dictionary<string, RecordDefinition>();
	
		#region ICacheProvider implementation
		public bool Set (string key, AbstractRecord value)
		{
            StopWatch watch = new StopWatch("RedisCacheClient.Set_AbstractRecord", this.GetType().Name);
            watch.Start();
			
			key = prepareKey(key);
			
			PutLocal (key, value);
            watch.Stop();
			
			if( value == null )
				throw new ArgumentException("Unable to index AbstractRecord: " + value );
			//log.Debug("Setting key for abstractrecord", key, value.OriginalValues );
			//log.DebugFormat ("Setting record : {0}, {1}", key, value); 
			MemoryStream s = new MemoryStream(100);
			ProtoSerializer.Serialize(value, s);
			try
			{
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					rc.Set<byte[]>(key, s.ToArray());
				}
				watch.Stop();
				return true;
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Set error. key={0}. Exception={1}", key, ex);
			}
			return false;
		}
		
		public void PutLocal ( string key, AbstractRecord value)
		{
			key = prepareKey(key);
			localRecordKeyMap[key] = value.Definition;
			localRecords[value.Definition] = value;
		}
		
		public bool Set (string key, object value)
		{
			StopWatch watch = new StopWatch("RedisCacheClient.Set_Object", this.GetType().Name);
            watch.Start();
			key = prepareKey(key);
			//log.Debug("Setting key for object", key, value );
			//localCache[key] = value;
			try
			{
				if (null != value)
				{
					CheckRedisClient();
					MemoryStream s = new MemoryStream(100);
					BinaryFormatter bf = new BinaryFormatter();
					bf.Serialize(s, value);
					using (IRedisClient rc = redisPool.GetClient())
					{
						rc.Set<byte[]>(key, s.ToArray());
					}
				}
				watch.Stop();
				return true;
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Set error. key={0}. Exception={1}", key, ex);
			}
			return false;
		}

		public bool Set(string key, string value)
		{
			StopWatch watch = new StopWatch("RedisCacheClient.Set_String", this.GetType().Name);
			watch.Start();
			key = prepareKey(key);
			//log.Debug("Setting key for string", key, value);
			//localCache[key] = value;
			try
			{
				if (null != value)
				{
					CheckRedisClient();
					using (IRedisClient rc = redisPool.GetClient())
					{
						rc.Set<string>(key, value);
					}
				}
				watch.Stop();
				return true;
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Set error. key={0}. Exception={1}", key, ex);
			}
			return false;
		}

		private static byte[] nullByte = { 0 };
		
		public void AppendStringList(string key, string value)
		{
			try
			{
				//log.Debug("Appending StringList key ", key);
				CheckRedisClient();
				key = prepareKey(key);
				using (IRedisClient rc = redisPool.GetClient())
				{
					rc.AddItemToList(key, value);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("AppendStringList error. key={0}. Exception={1}", key, ex);
			}
		}
		
		public string[] GetStringList(string key)
		{
			string[] items = null;
			try
			{
				//log.Debug("GetStringList key ", key);
				CheckRedisClient();
				key = prepareKey(key);
				List<string> listItems = null;
				using (IRedisClient rc = redisPool.GetClient())
				{
					listItems = rc.GetAllItemsFromList(key);
				}
				if (listItems != null && listItems.Count > 0)
				{
					items = listItems.ToArray();
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("GetStringList error. key={0}. Exception={1}", key, ex);
			}
			return items;
		}
		
		public AbstractRecord GetLocalRecord(RecordDefinition rd)
		{
			return localRecords.ContainsKey(rd) ? localRecords[rd] : null;
		}
		
		public AbstractRecord GetLocalRecord(string key)
		{
            key = prepareKey(key);
			if( localRecordKeyMap.ContainsKey(key) )
			{
				RecordDefinition rd = localRecordKeyMap[key];
				if( localRecords.ContainsKey( rd ) )
					return localRecords[rd];
			}
			return null;
		}

        private AbstractRecord GetLocalRecordFromPreparedKey(string keyPrepared)
        {
            if (localRecordKeyMap.ContainsKey(keyPrepared))
            {
                RecordDefinition rd = localRecordKeyMap[keyPrepared];
                if (localRecords.ContainsKey(rd))
                    return localRecords[rd];
            }
            return null;
		}

		
		public T GetRecord<T>(string key) where T : AbstractRecord, new()
		{
			key = prepareKey(key);
            AbstractRecord record = GetLocalRecordFromPreparedKey(key);
			if( null != record )
				return record as T;
			object o = null;
			try
			{
				//log.Debug("GetRecord<T> key ", key);
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					o = rc.Get<byte[]>(key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("GetRecord error. key={0}. Exception={1}", key, ex);
			}
			if (o == null)
				return null;
			if( !( o is byte[] ))
			{
				log.Error("Expecting value to be byte[] but got " + o);	
				throw new Exception("Expecting value to be byte[]");
			}
			byte[] bytes = (byte[])o;
			MemoryStream s = new MemoryStream(bytes);
			T t = ProtoSerializer.Deserialize<T>(s);
			PutLocal(key,t);
			return t;
		}	
		
		public AbstractRecord GetRecord(Type t, string key)
		{
			key = prepareKey(key);
            AbstractRecord record = GetLocalRecordFromPreparedKey(key);
			if( null != record )
				return record;
			object o = null;
			try
			{
				//log.Debug("GetRecord key ", key);
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					o = rc.Get<byte[]>(key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("GetRecord error. key={0}. Exception={1}", key, ex);
			}
			if (o == null)
				return null;
			if( !( o is byte[] ))
			{
				log.Error("Expecting value to be byte[] but got " + o);	
				throw new Exception("Expecting value to be byte[]");
			}
			byte[] bytes = (byte[])o;
			MemoryStream s = new MemoryStream(bytes);
			AbstractRecord r = ProtoSerializer.Deserialize(t,s);
			PutLocal(key,r);
			return r;
		}
		
		public object GetObject (string key)
		{
			//log.DebugFormat("key '{0}' in local cache? " + localCache.ContainsKey(key), key );
			key = prepareKey(key);
			//if( localCache.ContainsKey(key) )
			//	return localCache[key];
            StopWatch watch = new StopWatch("RedisCacheClient.Get", this.GetType().Name);
            watch.Start();
			object o = null;
			try
			{
				//log.Debug("GetObject key ", key);
				byte[] bytes = null;
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					bytes = rc.Get<byte[]>(key);
				}
				if (null != bytes && bytes.Length > 0)
				{
					MemoryStream s = new MemoryStream(bytes);
					BinaryFormatter bf = new BinaryFormatter();
					o = bf.Deserialize(s);
				}
			}
			catch (System.NullReferenceException)
			{
				return null;
			}
			catch (System.Runtime.Serialization.SerializationException)
			{
				return null;
			}
			catch (Exception ex)
			{
				log.ErrorFormat("GetObject error. key={0}. Exception={1}", key, ex);
			}
			finally
			{
				watch.Stop();
			}
			//log.Debug("Getting key", key, o );
			return o;
		}

		public string GetString(string key)
		{
			key = prepareKey(key);
			string o = null;
			try
			{
				//log.Debug("GetString key ", key);
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					o = rc.Get<string>(key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("GetString error. key={0}. Exception={1}", key, ex);
			}
			return o;
		}

		public object[] GetList(params string[] keys)
		{
            StopWatch watch = new StopWatch("RedisCacheClient.GetList", this.GetType().Name);
            watch.Start();

			for(int i = 0; i < keys.Length; i++ )
				keys[i] = prepareKey(keys[i]);
			try
			{
				CheckRedisClient();
				IList<object> values = null;
				using (IRedisClient rc = redisPool.GetClient())
				{
					values = rc.GetByIds<object>(keys);
				}
				watch.Stop();
				return values != null ? values.ToArray() : null;
			}
			catch (Exception ex)
			{
				log.Error("GetList error.", ex);
				return null;
			}
		}
		
		public void Remove (string key)
		{
			key = prepareKey(key);
            StopWatch watch = new StopWatch("RedisCacheClient.Remove", this.GetType().Name);
            watch.Start();
			try
			{
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					rc.Remove(key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Remove error. key={0}. Exception={1}", key, ex);
				return;
			}
			ClearFromLocalCache(key);
			//now send out the expiration notice
			SetExpirationEvent (key);
			//done.
            watch.Stop();
		}
		
		private void SetExpirationEvent (string key)
		{
			try
			{
				CheckRedisClient();
				//log.Debug("SetExpirationEvent for key: " + key);
				using (IRedisClient rc = redisPool.GetClient())
				{
					rc.PublishMessage ("EXPIRE_KEY", key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("SetExpirationEvent error. key={0}. Exception={1}", key, ex);
			}
		}
		
		public void Remove(AbstractRecord record, bool remoteOnly)
		{
			if (!remoteOnly)
			{
	            if (localRecords.Contains(new KeyValuePair<RecordDefinition, AbstractRecord>(record.Definition, record)))
	            {
	                localRecords[record.Definition].MarkAsStale();
	                localRecords.Remove(record.Definition);
	                record.MarkAsStale();
	            }
			}
			try
			{
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					rc.Remove(record.CreateStandardCacheKey());
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Remove error. key={0}. Exception={1}", record.CreateStandardCacheKey(), ex);
			}
			record.InvalidateCache();
			SetExpirationEvent(record.Definition.ToString());
		}
		
		private void ClearFromLocalCache (string key)
		{
			if( key == null )
			{
				log.Error("Null key attempted to clear from cache.", System.Environment.StackTrace);				
				return;
			}
			key = prepareKey(key);
			//log.Debug("clearing from cache " + key);
			if( localRecordKeyMap.ContainsKey(key) )
			{
				RemoveRecordByDefinition(localRecordKeyMap[key]);
				localRecordKeyMap.Remove(key);
			}
			else if( key.StartsWith("record:") )
			{
				RemoveRecordByDefinition(RecordDefinition.FromString(key));
			}
		}
		
		private void RemoveRecordByDefinition (RecordDefinition rd)
		{
			if( localRecords.ContainsKey(rd) )
			{
				AbstractRecord r = localRecords[rd];
				if( null != r )
					r.MarkAsStale();
				localRecords.Remove(rd);
			}
		}
		
		public bool ContainsLocalRecord( RecordDefinition rd )
		{
			return localRecords.ContainsKey(rd);
		}
		
		public void FlushAll ()
		{
			try
			{
				//TODO: need to send flush event out through system.
				localRecordKeyMap.Clear();
				localRecords.Clear();
				CheckRedisClient();
				using (IRedisClient rc = redisPool.GetClient())
				{
					rc.FlushAll();
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("FlushAll error. Exception={0}", ex);
			}
		}
		#endregion
		
		long lastModPos = 0;
		public PooledRedisClientManager RedisClientPool {
			get {
				return redisPool;
			}
		}

		private void CheckRedisClient()
		{
			if (null == redisPool)
			{
				throw new ApplicationException("RedisPool is NULL. Check the Redis Server for availability.");
			}
		}

		private void ConnectRedisClientToServer()
		{
			try
			{
				redisPool = new PooledRedisClientManager(new string[] { redisHost + ":" + redisPort });
			}
			catch (Exception e)
			{
				log.Warn("ConnectRedisClientToServer:", e);
				redisPool = null;
			}
		}
		
		public RedisCacheClient()
		{
			Initialize(true);
		}

		public RedisCacheClient(bool startExpirationThread)
		{
			Initialize(startExpirationThread);
		}

		public void Initialize(bool startExpirationThread)
		{
			try
			{
				log.InfoFormat("INITIALIZING CACHE CLIENT: Connecting redis client to {0}:{1}", redisHost, redisPort);
				ConnectRedisClientToServer();

				if (startExpirationThread)
				{
					log.Info("Starting background thread to check for Cache Expiration Events");
					ThreadStart job = new ThreadStart(CheckForExpirationEvents);
					Thread thread = new Thread(job);
					thread.Start();
				}
			}
			catch (Exception e)
			{
				log.Error("RedisCacheClient Initialize:", e);
				redisPool = null;
			}
		}
		
		/// <summary>
		/// Try to use redis' pub/sub feature for this.
		/// </summary>
		void CheckForExpirationEvents()
		{
			var rc = redisPool.GetClient ();
			var subscription = rc.CreateSubscription ();
			
			subscription.OnMessage += (channel, message) =>
			{
				switch (channel)
				{
				case "EXPIRE_KEY":
					ClearFromLocalCache(message);
					break; 
				}
			};
			
			subscription.SubscribeToChannels ("EXPIRE_KEY");			
		}
	}
}
