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
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EmergeTk.Model
{
	public class RedisReadWritePair
	{
		public String ReadServer { get; set; }
		public String WriteServer { get; set; }
	}
	public class HashCachePoolConfiguration : List<RedisReadWritePair>
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(HashCachePoolConfiguration));
		public void ConstructFromFile(String xmlFile)
		{
			//log.DebugFormat("Constructing HashCachePoolConfiguration object from file {0}", xmlFile);
			XmlDocument doc = new XmlDocument();
			if (!File.Exists(xmlFile))
				throw new Exception(String.Format("Configuration file {0} for HashCachePool does not exist!", xmlFile));

			//log.DebugFormat("Configuring HashCachePool from config file found at {0}", xmlFile);
			doc.Load(xmlFile);
			this.ConstructFromXmlDoc(doc);
		}

		public void ConstructFromXmlDoc(XmlDocument doc)
		{
			XmlNodeList shardNodes = doc.SelectNodes("hashPoolConfig/shard");
			foreach (XmlNode shardNode in shardNodes)
			{
				RedisReadWritePair readWritePair = new RedisReadWritePair();
				readWritePair.ReadServer = shardNode.SelectSingleNode("reads").InnerText;
				readWritePair.WriteServer = shardNode.SelectSingleNode("writes").InnerText;
				this.Add(readWritePair);
				//log.DebugFormat("Found shard with readServer = {0}, writeServer = {1}", readWritePair.ReadServer, readWritePair.WriteServer);
			}
		}

		public HashCachePoolConfiguration(String pathToXmlFile)
		{
			ConstructFromFile(pathToXmlFile);
		}

		public HashCachePoolConfiguration()
		{
		}
	}

	public class HashCachePool
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(HashCachePool));
		private List<PooledRedisClientManager> redisPools = new List<PooledRedisClientManager>();

		public void Configure(HashCachePoolConfiguration config)
		{
			foreach (RedisReadWritePair pair in config)
			{
				redisPools.Add(new PooledRedisClientManager(new List<String> { pair.WriteServer }, new List<String> { pair.ReadServer }));
			}
		}
		public IRedisClient GetReadClient(String key)
		{
			return GetShard(key).GetReadOnlyClient();
		}
		public IRedisClient GetWriteClient(String key)
		{
			return GetShard(key).GetClient();
		}

		private PooledRedisClientManager GetShard(String key)
		{
			int hash = Math.Abs(key.GetHashCode());
			int index =  hash % redisPools.Count;
			return redisPools[index];
		}

		public void FlushAll()
		{
			foreach (PooledRedisClientManager redisPool in redisPools)
			{
				redisPool.FlushAll();
			}
		}
	}



	public class LocalCache
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(LocalCache));
		private Dictionary<RecordDefinition, AbstractRecord> localRecords = new Dictionary<RecordDefinition, AbstractRecord>();
		Dictionary<string, RecordDefinition> localRecordKeyMap = new Dictionary<string, RecordDefinition>();

		// the syntax for child property list key expressions is:
		// fullname:rowid:propertyName
		// e.g. "FiveToOne.Gallery.Rmx.Playlist:12345:Creatives"
		protected static Regex rgxProperty = new Regex(@"(?<recordDefSuffix>[\w\.]+:\d+):(?<property>\w+)", RegexOptions.Compiled);

		[NonSerialized]
		ReaderWriterLockSlim dictionaryLock = Locks.GetLockInstance(LockRecursionPolicy.NoRecursion); //setup the lock;

		public ReaderWriterLockSlim DictionaryLock
		{
			get
			{
				return dictionaryLock;
			}
		}

		public void PutLocal(string key, AbstractRecord value)
		{
			using (new WriteLock(this.DictionaryLock))
			{
				key = PrepareKey(key);
				localRecordKeyMap[key] = value.Definition;
				if (localRecords.ContainsKey (value.Definition) && !object.ReferenceEquals (value, localRecords[value.Definition]))
				{
					//expire the old guy.
					localRecords[value.Definition].MarkAsStale ();
				}
				localRecords[value.Definition] = value;
			}
		}

		static public string PrepareKey(string key)
		{
			//return HttpUtility.UrlEncode( key ).ToLower();
			return key.Replace(" ", "-").ToLower();
		}

		public AbstractRecord GetLocalRecord(RecordDefinition rd)
		{
			using (new ReadLock(this.DictionaryLock))
			{
				if (localRecords.ContainsKey (rd))
				{
					var rec = localRecords[rd];
					if (rec != null && !rec.IsStale)
						return rec;
				}
				return null;
			}
		}

		public AbstractRecord GetLocalRecord(string key)
		{
			key = PrepareKey(key);
			using (new ReadLock(this.DictionaryLock))
			{
				if (localRecordKeyMap.ContainsKey(key))
				{
					RecordDefinition rd = localRecordKeyMap[key];
					if (localRecords.ContainsKey (rd))
					{
						var rec = localRecords[rd];
						if (rec != null && !rec.IsStale)
							return rec;
					}
				}
			}
			return null;
		}

		public AbstractRecord GetLocalRecordFromPreparedKey(string keyPrepared)
		{
			using (new ReadLock(this.DictionaryLock))
			{
				if (localRecordKeyMap.ContainsKey(keyPrepared))
				{
					RecordDefinition rd = localRecordKeyMap[keyPrepared];
					if (localRecords.ContainsKey (rd))
					{
						var rec = localRecords[rd];
						if (rec != null && !rec.IsStale)
							return rec;
					}
				}
			}
			return null;
		}

		public void Remove(RecordDefinition def)
		{
			using (new WriteLock(this.DictionaryLock))
			{
				if (localRecords.ContainsKey (def))
				{
					localRecords[def].MarkAsStale();
					localRecords.Remove(def);
				}
			}
		}

		public void ClearFromLocalCache(string key)
		{
			if (key == null)
			{
				log.Error("Null key attempted to clear from cache.", System.Environment.StackTrace);
				return;
			}
			Match m = rgxProperty.Match(key);

			if (m.Success)
			{
				String propertyName = m.Groups["property"].Value;
				String recordDefString = "Record:" + m.Groups["recordDefSuffix"].Value;
				AbstractRecord record = GetLocalRecord(RecordDefinition.FromString(recordDefString));
				if (record != null)
				{
					using (new WriteLock(this.DictionaryLock))
					{
						record.UnsetProperty(propertyName, false);
					}
				}
			}
			else
			{

				key = LocalCache.PrepareKey(key);
				//log.Debug("clearing from cache " + key);
				using (new WriteLock(this.DictionaryLock))
				{
					if (localRecordKeyMap.ContainsKey(key))
					{
						RemoveRecordByDefinition(localRecordKeyMap[key]);
						localRecordKeyMap.Remove(key);
					}
					else if (key.StartsWith("record:"))
					{
						RemoveRecordByDefinition(RecordDefinition.FromString(key));
					}
				}
			}
		}

		// N.B. - do NOT call this function without being in a WriteLock.
		private void RemoveRecordByDefinition(RecordDefinition rd)
		{
			if (localRecords.ContainsKey(rd))
			{
				AbstractRecord r = localRecords[rd];
				if (null != r)
					r.MarkAsStale();
				localRecords.Remove(rd);
			}
		}

		public bool ContainsLocalRecord(RecordDefinition rd)
		{
			using (new ReadLock(this.DictionaryLock))
			{
				return localRecords.ContainsKey(rd);
			}
		}

		public void Flush()
		{
			using (new WriteLock(this.DictionaryLock))
			{
				localRecordKeyMap.Clear();
				localRecords.Clear();
			}
		}
	}
	
	public class RedisCacheClient : ICacheProvider
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(RedisCacheClient));

		static public HashCachePool hashCachePool = new HashCachePool();
		LocalCache localCache = new LocalCache();
	
		#region ICacheProvider implementation

		public AbstractRecord GetLocalRecord(string key)
		{
			return localCache.GetLocalRecord(key);
		}
		
		public AbstractRecord GetLocalRecord (RecordDefinition rd)
		{
			return localCache.GetLocalRecord (rd);
		}

		public void PutLocal(string key, AbstractRecord value)
		{
			localCache.PutLocal(key, value);
		}

		public bool Set (string key, AbstractRecord value)
		{
            StopWatch watch = new StopWatch("RedisCacheClient.Set_AbstractRecord", this.GetType().Name);
            watch.Start();

			key = LocalCache.PrepareKey(key);
			localCache.PutLocal(key, value);
			watch.Stop();

			if (value is ICacheLocalOnly)
				return true;

			if (value == null)
				throw new ArgumentException("Unable to index AbstractRecord: " + value);
			//log.Debug("Setting key for abstractrecord", key, value.OriginalValues );
			//log.DebugFormat ("Setting record : {0}, {1}", key, value); 
			MemoryStream s = new MemoryStream(100);
			ProtoSerializer.Serialize(value, s);
			try
			{
				using (IRedisClient rc = hashCachePool.GetWriteClient(key))
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
		
		
		public bool Set (string key, object value)
		{
			StopWatch watch = new StopWatch("RedisCacheClient.Set_Object", this.GetType().Name);
            watch.Start();

			key = LocalCache.PrepareKey(key);
			try
			{
				if (null != value)
				{
					MemoryStream s = new MemoryStream(100);
					BinaryFormatter bf = new BinaryFormatter();
					bf.Serialize(s, value);
					using (IRedisClient rc = hashCachePool.GetWriteClient(key))
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

			key = LocalCache.PrepareKey(key);
			//log.Debug("Setting key for string", key, value);
			try
			{
				if (null != value)
				{
					using (IRedisClient rc = hashCachePool.GetWriteClient(key))
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

				key = LocalCache.PrepareKey(key);
				using (IRedisClient rc = hashCachePool.GetWriteClient(key))
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

				key = LocalCache.PrepareKey(key);
				List<string> listItems = null;
				using (IRedisClient rc = hashCachePool.GetReadClient(key))
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
		
		public T GetRecord<T>(string key) where T : AbstractRecord, new()
		{
			key = LocalCache.PrepareKey(key);

			AbstractRecord record = localCache.GetLocalRecordFromPreparedKey(key);
			if (null != record || typeof(T).GetInterface("IRedisLocalOnly") != null)
				return record as T;

			object o = null;
			try
			{
				//log.Debug("GetRecord<T> key ", key);
				using (IRedisClient rc = hashCachePool.GetReadClient(key))
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
			if (!(o is byte[]))
			{
				log.Error("Expecting value to be byte[] but got " + o);
				throw new Exception("Expecting value to be byte[]");
			}
			byte[] bytes = (byte[])o;
			MemoryStream s = new MemoryStream(bytes);
			T t = ProtoSerializer.Deserialize<T>(s);
			localCache.PutLocal(key, t);
			return t;
		}

		public AbstractRecord GetRecord(Type t, string key)
		{
			key = LocalCache.PrepareKey(key);
			AbstractRecord record = localCache.GetLocalRecordFromPreparedKey(key);
			if (null != record || t.GetInterface("IRedisLocalOnly") != null)
				return record;

			object o = null;
			try
			{
				//log.Debug("GetRecord key ", key);
				using (IRedisClient rc = hashCachePool.GetReadClient(key))
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
			if (!(o is byte[]))
			{
				log.Error("Expecting value to be byte[] but got " + o);
				throw new Exception("Expecting value to be byte[]");
			}
			byte[] bytes = (byte[])o;
			MemoryStream s = new MemoryStream(bytes);
			AbstractRecord r = ProtoSerializer.Deserialize(t, s);
			localCache.PutLocal(key, r);
			return r;
		}
		
		public object GetObject (string key)
		{
			//log.DebugFormat("key '{0}' in local cache? " + localCache.ContainsKey(key), key );
			key = LocalCache.PrepareKey(key);
            StopWatch watch = new StopWatch("RedisCacheClient.Get", this.GetType().Name);
            watch.Start();
			object o = null;
			try
			{
				//log.Debug("GetObject key ", key);
				byte[] bytes = null;
				using (IRedisClient rc = hashCachePool.GetReadClient(key))
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
			key = LocalCache.PrepareKey(key);
			string o = null;
			try
			{
				//log.Debug("GetString key ", key);
				using (IRedisClient rc = hashCachePool.GetReadClient(key))
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
			List<object> listObjects = new List<object>();

			for (int i = 0; i < keys.Length; i++)
			{
				//keys[i] = prepareKey(keys[i]);
				listObjects.Add(GetObject(keys[i]));
			}
			return listObjects.ToArray();

#if false
			// (Don Mar.13,2011) How can we re-write this function to take advantage of the RedisClient.GetByIds() functionality
			// while still preserving usage of hashed pool?  Perhaps we segregate the keys by hash client cluster,
			// make the calls, then rejoin the results?   Is that cheaper then calling GetObject() over and over?   
			// probably.  However, no one is calling this function currently; it's just here because it's a member
  			// of ICacheProvider.   Talk to Ben, and see if we can remove this member.
			try
			{
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
#endif
		}

		public void Remove(string originalKey)
		{
			var key = LocalCache.PrepareKey(originalKey);
			StopWatch watch = new StopWatch("RedisCacheClient.Remove", this.GetType().Name);
			watch.Start();
			try
			{
				using (IRedisClient rc = hashCachePool.GetWriteClient(key))
				{
					rc.Remove(key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Remove error. key={0}. Exception={1}", key, ex);
				return;
			}
			//now send out the expiration notice
			SetExpirationEvent(originalKey);
			//done.
			watch.Stop();
		}
		
		private List<string> localKeys = new List<string> ();
		private object localKeyLock = new object ();
		private void SetExpirationEvent (string key)
		{
			try
			{
				//log.Debug("SetExpirationEvent for key: " + key);
				using (IRedisClient rc = hashCachePool.GetWriteClient("EXPIRE_KEY"))
				{
					lock (localKeyLock)
					{
						localKeys.Add (key);
					}
					rc.PublishMessage ("EXPIRE_KEY", key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("SetExpirationEvent error. key={0}. Exception={1}", key, ex);
			}
		}

		public void Remove(AbstractRecord record)
		{
			log.Debug ("Removing ", record);
			localCache.Remove(record.Definition);
			if (! (record is ICacheLocalOnly))
				RemoveRemote (record);
		}
		
		private void RemoveRemote (AbstractRecord record)
		{
			try
			{
				String key = record.CreateStandardCacheKey();
				using (IRedisClient rc = hashCachePool.GetWriteClient(key))
				{
					rc.Remove(key);
				}
			}
			catch (Exception ex)
			{
				log.ErrorFormat("Remove error. key={0}. Exception={1}", record.CreateStandardCacheKey(), ex);
			}
			
			record.InvalidateCache();
			SetExpirationEvent(record.Definition.ToString());
		}
		
		//This function needs to ensure that no stale references to the record exist anywhere in the cache service.
		public void Update (AbstractRecord record)
		{
			var oldrec = localCache.GetLocalRecord (record.Definition);
			var equals = object.ReferenceEquals (record, oldrec);
			//log.Debug ("Updating ", record, oldrec, equals);
			//when updating, if the copy in the local cache is ref equal to the current record being udpated, do not remove it, or mark it as stale.
			if (! equals)
			{
				localCache.Remove(record.Definition);
				localCache.PutLocal (record.CreateStandardCacheKey (), record);
			}
			if (! (record is ICacheLocalOnly))
				RemoveRemote (record);			
		}	

		public void FlushAll()
		{
			try
			{
				//TODO: need to send flush event out through system.
				localCache.Flush();
				hashCachePool.FlushAll();
			}
			catch (Exception ex)
			{
				log.ErrorFormat("FlushAll error. Exception={0}", ex);
			}
		}
		#endregion
		
		public RedisCacheClient()
		{
			Initialize(true);
		}

		public RedisCacheClient(bool startExpirationThread)
		{
			Initialize(startExpirationThread);
		}

		static RedisCacheClient()
		{
			String redisConfig = ConfigurationManager.AppSettings["RedisConfig"];
			if (String.IsNullOrEmpty(redisConfig))
				throw new Exception("no RedisConfig key in App.Config, cannot continue");

			log.InfoFormat("INITIALIZING CACHE CLIENT: Configuring HashedPoolManager from config at {0} ", redisConfig);
			hashCachePool.Configure(new HashCachePoolConfiguration(redisConfig));
		}

		public void Initialize(bool startExpirationThread)
		{
			//log.DebugFormat("RedisCacheClient ctor called with startExpirationThread = {0}", startExpirationThread ? "true" : "false");
			try
			{
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
			}
		}
		
		/// <summary>
		/// Try to use redis' pub/sub feature for this.
		/// </summary>
		void CheckForExpirationEvents()
		{
			try
			{
				var rc = hashCachePool.GetWriteClient("EXPIRE_KEY");
				var subscription = rc.CreateSubscription ();
				
				subscription.OnMessage += (channel, message) =>
				{
					switch (channel)
					{
					case "EXPIRE_KEY":
						bool keyIsNotLocal = true;
						lock (localKeyLock)
						{
							if (localKeys.Contains (message))
							{
								localKeys.Remove (message);
								keyIsNotLocal = false;
							}
						}
						if (keyIsNotLocal)					
							localCache.ClearFromLocalCache(message);
						break; 
					}
				};
	
				subscription.SubscribeToChannels ("EXPIRE_KEY");
			}
			catch (Exception e)
			{
				log.Error ("Error checking for expiration events.", e);
			}
		}
	}
}
