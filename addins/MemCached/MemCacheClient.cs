// MemCacheClient.cs created with MonoDevelop
// User: ben at 10:27 AM 12/24/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using BeIT.MemCached;
using System.Web;

namespace EmergeTk.Model
{
	public class MemCacheClient : ICacheProvider
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(MemCacheClient));
		//public static MemCacheClient Instance = new MemCacheClient();
		
		static string cacheName = "EmergeTkCache";
		static string[] servers;
		static MemcachedClient client;
	
		static MemCacheClient()
		{			
		
			servers = Setting.GetValueT<string>("MemcachedServers", "localhost").Split(',');
			log.Info("MemCache Servers",servers);
			MemcachedClient.Setup(cacheName, servers );
			//log.Debug("Getting client instance");
			client = MemcachedClient.GetInstance(cacheName);
			//log.Debug("Got client instance",client);
		}
		
		private string prepareKey(string key)
		{
			return HttpUtility.UrlEncode( key ).ToLower();
		}
		
		public void FlushAll()
		{
			log.Debug("Flushing all items from memcached.");
			client.FlushAll();
		}

		public MemCacheClient()
		{
		}
		
		/// <summary>
		/// Set a record in the cache
		/// </summary>
		/// <param name="key">
		/// A <see cref="System.String"/> case insensitive key.
		/// </param>
		/// <param name="value">
		/// A <see cref="AbstractRecord"/>
		/// </param>
		public bool Set( string key, AbstractRecord value )
		{
			//log.Debug("setting key 1 ", prepareKey(key), value );
			if( value == null || value.OriginalSource == null )
				throw new ArgumentException("Unable to index AbstractRecord: " + value );
			return Set( key, value.OriginalSource.Table );
		}
		
		/// <summary>
		/// Set an object in the cache.
		/// </summary>
		/// <param name="key">
		/// A <see cref="System.String"/> case insensitive key.
		/// </param>
		/// <param name="value">
		/// A <see cref="System.Object"/>
		/// </param>
		public bool Set( string key, object value )
		{
			//log.Debug("setting key 2 ", prepareKey(key), value );
			return client.Set( prepareKey(key), value );
		}

		/// <summary>
		/// Retrieve an object from the cache.
		/// </summary>
		/// <param name="key">
		/// A <see cref="System.String"/> case insensitive key.
		/// </param>
		/// <returns>
		/// A <see cref="System.Object"/>
		/// </returns>
		public object Get( string key )
		{
			//log.Debug("requesting key ", prepareKey(key) );
			if( client == null )
				return null;
			object retVal = client.Get(prepareKey(key));
			//log.Debug("found ", retVal );
			return retVal;
		}

		public object[] Get(params string[] keys )
		{
			List<string> encKeys = new List<string>();
			foreach( string k in keys )
				encKeys.Add( prepareKey(k) );
			return client.Get(encKeys.ToArray());
		}

		public void Remove(string key)
		{
			client.Delete(prepareKey(key));
		}
	}
}
