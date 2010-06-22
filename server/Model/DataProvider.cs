using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;

namespace EmergeTk.Model
{
    public abstract class DataProvider
    {
    	protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(DataProvider));
    	
        static Dictionary<Type, IDataProvider> Providers = new Dictionary<Type, IDataProvider>();

        public static void RegisterProvider(Type t, IDataProvider provider )
        {
            Providers[t] = provider;
        }

        public static IDataProvider RequestProvider<T>() where T : AbstractRecord, new()
        {
        	/*
            Type t = typeof(T);
            if (!Providers.ContainsKey(t))
            {
            	T r = new T();//dummy instantiation to register provider.            	
            }
            return Providers[t];
            */
            return DefaultProvider;
        }
        
        static IDataProvider defaultProvider;
        public static IDataProvider DefaultProvider
        {
        	get {
        		if( defaultProvider == null )
        		{
        			try {
	        			if( ConfigurationManager.AppSettings["DefaultDataProvider"] == null )
	        			{
	        				log.Warn("You do not have a default data provider specified.  defaulting to SQLite.");
	        				defaultProvider = EmergeTk.Model.Providers.SQLiteProvider.Provider;
	        			}
	        			else
	        				defaultProvider = Activator.CreateInstance( TypeLoader.GetType( ConfigurationManager.AppSettings["DefaultDataProvider"] ) ) as IDataProvider;

        			} catch (Exception e) {
        				log.Error( "Unable to load specified data provider: ", ConfigurationManager.AppSettings["DefaultDataProvider"],
        					Util.BuildExceptionOutput( e ) );
        			}
        		}
        		return defaultProvider;
        	}
        	set {
        		defaultProvider = value;
        	}
        }
        
        public virtual int RowCount<T>() where T : AbstractRecord, new()
        {
        	IDataProvider idp = ((IDataProvider)this);
        	T t = new T();
            if( ! idp.TableExists( t.DbSafeModelName ) )
            	return 0;
            object count = idp.ExecuteScalar(string.Format("SELECT COUNT(*) FROM {0};", t.DbSafeModelName));
            #if DEBUG
            	log.Debug( "count is ", count, count.GetType() ); 
            #endif
            return Convert.ToInt32( (long)count );
        }
        
        public static int GetRowCount<T>() where T : AbstractRecord, new()
        {
        	return RequestProvider<T>().RowCount<T>();
        }

        public static IRecordList<T> LoadList<T>() where T : AbstractRecord, new()
            { return RequestProvider<T>().Load<T>(); }
        public static IRecordList<T> LoadList<T>(params SortInfo[] sortInfos) where T : AbstractRecord, new()
        {
            IRecordList<T> list = RequestProvider<T>().Load<T>(sortInfos);
            list.Sorts = new List<SortInfo>(sortInfos);
			list.Clean = true;
			return list;
            //return RequestProvider<T>().Load<T>(sortInfos); 
        }
        public static IRecordList<T> LoadList<T>(params FilterInfo[] filterInfos) where T : AbstractRecord, new()
        {
            IRecordList<T> list = RequestProvider<T>().Load<T>(filterInfos);
            list.Filters = new List<FilterInfo>(filterInfos);
			list.Clean = true;
			return list;
            //return RequestProvider<T>().Load<T>(filterInfos); 
        }

		public static IRecordList<T> LoadList<T>(params IQueryInfo[] queryInfos) where T : AbstractRecord, new()
		{
			List<FilterInfo> filters = new List<FilterInfo>();
			List<SortInfo> sorts = new List<SortInfo>();
			foreach( IQueryInfo qi in queryInfos )
			{
				if( qi is SortInfo )
					sorts.Add(qi as SortInfo);
				else
					filters.Add(qi as FilterInfo);
			}
			IRecordList<T> irl = LoadList<T>(filters.ToArray(),sorts.ToArray());
			irl.Clean = true;
			return irl;
		}

        public static IRecordList<T> LoadList<T>(FilterInfo[] filterInfos, SortInfo[] sortInfos) where T : AbstractRecord, new()
        { 
            IRecordList<T> list = RequestProvider<T>().Load<T>(filterInfos, sortInfos);
            list.Filters = new List<FilterInfo>(filterInfos);
            list.Sorts = new List<SortInfo>(sortInfos);
			list.Clean = true;
            return list;
        }
		
		static bool lowerCaseTableNames = false;
		static bool lowerCaseTableNamesChecked = false;
		public static bool LowerCaseTableNames {
			get {
				if ( !lowerCaseTableNamesChecked )
				{
					LowerCaseTableNames =  Convert.ToBoolean(ConfigurationManager.AppSettings["LowerCaseTableNames"]);
					lowerCaseTableNamesChecked = true;
					log.Debug("Checked LowerCaseTableNames: set to " + LowerCaseTableNames);
				}
				return lowerCaseTableNames;
			}
			set {
				lowerCaseTableNames = value;
			}
		}
    }
}
