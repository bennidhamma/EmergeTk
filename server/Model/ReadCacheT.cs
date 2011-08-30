using System;
using System.Collections.Generic;
using System.Threading;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Model
{
	
	
	public class ReadCache<T> where T : AbstractRecord, IVersioned, new()
	{
		//static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ReadCache<T>));
		private object thisLock = new object();
		static string modelName;
		
		static ReadCache()
		{
			T t = new T();
			modelName = t.DbSafeModelName;
		}
		
		Dictionary<int, T> map = new Dictionary<int, T>();
		Dictionary<int, DateTime> ages = new Dictionary<int, DateTime>();
		
		//in seconds
		int ttl = 600;
		
		//// <value>
		/// Ttl in seconds to test for new versions of objects.
		/// </value>
		public int Ttl { get { return ttl; } set { ttl = value; } }
		
		public ReadCache()
		{
			
		}
		
		public void Flush()
		{
			map.Clear();
			ages.Clear();
		}
		
		public void Monitor()
		{
			List<int> ids = new List<int>(map.Keys);
			foreach( int id in ids )
			{
				GetNewCopy(id);
			}
		}
		
		public T this[int key]
		{
			get
			{
//				log.Debug( "requesting record from read cache", typeof(T), key, map.ContainsKey( key ) );
//				
//				if( ages.ContainsKey( key ) )
//				{
//					log.DebugFormat("Key: {0} OldAge: {1} HowOld? {2} ", key, ages[key], ages[key] - DateTime.UtcNow.AddSeconds( ttl * -1 ) );
//				}
				
				if( ages.ContainsKey( key ) && ages[key] < DateTime.UtcNow.AddSeconds( ttl * -1 ) )
				{
					GetNewCopy(key);
				}
				
				if( map.ContainsKey(key) )
				{					
					//log.Debug("returning ", map[key]);
					return map[key];
				}
				
				GetNewCopy(key);
				if( map.ContainsKey(key) )
					return map[key];
				return null;				
			}
			set
			{
				LoadObject( key, value );
			}
		}
		
		private void GetNewCopy( int id )
		{
			if( ! map.ContainsKey(id) )
			{
				LoadObject(id, AbstractRecord.Load<T>(id));
				return;
			}					
				
			int dbVersion = DataProvider.DefaultProvider.GetLatestVersion( modelName, id ) ;
			int localVersion = 0;
			if( map[id] != null )
				localVersion = ((IVersioned)map[id]).Version;
			//log.DebugFormat("dbVersion {0}, localVersion {1}", dbVersion, localVersion );
			if( dbVersion == localVersion )
			{
				lock (thisLock)
				{
					ages[id] = DateTime.UtcNow;
				}
				return;
			}
			LoadObject(id, AbstractRecord.Load<T>(id));
//			if( map[id] != null )
//				log.DebugFormat("Getting copy {0} ~size(kb): {1} ", map[id], map[id].SizeOf() );
		}
		
		private void LoadObject(int id, T value )
		{
			lock (thisLock)
			{
				map[id] = value;
				ages[id] = DateTime.UtcNow;
			}
		}
	}
}
