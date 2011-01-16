using System;
using System.Collections.Generic;
using System.Web.Caching;
using System.Web;
using System.Collections;


namespace EmergeTk.Model
{	
	internal class CacheManager : ICacheProvider
	{		
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(CacheManager));
		Cache cache;
		
		private Cache Cache
		{
			get 
			{
				if( cache == null )
					cache = System.Web.HttpRuntime.Cache;
				return cache;
			}
			set
			{
				cache = value;
			}
		}
		
		private CacheManager()
		{
			try
			{
				cache = System.Web.HttpRuntime.Cache;
			}
			catch (Exception e)
			{
				log.Error("Error initializing Cache",e);
			}	
		}
		
		#region ICacheProvider implementation 
		
		public bool Set( string key, AbstractRecord r )
		{
			Set( key, r as object);
			return true;
		}
		
		public bool Set (string key, object value)
		{
			//TODO: if we are running in single server mode, why don't we cache for much longer than 30 minutes?
			//if we are running in multi server, and using local cache, it should be much less - that or we should check for
			//Not Modified status.
			int cacheTime = 30;
			Cache.Add(key, value, null, DateTime.Now.AddMinutes(cacheTime),new TimeSpan(0),CacheItemPriority.Normal,null);
			return true;
		}
		
		public void AppendStringList(string key, string value)
		{
			if( Cache[key] == null )
			{
				Cache[key] = new List<string>();
			}
			(Cache[key] as List<string>).Add(value);
		}
		
		public string[] GetStringList(string key)
		{
			List<string> items = Cache[key] as List<string>;
			if( null != items )
			{
				return items.ToArray();
			}
			return null;
		}
		
		public bool IsMostUpToDate (AbstractRecord record)
		{
			throw new NotImplementedException ();
		}
		
		public AbstractRecord GetLocalRecord(string key)
		{
			return Cache.Get(key) as AbstractRecord;
		}
		
		public AbstractRecord GetLocalRecord(RecordDefinition def)
		{
			throw new NotImplementedException ();
		}
		
		public object GetObject (string key)
		{
			return Cache.Get(key);
		}
		
		public T GetRecord<T>(string key) where T : AbstractRecord, new()
		{
			return Cache.Get(key) as T;	
		}
		
		public AbstractRecord GetRecord(Type t, string key)
		{
			return Cache.Get(key) as AbstractRecord;
		}
		
		public object[] GetList (params string[] keys)
		{
			List<object> results = new List<object>();
			foreach( string k in keys )
			{
				results.Add( Cache.Get( k ) );
			}
			return results.ToArray();
		}
		
		public void Remove (string key)
		{
			if(Cache[key] != null)
				Cache.Remove(key);
		}
		
		public void Remove(AbstractRecord record)
		{
			record.InvalidateCache();
		}
		
		public void FlushAll()
		{			
			foreach(DictionaryEntry entry in Cache) {
          		Cache.Remove((string)entry.Key);
	      	}
		}
		
		public void PutLocal ( string key, AbstractRecord value)
		{
			Set(key,value);
		}
		
		#endregion 
				
		static CacheManager manager = new CacheManager();
		
		public static CacheManager Manager {
			get {
				return manager;
			}
		}

		
	}
}
