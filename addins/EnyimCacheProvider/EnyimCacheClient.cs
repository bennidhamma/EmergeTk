using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Enyim.Caching.Configuration;
using EmergeTk;
using EmergeTk.Model;
using System.Linq;
using System.Threading;
using ProtobufSerializer;
using System.IO;
using System.Data;
using System.Text;

namespace EmergeTk.Model
{
	
	public class EnyimCacheClient : ICacheProvider
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(EnyimCacheClient));
	
		const string ModificationPositionKey = "mod-pos";
		const string ModificationPrefix = "mod-";
		MemcachedClient mc;
		
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
            StopWatch watch = new StopWatch("EnyimCacheClient.Set", this.GetType().Name);
            watch.Start();
			
			key = prepareKey(key);
			
			PutLocal (key, value);
            watch.Stop();
			
			if( value == null )
				throw new ArgumentException("Unable to index AbstractRecord: " + value );
			//log.Debug("Setting key", key, value.OriginalSource.Table );
			MemoryStream s = new MemoryStream(100);
			ProtoSerializer.Serialize(value, s);
			bool ret = mc.Store(StoreMode.Set,key,s.ToArray(),0,(int)s.Length,new TimeSpan(24,0,0));
			//bool ret = mc.Store(StoreMode.Set,key,value.OriginalSource.Table,new TimeSpan(24,0,0) );
            watch.Stop();
            return ret;
		}
		
		public void PutLocal ( string key, AbstractRecord value)
		{
			key = prepareKey(key);
			localRecordKeyMap[key] = value.Definition;
			localRecords[value.Definition] = value;
		}
		
		public bool Set (string key, object value)
		{
			StopWatch watch = new StopWatch("EnyimCacheClient.Set", this.GetType().Name);
            watch.Start();
			key = prepareKey(key);
			//log.Debug("Setting key", key, value );
			//localCache[key] = value;
			//TODO: should not have hard coded exp. time
			bool ret = mc.Store(StoreMode.Set,key,value, new TimeSpan(24,0,0) );
            watch.Stop();
            return ret;
		}
		
		private static byte[] nullByte = {0};
		
		public void AppendStringList(string key, string value)
		{
			key = prepareKey(key);
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
			bytes = bytes.Concat(nullByte).ToArray();
			if( ! mc.Append(key, bytes) )
			{
				mc.Store(StoreMode.Add,key,new byte[]{});
				mc.Append(key, bytes);
			}
		}
		
		public string[] GetStringList(string key)
		{
			key = prepareKey(key);
			byte[] bytes = mc.Get<byte[]>(key); //a null seperated set of UTF8 encoded values
			if( bytes == null )
				return null;
			
			int start = 0; //start of current string
			int length = 0; //length of current string
			string[] items = null; //return array;
			int itemCount = 0; //number of items to allocate in return array
			int itemPos = 0; //pos of next item to add to return array
			
			//iterate to get a total item count
			foreach( byte b in bytes )
			{
				if( b == 0 )
					itemCount++;
			}
			//allocate return array
			items = new string[itemCount];
			//accumulate
			foreach( byte b in bytes )
			{
				//check for NULL seperator
				if( b > 0 )
				{
					length++;
				}
				else
				{
					//ensure we have accumulated enough bytes to create a string
					if( length > 0 )
					{
						//encode current value into string and add into return list
						items[itemPos++] = Encoding.UTF8.GetString( bytes, start, length );
						//reset start and length
						start += length + 1; //next string starts at current start + length + 1 for null byte.
						length = 0;
					}
				}
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
			if( mc == null )
				throw new Exception("mc is null");
			object o = mc.Get(key);
			if( o == null )
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
			object o = mc.Get(key);
			if( o == null )
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
            StopWatch watch = new StopWatch("EnyimCacheClient.Get", this.GetType().Name);
            watch.Start();
			object o = null;
			try
			{
				o = mc.Get(key);
			}
			catch(System.NullReferenceException)
			{
				return null;
			}
            finally
            {
				watch.Stop();
			}
			//log.Debug("Getting key", key, o );
			return o;
		}
		
		public object[] GetList (params string[] keys)
		{
            StopWatch watch = new StopWatch("EnyimCacheClient.Get", this.GetType().Name);
            watch.Start();

			for(int i = 0; i < keys.Length; i++ )
				keys[i] = prepareKey(keys[i]);
			IDictionary<string,object> result = mc.Get(keys);
			List<object> values = new List<object>();
			foreach( string key in keys )
			{
				values.Add( result[key] );
			}
            watch.Stop();
			return values.ToArray();
		}
		
		public void Remove (string key)
		{
			key = prepareKey(key);
            StopWatch watch = new StopWatch("EnyimCacheClient.Remove", this.GetType().Name);
            watch.Start();
			mc.Remove(key);
			ClearFromLocalCache (key);
			//now send out the expiration notice
			SetExpirationEvent (key);
			//done.
            watch.Stop();
		}
		
		private void SetExpirationEvent (string key)
		{
			long modPosition = mc.Increment(ModificationPositionKey,1);
			ownedExpirationEvents.Add( modPosition );
			Set(BuildModificationEntryKey(modPosition), key);
		}
		
		public void Remove(AbstractRecord record)
		{
            if (localRecords.Contains(new KeyValuePair<RecordDefinition, AbstractRecord>(record.Definition, record)))
            {
                localRecords[record.Definition].MarkAsStale();
                localRecords.Remove(record.Definition);
            }
			mc.Remove(record.CreateStandardCacheKey());
			record.MarkAsStale();
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
		
		private string BuildModificationEntryKey(long pos)
		{
			return ModificationPrefix + pos;
		}
		
		public bool ContainsLocalRecord( RecordDefinition rd )
		{
			return localRecords.ContainsKey(rd);
		}
		
		public void FlushAll ()
		{
			//TODO: need to send flush event out through system.
			localRecordKeyMap.Clear();
			localRecords.Clear();
			mc.FlushAll();
		}
		#endregion
		
		long lastModPos = 0;
		public MemcachedClient MemcacheClient {
			get {
				
				return mc;
			}
		}
		
		public EnyimCacheClient()
		{
			try
			{
				mc = new MemcachedClient();
				object lastModObj = mc.Get(ModificationPositionKey);
				
				//log.Debug("last mod obj: " + lastModObj );
				if( lastModObj == null )
				{
					Set(ModificationPositionKey, "0");
					lastModPos = 0;
				}
				else
				{
					//log.Debug("lastmodobj type:" + lastModObj.GetType());
					lastModPos = Convert.ToInt64(lastModObj);
				}

			   	ThreadStart job = new ThreadStart(CheckForExpirationEvents);
			    Thread thread = new Thread(job);
			    thread.Start();		
			}
			catch(Exception e )
			{
				log.Error("EnyimCacheClient ctor:", e );
			}
		}
		
		//track expiration events that originate from this node.
		List<long> ownedExpirationEvents = new List<long>();
		
		/// <summary>
		/// Checks a memcached counter to determine the current log position of expiration events.
		/// Then requests each individual message and flushes out local cache accordingly.
		/// 
		/// Runs in a timer every 10 seconds, only making method public for unit tests.
		/// </summary>
		//public void CheckForExpirationEvents ()
		void CheckForExpirationEvents()
		{
			while (true)
			{
				try
				{
					//get the next log position:
				    //log.Debug("looking for new cache entries");
					long nextLogPos = Convert.ToInt64(mc.Get(ModificationPositionKey));
					//log.DebugFormat("found {0} modified entries", nextLogPos - lastModPos);
					for(long i = lastModPos + 1; i <= nextLogPos; i++ )
					{
						if( ownedExpirationEvents.BinarySearch(i) < 0 )
						{
							string key = (string)mc.Get(BuildModificationEntryKey(i));
							if( key == null )
							{
								log.WarnFormat("Expiration event at position {0} is null. ", i);
							}
							ClearFromLocalCache(key);
						}
					}
					lastModPos = nextLogPos;					
				}
				catch(Exception e)
				{
					log.Error(e);	
				}
				
				Thread.Sleep(10000);
			}
		}
	}
}
