using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Web.Caching;
using System.Linq;
using EmergeTk.Model.Security;
using System.Runtime.Serialization;

namespace EmergeTk.Model
{
	public enum RecordState
	{
		New,
		Persisted,
		Deleted
	}
	
    [IgnoreType]
    public class AbstractRecord : IRecord, ICloneable, IJSONSerializable, IComparable
    {
       	protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(AbstractRecord));
		
		public void SetId(int id){
			this.id = id;
			definition = new RecordDefinition( this.GetType(), this.id );
		}
    	
    	public virtual void EnsureId()
    	{
    		if( id == 0 )
			{
				int newId = DataProvider.Factory.RequestId(this.GetType());
    			SetId( newId );
			}
    	}
		
		private IDataProvider _provider;
		public IDataProvider GetProvider()
		{
			if( _provider == null )
				_provider = DataProvider.Factory.GetProvider(this.GetType(), this.id );
			return _provider;
		}

       //if the record has an datarow reference it has been inserted.
		bool persisted = false;
		public bool Persisted {
			get
			{
				return originalValues != null || persisted;
			}
		}
		
		RecordState recordState = RecordState.New;
		public RecordState State
		{
			get
			{
				return recordState;
			}
		}
		
		public void SetLoadState( bool loadState )
		{
			loading = loadState;	
		}
		
		public void Save()
		{
			Save(false);
		}

		public void Save(DbConnection conn)
        {
            Save(false, true, conn);
        }
		
		public void Save(bool SaveChildren)
        {
            Save(SaveChildren, true, null);
        }
    
       	public virtual void Save(bool SaveChildren, bool IncrementVersion, DbConnection conn)
       	{
			if( loading )
				return;
			if( ! GetProvider().Synchronizing && ! disableValidation) 
			{
				ValidateAndThrow();
			}
			EnsureTablesCreated();
			bool inserted = Persisted;

            // right now, the IncrementVersion version of DataProvider.Save() is only needed when
            // we pass in the connection object (because we're doing the two stage save and 
            // need to wrap multiple saves in a transaction object).  The other permutations
            // are not needed as yet.  
            if (conn == null)
                GetProvider().Save(this, SaveChildren);
            else
                GetProvider().Save(this, SaveChildren, IncrementVersion, conn);

			this.persisted = true;
			this.recordState = RecordState.Persisted;
       	}

		/// <summary>
		/// returns a list of all cache keys associated with this record.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String[]"/>
		/// </returns>
		public static string[] GetCacheKeyList(AbstractRecord record)
		{
			if( CacheProvider.Instance == null )
				return null;
			return CacheProvider.Instance.GetStringList(record.GetCacheKey());
		}
		
		public static string[] GetCacheKeyList(RecordDefinition rd)
		{
			if( CacheProvider.Instance == null )
				return null;
			return CacheProvider.Instance.GetStringList(GetCacheKey(rd));
		}
		
		/// <summary>
		/// Returns the cache key for the list of all cache keys referring to this record.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		private string GetCacheKey()
		{
			RecordDefinition rd = new RecordDefinition( this.GetType(), this.id );
			return GetCacheKey(rd);
		}
		
		public static string GetCacheKey(RecordDefinition rd)
		{
			return "cachekeys:" + rd.ToString(); 
		}
		
		private static void AppendCacheKey( AbstractRecord record, string queryKey )
		{
			if( CacheProvider.Instance == null )
				return;
			CacheProvider.Instance.AppendStringList(record.GetCacheKey(),queryKey);
		}
		
		private static void AppendCacheKey( RecordDefinition def, string queryKey )
		{
			if( CacheProvider.Instance == null )
				return;
			CacheProvider.Instance.AppendStringList(GetCacheKey(def),queryKey);
		}
		
		private static void DeleteCacheKeyList(AbstractRecord record)
		{
			if( CacheProvider.Instance == null )
				return;
			CacheProvider.Instance.Remove( record.GetCacheKey() );  
		}
		
		private static void DeleteCacheKeyList(RecordDefinition rd)
		{
			if( CacheProvider.Instance == null )
				return;
			CacheProvider.Instance.Remove( GetCacheKey(rd) );
		}
		
		public void InvalidateCache()
		{
			InvalidateCache(this);
		}

		public static void InvalidateCache(AbstractRecord record)
		{
			if (CacheProvider.Instance == null)
				return;
			string[] keys = GetCacheKeyList(record);
			RemoveCacheKeys (keys);
			DeleteCacheKeyList(record);
		}
		
		public static void InvalidateCache(RecordDefinition rd)
		{
			if (CacheProvider.Instance == null)
				return;
			string[] keys = GetCacheKeyList(rd);
			RemoveCacheKeys (keys);
			DeleteCacheKeyList(rd);
		}
		
		private static void RemoveCacheKeys (string[] keys)
		{
			if( keys != null )
			{
				foreach (string s in keys)
				{
					log.Debug("removing cache item " + s);
					CacheProvider.Instance.Remove(s);
				}
			}
		}

		public void SaveChildRecordList<T>(string PropertyName, IRecordList<T> list) where T : AbstractRecord, new()
        {
            SaveRelations(PropertyName, list, true);
        }

        public virtual void SaveRelations(string PropertyName )
        {
        	SaveRelations(PropertyName, this[PropertyName] as IRecordList, false );
        }
        
        public virtual void SaveRelations(string PropertyName, IRecordList list, bool saveChildren )
		{
			EnsureId();
			//log.Debug( "saving list", PropertyName, list );
			if( list != null )
			{				
				foreach( AbstractRecord r in list )
				{
					if (r != null)
						r.EnsureId();
				}
			}
			
			string tableName = DbSafeModelName;
			string childTableName = DbSafeModelName + "_" + PropertyName;
			if ( DataProvider.LowerCaseTableNames ) 
			{
				childTableName = childTableName.ToLower();
				tableName = tableName.ToLower();
			}
			
		//	log.Debug("SaveRelations on tableName '" + tableName + "' and childTableName '" + childTableName + "'");
			
			ColumnInfo col = GetFieldInfoFromName(PropertyName);
			//log.Debug("saving relations for ", this, PropertyName, col.IsDerived);
            if (!GetProvider().TableExists(childTableName) || col.IsDerived)
                GetProvider().CreateChildTable(childTableName, col);
			
            if( list == null )
            {
            	log.Warn( "SaveRelations called with null list.");
            	return;
            }
			List<int> newRelations = new List<int>();
			
			//note - this operation is not thread safe.
			//foreach( AbstractRecord r in list )
			for( int i = 0; i < list.Count; i++ )
			{
				AbstractRecord r = list[i];
				if( r == null )
					continue;
				if( newRelations.Contains( r.id ) )
				{
					//Don't allow duplicate saves.
					log.Debug("Duplicate child record in list", this, r );
					continue;
				}
				
				newRelations.Add( r.id );
				
				if( saveChildren ) 
					r.Save(false);
				
			}
			
			List<int> oldRelations = list.RecordSnapshot;
			
			// add relations:
			IEnumerable<int> changeList = Enumerable.Except<int>(newRelations, oldRelations);
			//log.Debug("items adding to database", childTableName, PropertyName, changeList);
			foreach ( int id in changeList )
			{
				GetProvider().SaveSingleRelation( childTableName, this.ObjectId.ToString(), id.ToString() );
			}
			
			// remove relations:
			changeList = Enumerable.Except<int>(oldRelations, newRelations);
		//	log.Debug("items removing from database", childTableName, PropertyName, changeList);
			foreach ( int id in changeList) 
			{
				GetProvider().RemoveSingleRelation( childTableName, this.ObjectId.ToString(), id.ToString());
			}
				
			// copy list to its snapshot
			list.RecordSnapshot = newRelations;									
			ExpireRelations(PropertyName);
       	}
       	
       	public void ExpireRelations(string PropertyName )
       	{
     	  	if( CacheProvider.Instance != null )
			{
				string cacheKey = GetFieldReference( PropertyName ).ToString();
                if (cacheKey != null)
            	    CacheProvider.Instance.Remove(cacheKey);
			}
       	}
		
		public void SaveSingleRelation(string PropertyName, AbstractRecord child) 
		{
			SaveSingleRelation(PropertyName, child, false, true);
		}
		
		public void SaveSingleRelation(string PropertyName, AbstractRecord child, bool saveChildren, bool addToLocalList )
		{
			if( child == null || PropertyName == null )
			{
				throw new ArgumentNullException();	
			}
			ColumnInfo ci = this.GetFieldInfoFromName(PropertyName);
			if( (!(child is IDerived) && ci.ListRecordType != child.GetType()) || (child is IDerived && !ci.ListRecordType.IsInstanceOfType (child) ) )
			{
				throw new ArgumentException(string.Format("cannot save relation with child of type {0} into list of type {1}", child.GetType(), ci.ListRecordType));
			}
			string childTableName = DbSafeModelName + "_" + PropertyName;
			if ( DataProvider.LowerCaseTableNames ) childTableName = childTableName.ToLower();

			if (!GetProvider().TableExists(childTableName) || ci.IsDerived)
                GetProvider().CreateChildTable(childTableName, ci);
			
			// update snapshot
			if ( addToLocalList ) 
			{
				IRecordList relationsList = this[PropertyName] as IRecordList;
				if( relationsList != null )
				{
					// add child to relationsList if it doesn't already contain it
					if ( ! relationsList.Contains( child ) )
					{
						relationsList.Add( child );
						child.parent = this;
					}
			
					relationsList.RecordSnapshot.Add( child.id );
				}
			}
			
			if( saveChildren ) 
				child.Save(false);
			
			GetProvider().SaveSingleRelation(childTableName, this.ObjectId.ToString(), child.ObjectId.ToString() );
			
			ExpireRelations(PropertyName);
		}
		
		public void RemoveSingleRelation(string PropertyName, AbstractRecord child) 
		{
			if( child == null || PropertyName == null )
			{
				throw new ArgumentNullException();	
			}
			ColumnInfo ci = this.GetFieldInfoFromName(PropertyName);
			if( ci.ListRecordType != child.GetType() && ! child.GetType().IsSubclassOf( ci.ListRecordType ) )
			{
				throw new ArgumentException(string.Format("cannot remove relation with child of type {0} into list of type {1}", child.GetType(), ci.ListRecordType));
			}

			string childTableName = DbSafeModelName + "_" + PropertyName;
			if ( DataProvider.LowerCaseTableNames ) childTableName = childTableName.ToLower();	

			IRecordList relationsList = this[PropertyName] as IRecordList;
			
			log.Debug("removing single relation", PropertyName, childTableName, child, relationsList);
			
			// update snapshot
			if ( relationsList != null )
			{
				// remove child if it is still in relationsList
				if ( relationsList.Contains( child ) )
					relationsList.Remove( child );
				
				relationsList.RecordSnapshot.Remove( child.id );
			}
			
			GetProvider().RemoveSingleRelation(childTableName, this.ObjectId.ToString(), child.ObjectId.ToString()); 
			
			ExpireRelations(PropertyName);
		}
		
		public void Move( AbstractRecord oldParent, string oldProperty, AbstractRecord newParent, string newProperty )
		{
			oldParent.RemoveSingleRelation(oldProperty,this);
			newParent.SaveSingleRelation(newProperty,this);
			if( this.parent == oldParent )
				this.parent = newParent;
		}     
		
		private RecordDefinition definition;
		public RecordDefinition Definition
		{
			get
			{
				if (definition.Type == null || definition.Id == 0)
				{
					if ( this.id == 0 )
						throw new InvalidOperationException ("Cannot request defintion on a type with 0 id.");
					definition = new RecordDefinition(this.GetType (), this.id);
				}
				return definition;
			}
		}

		public FieldReference GetFieldReference( string column )
		{
			return new FieldReference( Definition, column );
		}

        public virtual void Delete()
        {
			this.recordState = RecordState.Deleted;
			GetProvider().Delete(this);
            if( CacheProvider.Instance != null )
				CacheProvider.Instance.Remove(this);
        }

		public void EnsureTablesCreated()
		{
			string tableName = this.DbSafeModelName;	
			if ( DataProvider.LowerCaseTableNames ) tableName = tableName.ToLower();
            if (GetProvider().TableExists(tableName))
                return;
            GetProvider().CreateTable(this.GetType(), tableName);
		}

		/*private static Dictionary<string,AbstractRecord> recordCache = new Dictionary<string,AbstractRecord>();
		private static Dictionary<AbstractRecord,List<string>> cacheMap = new Dictionary<AbstractRecord,List<string>>();
        private static Dictionary<Type, List<string>> cacheKeyByType = new Dictionary<Type, List<string>>();
        private static Dictionary<RecordDefinition, List<string>> cacheKeyByRecordDefinition = new Dictionary<RecordDefinition, List<string>>();
		*/

		private Dictionary<RecordDefinition,AbstractRecord> loadingContext;
		private AbstractRecord GetRecordFromLoadingContext( Type t, int id )
		{
			RecordDefinition rd = new RecordDefinition(t,id);
			if( loadingContext != null && loadingContext.ContainsKey(rd) )
			{
				return loadingContext[rd];
			}
			return null;
		}
		
		private void SetRecordToLoadingContext( AbstractRecord r )
		{
			if( loadingContext == null )
			{
				loadingContext = new Dictionary<RecordDefinition,AbstractRecord>();
			}
			loadingContext[ new RecordDefinition( r.GetType(), r.id ) ] = r;
		}
		
		private void ClearLoadingContext()
		{
			loadingContext = null;
		}
		
		public static T Load<T>(params FilterInfo[] filters) where T : AbstractRecord, new()
		{
			return Load<T>( null, filters );
		}
		
		public static T Load<T>(T record, params FilterInfo[] filters) where T : AbstractRecord, new()
        {
            return Load<T>(record, true, filters);
        }
        
       	public static AbstractRecord Load(Type type, params FilterInfo[] filters )
		{
			return (AbstractRecord)TypeLoader.InvokeGenericMethod
				(typeof(AbstractRecord),"Load", new Type[]{type}, null, new Type[]{typeof(FilterInfo[])}, new object[]{filters});
		}
		
		public static T Load<T>(bool allowCustomLoad, params FilterInfo[] filters) where T : AbstractRecord, new()
		{
			return Load<T>(null, allowCustomLoad, filters );
		}
		
		public static T Load<T>(T record, bool allowCustomLoad, params FilterInfo[] filters) where T : AbstractRecord, new()
		{
			if ( ( record is ICustomLoadable<T> ) && allowCustomLoad)
            {
                return (record as ICustomLoadable<T>).CustomLoad(filters);
            }
            
            //TODO: support ORs, parentheticals.			
			int id = -1;
            string WhereClause = DataProvider.Factory.GetProvider(typeof(T)).BuildWhereClause( filters );
			if( filters != null && filters.Length > 0 && filters[0].ColumnName == "ROWID" )
				id = Convert.ToInt32 (filters[0].Value);
            if( id == 0 ) //will never load a valid record with id 0.
				return null;
			return Load<T>( record, id, allowCustomLoad, WhereClause );		
		}

		public static T Load<T>(bool allowCustomLoad, string WhereClause) where T : AbstractRecord, new()
		{
			return Load<T>( null, -1, allowCustomLoad, WhereClause );
		}
		
		public string CreateStandardCacheKey()
		{
			return CreateStandardCacheKey (this.DbSafeModelName.ToLower(), this.id);
		}	

		public static string CreateStandardCacheKey (string model, int id)
		{
			return string.Format("{0}.rowid-=-{1}", model, id );
		}

		static float loads, hits, misses, singularHits, pluralHits;
		public static T Load<T>(T record, int id, bool allowCustomLoad, string WhereClause) where T : AbstractRecord, new()
        {
        	IDataProvider provider = DataProvider.Factory.GetProvider(typeof(T),id);
			
			if( provider == null )
				throw new NullReferenceException("Provider is null.");
			
			
			string name = GetDbSafeModelName(typeof(T));
			if ( DataProvider.LowerCaseTableNames ) name = name.ToLower();
			
            // string key = name + "_" + WhereClause;
			
			if (!provider.TableExists(name))
            {
				if( record == null )
					record = new T();
                (record as AbstractRecord).EnsureTablesCreated();
                return null;
            }
            
            DataTable result = null;
			T cacheRecord = null;
			string cacheKey = null;
			if( CacheProvider.Instance != null )
			{
            	cacheKey = name + "." + WhereClause;
				//first attempt to see if we have any objects in the cache for an arbitrary where clause.
				//later, once we know what id we're dealing with, we'll do this again with the id.
            	cacheRecord = CacheProvider.Instance.GetRecord<T>(cacheKey);
			}

            if( cacheRecord != null )
            {
            	loads++;
            	hits++;
            	//log.Debug("CACHE HIT ", cacheKey, loads, hits, misses, hits/loads );
            	return cacheRecord;            		
            }
			else
			{
				loads++;
				misses++;
				//log.Debug("CACHE MISS ", cacheKey, loads, hits, misses, hits/loads );
				//I don't think we need to do IoC here - all dbs support this simple of a select stmt!
				result = provider.ExecuteDataTable(string.Format("SELECT * FROM {0} WHERE {1} ", provider.EscapeEntity(name), WhereClause));
            }

            id = record != null ? record.id : id;
			if (result == null || result.Rows.Count == 0 )
            {
            	if( cacheKey != null && cacheKey.Contains( provider.GetIdentityColumn() ) )
            	{
            		PutItemInCache<T>( cacheKey, id, null );
            	}
                return null;
            }
			
			id = Convert.ToInt32(result.Rows[0][provider.GetIdentityColumn()]);

			//do we always want to use ROWID?
			string k = typeof(T).Name + "." + id;
			T o = null;
			if( CacheProvider.Instance != null )
			{
				//try and get local
				var rd = new RecordDefinition (typeof(T), id);
				o = CacheProvider.Instance.GetLocalRecord (rd) as T;
				if (o == null)
					o = CacheProvider.Instance.GetRecord<T>(k);
			}
        	if( o != null )
        	{
        		return o;
        	}
            if( record == null )
				record = new T();
			record.SetId (id);
			if (CacheProvider.EnableCaching)
				CacheProvider.Instance.PutLocal(cacheKey, record);
           	LoadFromDataRow<T>(record, result.Rows[0]);
            record.recordState = RecordState.Persisted;
			PutItemInCache<T>(cacheKey, record.id, record);
            return record;
        }
        
        public static void PutItemInCache<T>( string key, int record_id,  object o )
        {
        	if( CacheProvider.EnableCaching == true ) 
        	{
        		PutObjectInCache( key, o );
	            RecordDefinition rd = new RecordDefinition( typeof(T), record_id );
				AppendCacheKey(rd, key );
	        }
        }
        
		public static void PutRecordInCache( AbstractRecord r )
        {
        	if( CacheProvider.EnableCaching == true ) 
        	{
				string key = r.CreateStandardCacheKey();
        		PutObjectInCache( key, r );	            
				AppendCacheKey(r.Definition, key );
	        }
        }
		
        private static void PutObjectInCache( string key, object o )
        {
        	if( CacheProvider.EnableCaching == true && CacheProvider.Instance != null ) 
        	{	           
        		if( o is AbstractRecord )
        		{
					CacheProvider.Instance.Set(key, o as AbstractRecord);
        		}
        		else
        		{
					CacheProvider.Instance.Set(key,o);
				}
            }
        }

		public P LoadParent<P>(string column) where P : AbstractRecord, new()
		{
			ColumnInfo ci = ColumnInfoManager.RequestColumn<P>(column);
			if (ci == null)
				throw new InvalidOperationException (
					string.Format ("The specified column: {0} does not exist on {1}", column, typeof(P)));
			IRecordList<P> cs = LoadParents<P>(ci);
			if( cs != null && cs.Count > 0 )
			{
				return cs[0];
			}
			else
				return null;
		}

		public IRecordList<P> LoadParents<P>(string column) where P : AbstractRecord, new()
		{
			ColumnInfo ci = ColumnInfoManager.RequestColumn<P>(column);
			return LoadParents<P>(ci);
		}
		
		public List<int> LoadParentIds(ColumnInfo parentColumn)
		{
			return LoadParentIds(GetProvider(), parentColumn, ObjectId);
		}
		
		public static List<int> LoadParentIds(IDataProvider provider, ColumnInfo parentColumn, object value)
		{
			List<int> parents = null;
			if( parentColumn.IsList )
			{
				string parentName = parentColumn.ModelType.Name;
				if ( DataProvider.LowerCaseTableNames ) parentName = parentName.ToLower();
	        	string parentTableName = parentName + "_" + parentColumn.Name;
				if ( DataProvider.LowerCaseTableNames ) parentTableName = parentTableName.ToLower();
	            if (!provider.TableExists(parentTableName))
	                return null;
				//TODO: investigate an approach to caching the loaded parents list that can
				//efficiently be cleaned up when the relations are saved.            
				parents = provider.ExecuteVectorInt(string.Format("SELECT Parent_Id FROM {0} WHERE Child_Id = '{1}'", parentTableName, value));
			}
			else
			{
				string parentName = parentColumn.ModelType.Name;
				FilterInfo fi = new FilterInfo(parentColumn.Name, value);
				string whereClause = DataProvider.DefaultProvider.BuildWhereClause(new FilterInfo[]{fi});
				parents = provider.ExecuteVectorInt(string.Format("SELECT ROWID FROM {0} WHERE {1}", parentName, whereClause));
			}
			return parents;
		}
		
        public IRecordList<P> LoadParents<P>(ColumnInfo parentColumn) where P : AbstractRecord, new()
        {
        	List<int> parents = LoadParentIds(parentColumn);
            if (parents == null || parents.Count == 0)
            {
                return new RecordList<P>();
            }
            return DataProvider.DefaultProvider.Load<P>(parents);
        }

		public List<int> LoadChildrenIds(ColumnInfo fi)
		{
			if( this.id == 0 )
				return null;
			if( fi == null )
        	{
        		throw new ArgumentNullException("ColumnInfo");
        	}
            string joinTable = DbSafeModelName + "_" + fi.Name;
			string childTable = GetDbSafeModelName(fi.ListRecordType);
			if ( DataProvider.LowerCaseTableNames ) joinTable = joinTable.ToLower();
            if (!GetProvider().TableExists(joinTable) || (!fi.IsDerived && !GetProvider ().TableExists (childTable)))
                return null;
			
            string cacheKey = GetFieldReference( fi.Name ).ToString();
			//log.Debug("LoadChildrenIds cachekey: " + cacheKey);
	        	
	        List<int> childrenIds = null;
			if( CacheProvider.Instance != null && cacheKey != null)
				childrenIds = CacheProvider.Instance.GetObject(cacheKey) as List<int>;
	        	
        	if( childrenIds == null )
        	{
				if (fi.ListRecordType.GetInterface("IDerived") == null)
				{
					childrenIds = GetProvider().ExecuteVectorInt(string.Format("SELECT Child_Id FROM `{0}` a JOIN `{2}` b on a.child_id = b.rowid WHERE Parent_Id = '{1}'", 
	                                       joinTable, ObjectId, childTable));
				}
				else
				{
					childrenIds = GetProvider().ExecuteVectorInt(string.Format("SELECT Child_Id FROM `{0}` WHERE Parent_Id = '{1}'", 
	                                       joinTable, ObjectId));
				}
        		PutObjectInCache( cacheKey, childrenIds);
        	}
			return childrenIds;
		}

	
		protected IRecordList<C> LoadChildren<C>(ColumnInfo fi) where C : AbstractRecord, new()
        {
			List<int> childrenIds = LoadChildrenIds(fi);
			if( childrenIds == null || childrenIds.Count == 0 )
				return new RecordList<C>();
            IRecordList<C> childRecords = DataProvider.DefaultProvider.Load<C>(childrenIds);
			childRecords.RemoveAll (r=>r==null);
            childRecords.Parent = this;
            foreach (C c in childRecords)
				if (c != null)
                	c.parent = this;
			childRecords.Parent = this;
            childRecords.Clean = true;
			// save snapshot:
			if( childRecords.Count > 0 )
				childRecords.RecordSnapshot = new List<int>(childrenIds);
            return childRecords;
        }
        
        protected C LoadChild<C>(object childId, ColumnInfo fi) where C : AbstractRecord, new()
        {
            string childTableName = DbSafeModelName + "_" + fi.Name;
			if ( DataProvider.LowerCaseTableNames ) childTableName = childTableName.ToLower();
            if (!GetProvider().TableExists(childTableName))
          		return null;

			C child = null;
            //again, this sql is so simple I don't think we need IoC
            DataTable childTable = GetProvider().ExecuteDataTable(string.Format("SELECT * FROM {0} WHERE Parent_Id = '{1}'AND Child_Id = '{2}' ORDER BY ROWID", childTableName, ObjectId, childId));
            foreach (DataRow row in childTable.Rows)
            {       
        		child= (C)GetRecordFromLoadingContext( typeof(C), (int)row["Child_Id"] );
        		if( child == null )
        		{
                    child = Activator.CreateInstance<C>();
                	child.Parent = this;
                    child.loadingContext = loadingContext;
                    child = AbstractRecord.LoadUsingRecord<C>(child, (int)row["Child_Id"]);
                }
            }
            return child;
        }

        public void LoadChildList<T>(string prop) where T : AbstractRecord,new()
        {
        	if( loadedProperties == null )
        		loadedProperties = new HashSet<string>();
        	this[prop] = this.LoadChildren<T>(this.GetFieldInfoFromName(prop));
        	loadedProperties.Add(prop);
        }
        
        public static T Load<T>(object id) where T : AbstractRecord, new()
		{
			int intId;
            if ((id as String) == String.Empty || id == null)
                return null;
			//log.DebugFormat("loading type {0} with id: '{1}'", typeof(T), id );
			T returnValue = null;
			if( id is int )
			{
				returnValue = LoadUsingRecord<T>( null, (int)id );
			}
			else if( id is string && int.TryParse((string)id,out intId) )
			{
				returnValue = LoadUsingRecord<T>( null, intId );
			}
			else
			{
				returnValue = LoadUsingRecord<T>( null, id );
			}
			return returnValue;
		}

        public static T LoadUsingRecord<T>(T record, object id) where T : AbstractRecord, new()
        {        	
		   return Load<T>(record, id, "ROWID" );
        }
		
		public static T LoadUsingRecord<T>(T record, int id) where T : AbstractRecord, new()
        {        	
			if (record == null)
			{
				RecordDefinition rd = new RecordDefinition (typeof(T), id);
				if (CacheProvider.Instance != null)
				{
					record = (T) CacheProvider.Instance.GetLocalRecord (rd);
					if (record != null)
					{
						return record;	
					}
				}
			}
			return Load<T>(record, id,  "ROWID" );
        }
		
		public void Reload()
		{
			log.Debug("reloading - " + this.id);
            bool oldLoadState = this.loading;
            this.SetLoadState(true);
            try
            {
                TypeLoader.InvokeGenericMethod
                    (typeof(AbstractRecord), "Reload", new Type[] { this.GetType() }, this,
                    Type.EmptyTypes, new object[] { });
            }
            finally
            {
                this.SetLoadState(oldLoadState);
            }
			log.Debug("done reloading - " + this.id);
		}
		
		public void Reload<T>() where T : AbstractRecord, new()
		{
			//log.Debug("Reloading ", this.Definition, System.Environment.StackTrace);
			if (this.loadedProperties != null )
			{
				string[] props = this.loadedProperties.ToArray();
				foreach( string key in props )
				{
					log.Debug ("Nulling out property", key);
					this[key] = null;
				}
				this.loadedProperties.Clear();
			}
			AbstractRecord.LoadUsingRecord<T>(this as T,this.id);
		}

		public static T Load<T>(string columnName, object id) where T : AbstractRecord, new()
		{
			return Load<T>(null, id, columnName);
		}
		
		public static T Load<T>(string columnName, int id) where T : AbstractRecord, new()
		{
			return Load<T>(null, id, columnName);
		}

        public static T Load<T>(T record, object id, string columnName) where T : AbstractRecord, new()
		{
			return Load<T>(record, new FilterInfo(columnName,id,FilterOperation.Equals));
		}
		
		 public static T Load<T>(T record, int id, string columnName) where T : AbstractRecord, new()
		{
			return Load<T>(record, new FilterInfo(columnName,id,FilterOperation.Equals));
		}

    	private int id = 0;
        
        AbstractRecord parent;
      
        virtual public int Id { get { return id; } internal set {
			SetId(value);
        	} 
        }
        
        int version = 0;
        public int Version { 
        	get
        	{
        		return version;
        	}
        	internal set
        	{
        		version = value;
        	}
        }

		public void SetVersion(int version)
		{
			this.version = version;
		}

		string modelName;				
        virtual public string ModelName 
        {
        	get
        	{
        		if( modelName == null )
        		{        			
	        		Type type = this.GetType();
	        		modelName = type.Name;  
		        	if (type.IsGenericType)
		            {
		                modelName = modelName.Substring(0, modelName.IndexOf('`'));
		                foreach( Type t in type.GetGenericArguments() )
		                {
		                    modelName += t.Name;
		                }
		            }		            	
		        }				
        		return modelName; 
        	}
        }
		
		static Dictionary<Type,string> dbSafeModelNames = new Dictionary<Type, string>();
		static Dictionary<string,Type> dbSafeModelNamesRev = new Dictionary<string, Type>();
		
		public static string GetDbSafeModelName(Type t)
		{
			if (t == null || t.IsGenericTypeDefinition || t.IsAbstract)
				return null;
			if( ! dbSafeModelNames.ContainsKey(t) )
			{
				AbstractRecord r = (AbstractRecord)Activator.CreateInstance(t);
				dbSafeModelNames[t] = r.DbSafeModelName;
				dbSafeModelNamesRev[r.DbSafeModelName] = t;
			}
			return dbSafeModelNames[t];
		}
        
		public static Type GetTypeFromDbSafeName (string safeName)
		{
			if (dbSafeModelNamesRev.ContainsKey(safeName))
				return dbSafeModelNamesRev[safeName];
			return null;
		}
        
        public string DbSafeModelName
        {
        	get
        	{
        		if ( DataProvider.LowerCaseTableNames )
        			return ModelName.ToLower();
        		else
        			return ModelName;
        	}
        }

		virtual public T GetT<T>(String indexer) 
		{
			return (T)this[indexer];
		}

        virtual public object this[string Name]
        {
            get
            {
				if( isStale )
				{
					Reload();
				}
				
            	if( Name == null )
            	{
            		log.Error("AbstractRecord indexer called with null Name parameter");
            		return null;
            	}
            	if( Name.Contains(".") )
            	{
            		return GetDerivedProperty( Name );
            	}
            	GenericPropertyInfo gpi = TypeLoader.GetGenericPropertyInfo(this,Name);
                if (gpi.PropertyInfo == null)
                {
                	log.Warn("Invalid property requested", this, this.GetType(), Name );
                	return null;
                }
                return gpi.Getter(this);
            }
            set
            {
                GenericPropertyInfo gpi = TypeLoader.GetGenericPropertyInfo(this,Name);
                if (gpi.PropertyInfo == null || gpi.Setter == null)
                {
                	log.Warn("Invalid propercty requested", this, Name );
                	return;
                }
                //if( value == this[Name] )
                //	return;
                	
                if( value != null && value.GetType() == gpi.PropertyInfo.PropertyType )
                {
					gpi.Setter(this,value);
                }
				else
				{
					gpi.Setter(this, PropertyConverter.Convert(value, gpi.PropertyInfo.PropertyType) );
				}
            }
        }
        
        public object GetDerivedProperty( string name )
        {
       		if( name.Contains(".") )
            {
            	AbstractRecord tmpSource = this;
    			string[] parts = name.Split('.');
    			for( int i = 0; i < parts.Length - 1; i++ )
    			{
    				if( tmpSource[parts[i]] is AbstractRecord )
    					tmpSource = tmpSource[parts[i]] as AbstractRecord;
    				else
    				{
    					Debug.Trace(string.Format("AbstractRecord:GetDerivedProperty: {0} -- {1} -- {2} does not chain down AbstractRecord sources.", 
    						tmpSource, tmpSource.GetType(), name) );
    					return null;
    				}
    			}
    			string fieldKey = parts[parts.Length - 1];
    			return tmpSource[ fieldKey ];
	        }
	        return null;
        }
        
        public virtual int ComputeRecordWeight()
        {
            return 1;
        }

        virtual public ColumnInfo[] Fields
        {
            get
            {
                if (!ColumnInfoManager.TypeIsRegistered(this.GetType()))
                {
                    List<PropertyInfo> props = DiscoverDerivedProperties(this.GetType());
                    List<ColumnInfo> fs = new List<ColumnInfo>();
                    foreach (PropertyInfo pi in props)
                    {
                        bool readOnly = !pi.CanWrite;
						
						bool exists = false;
                        foreach (ColumnInfo ci in fs)
                        {
                            if (ci.Name == pi.Name)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (exists) { continue; }

                        ColumnInfo fi = new ColumnInfo(pi.Name,pi.PropertyType,DataType.None,this.GetType(),readOnly);
                        fi.PropertyInfo = pi;
                        PropertyTypeAttribute pta = TypeLoader.GetAttribute(typeof(PropertyTypeAttribute), pi) as PropertyTypeAttribute;
                        FriendlyNameAttribute fa = TypeLoader.GetAttribute(typeof(FriendlyNameAttribute), pi) as FriendlyNameAttribute;
                        HelpTextAttribute ha = TypeLoader.GetAttribute(typeof(HelpTextAttribute), pi) as HelpTextAttribute;
                        IdentityAttribute ia = TypeLoader.GetAttribute(typeof(IdentityAttribute), pi) as IdentityAttribute;
                        if (pta != null)
                        {
                            if (pta.Type == DataType.Ignore)
                                continue;
                            fi.DataType = pta.Type;
							if( pta.Type == DataType.ReadOnly )
								fi.ReadOnly = true;
                            if (pta.Type == DataType.Text)
                                fi.Length = pta.Length;
                        }
                        else
                        {
                            if (TypeIsRecordList(fi.Type))
                                fi.DataType = DataType.RecordList;
                            else
                                fi.DataType = DataType.None;
                        }
                        
                        if( fa != null )
                        	fi.FriendlyName = fa.Name;
                        if( ha != null )
						{
                        	fi.HelpText = ha.Text;
						}
                        fi.Identity = ia != null;
                        if( fi.Identity )
							this.DefaultProperty = fi.Name;
                        
                        if (TypeIsRecordList(fi.Type))
                        {
	                        if( pi.PropertyType.GetGenericArguments()[0].GetInterface("IDerived") != null )
	                        {
	                        	fi.IsDerived = true;
	                        }
	                    }
	                    else
	                    {
	                    	if( pi.PropertyType.GetInterface("IDerived") != null )
	                        {
	                        	fi.IsDerived = true;
	                        }
	                    }
                        
                        fs.Add(fi);
                    }
                    ColumnInfoManager.RegisterColumns(this.GetType(), fs.ToArray());
                }
                return ColumnInfoManager.RequestColumns(this.GetType());
            }
			set
			{
				throw new System.NotImplementedException("That feature is not implemented.");
			}
        }

        public virtual Type GetFieldTypeFromName(string name)
        {
			ColumnInfo ci = GetFieldInfoFromName(name);
			if( ci == null )
				return null;
            return GetFieldInfoFromName(name).Type;
        }

        public ColumnInfo GetFieldInfoFromName(string name)
        {
            foreach (ColumnInfo fi in Fields)
                if (fi.Name == name)
                    return fi;
            //log.Debug("could not find field info", this, this.GetType(), name, Fields );
            return null;
        }
		
        private bool loading = false;

		public bool Loading {
			get {
				return loading;
			}
		}

        private bool lazyLoad = true;
        private bool lazyLoadProperties = false;
        public bool LazyLoad { get { return lazyLoad; } set { lazyLoad = value; } }
        public bool LazyLoadProperties { get { return lazyLoad; }  set { lazyLoadProperties = value; } }
        
        public static T LoadFromDataRow<T>(DataRow row) where T : AbstractRecord, new()
        {
        	T newT = new T ();
			LoadFromDataRow(newT, row);
			PutRecordInCache (newT);
			return newT;
        }
        
        public static T LoadFromDataRow<T>(T r, DataRow row) where T : AbstractRecord, new()
        {
            r.loading = true;            
            r.SetId(Convert.ToInt32(row["ROWID"]));
			if( r is IVersioned )			
            	r.Version = Convert.ToInt32( row["Version"] );
            r.SetRecordToLoadingContext(r);
			r.SetupOriginalValues(row);
           	r.SyncToSource();
           	r.ClearLoadingContext();
            r.loading = false;
            r.Authorize();
            return r;
        }	
		
		public static void VoidLoadFromReader<T>(IDataReader reader) where T : AbstractRecord, new()
		{
			//overload to allow for one line reader to load code.
			LoadFromReader<T>(reader);
		}
		
		public static T LoadFromReader<T>(IDataReader reader) where T : AbstractRecord, new()
        {
        	T r = new T ();
			r.loading = true;            
            r.SetId(Convert.ToInt32(reader["ROWID"]));
			if( r is IVersioned )			
            	r.Version = Convert.ToInt32( reader["Version"] );
            r.SetRecordToLoadingContext(r);
			r.SetupOriginalValues(reader);
           	r.SyncToSource();
           	r.ClearLoadingContext();
            r.loading = false;
            r.Authorize();
			//CacheProvider.Instance.PutLocal (string.Format("{0} . ROWID = {1}", GetDbSafeModelName(typeof(T)), r.id), r);
            PutRecordInCache (r);
            return r;
        }		
		
		/// <summary>
		/// The purpose of CreateFromRecord is to create a copy of the input record as close as possible
		/// to what is represented in the database.  If the record has not yet been saved to the database, or this i
		/// is the original copy of the record, then we call clone instead (although if an id is set, that is associated.)
		/// perform a clone operation.
		/// 
		/// Note that this is different from the clone operation, which zeroes out the id.
		/// </summary>
		/// <param name="source">
		/// A <see cref="T"/>
		/// </param>
		/// <returns>
		/// A <see cref="T"/>
		/// </returns>
		public static T CreateFromRecord<T>(T source) where T : AbstractRecord, new()
		{
			if( source.id == 0 )
			{
				//if id is 0, this is identical to a clone operation.
				return source.Clone() as T;
			}
			if (source is IDerived && source.GetType () != typeof(T))
			{
				return TypeLoader.InvokeGenericMethod(typeof(AbstractRecord), "CreateFromRecord", new Type[] {source.GetType()}, 
					null, new object[] {source}) as T;
			}
			
			if( source.OriginalValues == null )
			{
				//I think that if we have no original values, honoring the spirit of this function
				//suggests that we clone and set id to the source id.
				T t = source.Clone() as T;
				t.SetId(source.id);
				return t;
			}
			T dest = new T();
			dest.SetId(source.id);
			dest.loading = true;
			if( dest is IVersioned )
				dest.Version = source.Version;
			foreach( var kvp in source.OriginalValues )
				dest.SetOriginalValue( kvp.Key, kvp.Value );
			dest.SyncToSource();
            if (source.loadedProperties != null)
            {
                dest.loadedProperties = new HashSet<string>();
				string[] props = source.loadedProperties.ToArray();
                foreach (String prop in props)
                {
                    dest[prop] = source[prop];
					dest.loadedProperties.Add(prop);
                }
            }
			dest.loading = false;
			dest.Authorize();
			return dest;
		}
		
		private void SetupOriginalValues(DataRow row)
		{
			foreach( DataColumn col in row.Table.Columns)
			{
				SetOriginalValue( col.ColumnName, row[col.ColumnName] );
			}
		}
		
		private void SetupOriginalValues(IDataReader reader)
		{
			for (int i = 0; i < reader.FieldCount; i++)
			{
				SetOriginalValue (reader.GetName(i), reader[i]);
			}
		}
		
		private Dictionary<string,object> originalValues;
		public Dictionary<string,object> OriginalValues
		{
			get {
				return originalValues;
			}
		}
		
		public void SetOriginalValue(string key, object value)
		{
            if (value == System.DBNull.Value) 
                return;

			if( originalValues == null )
				originalValues = new Dictionary<string, object>();
			originalValues[key] = value;
		}
        
		public void SyncToSource()
		{
			SyncToSource(this.originalValues);
		}
		
        public void SyncToSource (Dictionary<string, object> originals)
        {
        	foreach (ColumnInfo fi in this.Fields)
	        {
        		if (fi.DataType != DataType.RecordList)
	            {
        			if (originals.ContainsKey (fi.Name) && originals[fi.Name] != DBNull.Value)
					{
        				Type type = fi.Type;
        				
                    	if (fi.IsRecord)
                    	{
        					if (originals[fi.Name] != DBNull.Value && !lazyLoadProperties)
        						LoadProperty (fi);
        				}
                    	else
						{
							if (fi.DataType != DataType.Json)
							{
								this[fi.Name] = PropertyConverter.Convert(originals[fi.Name], type);
							}
							else
							{
								Object o = originals[fi.Name];
								string s = o as string;
								
								if (! string.IsNullOrEmpty (s))
								{
									this[fi.Name] = JSON.DeserializeObject (fi.Type, s);
								}
								else if (o != null)
									this[fi.Name] = o;
							}
	                    }
	                }
	            }
	            else if( ! this.lazyLoad )
	            {
	                try
	                {
						this[fi.Name] = TypeLoader.InvokeGenericMethod(fi.ListRecordType,"LoadChildren", new Type[] { typeof(ColumnInfo) }, this, new object[]{fi} );
	                }
	                catch(Exception e)
                    {
                     	log.Error(e);
						throw new Exception("error syncing to source", e); 
                    }
	            }
	        }
        }

        public bool PropertyLoaded(String propertyName)
        {
            return loadedProperties != null && loadedProperties.Contains(propertyName);
        }
        
        HashSet<string> loadedProperties;
        bool isStale = false;
        public void CheckProperty(string prop, AbstractRecord record)
        {
			bool reload = false;
			
			if( record != null )
			{				
				if( !record.isStale ) //valid record.
					return;

                if( record.recordState == RecordState.Deleted )
				{
					UnsetProperty(prop, true);
					return;
				}
				else
				{
					UnsetProperty(prop, false);
				}
			}

			bool unsetLoading = false;
        	
        	if( loadedProperties == null )
        	{
        		loadedProperties = new HashSet<string>();
        	}
        	else if( loadedProperties.Contains(prop) )
			{
				//valid null prop
				return;
			}
			
			if( ! loading )
        	{
        		unsetLoading = true;
        		loading = true;
        	}
        	
			LoadProperty(this.GetFieldInfoFromName(prop));
			
			if( unsetLoading )
			{
				loading = false;
			}
        }
		
		public void MarkAsStale ()
		{
			this.isStale = true;	
		}
		
		public void UnmarkAsStale()
		{
			this.isStale = false;
		}

        public void UnsetProperty(String prop, bool nullify)
        {
            bool oldLoading = loading;
            loading = true;
            // set the loading flag, so that nulling out the property (which goes through the object property getter)
            // does not have unwanted side effects.

            RemoveFromLoadedProperties(prop);
			if (nullify)
           	 this[prop] = null;
            loading = oldLoading;
        }

        private void RemoveFromLoadedProperties(String prop)
        {
            if (this.loadedProperties != null && loadedProperties.Contains(prop))
            {
                loadedProperties.Remove(prop);
            }
        }

        public void RemoveFromLoadedProperties(params String[] props)
        {
            foreach (String prop in props)
                this.RemoveFromLoadedProperties(prop);
        }
		
		public static bool IsDerived( Type t )
		{
			return typeof(IDerived).IsAssignableFrom(t);
		}

        internal void AddToLoadedProperties(String prop)
        {
            if (this.loadedProperties == null)
                loadedProperties = new HashSet<string>();

            if (!loadedProperties.Contains(prop))
                loadedProperties.Add(prop);
        }
        
        private void LoadProperty(ColumnInfo fi )
        {
            if (originalValues == null || !originalValues.ContainsKey(fi.Name) || originalValues[fi.Name] == DBNull.Value || originalValues[fi.Name] == null)
                return;
        		
        	try
    		{
    			Type type = fi.Type;
				int id = (int)originalValues[fi.Name];
				if( id == 0 )
					return;
				
				if( fi.IsDerived )
				{
					type = DataProvider.DefaultProvider.GetTypeForId(id);	
				}
				if( type != null )
				{
					AbstractRecord r = AbstractRecord.Load(type, id);
					if( r != null )
					{
						r.parent = this;
					}
	    			this[fi.Name] = r;
				}
				else
					this[fi.Name] = null;
                AddToLoadedProperties(fi.Name);
    		}
    		catch(Exception e)
    		{	                    			
    			log.Error("error converting child field key: %s value: %o", fi.Name, originalValues[fi.Name], Util.BuildExceptionOutput(e) );
    			throw new Exception("Error syncing to source", e);
    		}
        }

        protected void lazyLoadProperty<T>(string prop) where T : AbstractRecord,new()
        {
         	lazyLoadProperty<T>(prop, true );
        }

		protected void lazyLoadProperty<T>(string prop, bool createIfEmpty ) where T : AbstractRecord,new()
		{
			if( loadedProperties == null )
        		loadedProperties = new HashSet<string>();
        	if( ! loadedProperties.Contains( prop ) )
        	{
				var originalLoading = this.loading;
				this.loading = true;
        		IRecordList<T> list = this.LoadChildren<T>(this.GetFieldInfoFromName(prop));
				list.Clean = true;
				if( list.Count > 0 || createIfEmpty )
				{
        			this[prop] = list;
				}
        		loadedProperties.Add(prop);
				this.loading = originalLoading;
        	}
		}

        //Properties
        private string defaultProperty = "ObjectId";
        public virtual IDataProvider Provider { get{return null;} }
        public virtual object ObjectId { get { return Id; } }
        public virtual string TableIdentityColumn { get { return "ROWID"; } }
        
        public virtual string DefaultProperty
        {
        	get
        	{
        		return defaultProperty;
        	}
        	protected set 
        	{
        		defaultProperty = value;
        	}
        }    
        
        public AbstractRecord Parent {
        	get {        		
        		return parent;
        	}
        	set {
        		parent = value;
        	}
        }

        public bool IsDeserializing {
        	get {
        		return isDeserializing;
        	}
        	set {
        		isDeserializing = value;
        	}
        }
        
        public override bool Equals(object o)
        {
           	if( o == null )
        		return false;
        	else if( o.GetType() != this.GetType() )
        		return false;
        	else if( o is AbstractRecord )
        	{
				var r = (AbstractRecord)o;
        		if( this.id > 0 && r.id > 0 )
					return this.id == r.id;
				else
					return Object.ReferenceEquals (this, r);
        	}
        	return false;
        }
        
        public override int GetHashCode()
        {
        	return (this.GetType().FullName + this.id.ToString()).GetHashCode();
        }
        
       	public static bool operator ==( AbstractRecord a, AbstractRecord b )
        {
       	    if (System.Object.ReferenceEquals(a, b))
		    {
		        return true;
		    }

		    // If one is null, but not both, return false.
		    if (((object)a == null) || ((object)b == null))
		    {
		        return false;
		    }
        	return a.Equals(b);
        }
        
        public static bool operator != ( AbstractRecord a, AbstractRecord b )
        {
        	return !(a==b);
        }
        
        public override string ToString()
        {
            if (this[DefaultProperty] != null)
                return this[DefaultProperty].ToString();
            else
                return base.ToString();
        }

        public virtual string SelfString {
        	get {
        		return ToString();
        	}
        }
        
        public static float SingularHits {
        	get {
        		return singularHits;
        	}
        	set {
        		singularHits = value;
        	}
        }

        public static float PluralHits {
        	get {
        		return pluralHits;
        	}
        	set {
        		pluralHits = value;
        	}
        }

        public static float Misses {
        	get {
        		return misses;
        	}
        	set {
        		misses = value;
        	}
        }

        public static float Loads {
        	get {
        		return loads;
        	}
        	set {
        		loads = value;
        	}
        }

        public static float Hits {
        	get {
        		return hits;
        	}
        	set {
        		hits = value;
        	}
        }

        public static object[] emptyObjectArray = new object[] { };
        
		public static bool TypeIsRecordList(Type t)
        {
            if (t.Name == "IRecordList`1") return true;
            Type[] interfaces = t.GetInterfaces();
            foreach (Type iface in interfaces)
                if (iface.Name == "IRecordList`1")
                    return true;
            return false;
        }

        public static explicit operator int(AbstractRecord r)
        {
            return r.Id;
        }
		
        public static AbstractRecord Load(Type t, object id)
        {
			//log.DebugFormat("abstract loading through dynamic method type: {0} id: {1} id type {2}", t, id, id.GetType() );
            return (AbstractRecord)TypeLoader.InvokeGenericMethod(typeof(AbstractRecord),"Load",new Type[]{t},null,new Type[]{typeof(object)},new object[]{id});
        }

        protected static List<PropertyInfo> DiscoverDerivedProperties(Type modelType)
        {
        	List<PropertyInfo> properties = new List<PropertyInfo>();
        	while( modelType != null && modelType.GetCustomAttributes(typeof(IgnoreTypeAttribute), false ).Length == 0)
        	{
            	PropertyInfo[] derivedProperties = modelType.GetProperties(BindingFlags.Instance|BindingFlags.DeclaredOnly|BindingFlags.Public);
            	properties.AddRange( derivedProperties );
            	modelType = modelType.BaseType;
            }            
            return properties;
        }
        
        public virtual object Clone()
        {
			AbstractRecord r = MemberwiseClone() as AbstractRecord;
			r.SetId(0);
			r.persisted = false;
			r.originalValues = null;
        	return r;
        }
        
        bool isDeserializing = false;
        
        public virtual Dictionary<string,object> Serialize()
        {
        	Dictionary<string,object> h = new Dictionary<string,object>();
        	h["_type"] = this.GetType().FullName;
        	h["_id"] = this.id;
        	return h;
        }
        
        public virtual void Deserialize(Dictionary<string,object> h)
        {
        	log.Debug("calling Deserialize on AbstractRecord");
        }
        
        private bool disableValidation = false;
        public bool DisableValidation { get { return disableValidation; } set { disableValidation = value ; } }

        public Dictionary<RecordDefinition, AbstractRecord> LoadingContext {
        	get {
        		return loadingContext;
        	}
        	set {
        		loadingContext = value;
        	}
        }
        public bool IsStale {
    		get {
    			return isStale;
    		}
    	}
    	
		static AbstractRecord ()
		{
			try
			{
				HashSet<string> names = new HashSet<string> ();
				//get all derived types.
				//System.Console.WriteLine ("loading base types");
				foreach (Type t in TypeLoader.GetTypesOfBaseType (typeof(AbstractRecord)))
				{
					string n = GetDbSafeModelName (t);
					if (n == null)
						continue;
					if (names.Contains (n))
					{
						log.Warn ("Name: ", t, "New Type:", t, "Old Type:", GetTypeFromDbSafeName (n) );
						throw new InvalidOperationException ("Duplicate safe name detected.");
					}
					names.Add (n);
				}
				//System.Console.WriteLine ("done");
			}
			catch (Exception e)
			{
				log.Error ("Error initializing AbstractRecord!", e);
				throw new Exception ("Error initializing AbstractRecord!", e);
			}
		}
    	
        public virtual List<ValidationError> Validate(string path, List<ValidationError> errors){return errors;}
        
		public void ValidateAndThrow()
		{
			List<ValidationError> errors = Validate(string.Empty, new List<ValidationError> ());
			if( errors != null && errors.Count > 0 )
			{
				log.ErrorFormat("Validation failed for record: {0} ", this);
				foreach (var error in errors)
				{
					 log.Error (error);
				}
				throw new ValidationException("Validation error(s) occurred.", errors);
			}
		}

        public virtual void Authorize()
        {
        	//default authorize implementation
        	if( this is ILicensed )
        	{
        		License l = License.Load<License>(
        			new FilterInfo("ObjectType", this.GetType().FullName),
        			new FilterInfo("ObjectId", this.id ) );
        		if( l == null )
        			throw new UnauthorizedRecordAccessException("No licenses have been defined for this ILicensed record");        		
        		//valid license, invalid user.
        		throw new UnauthorizedRecordAccessException("User does not have access to this ILicensed record.");
        	}
        	
        	//non-licensed item
        	return;
        }

        public virtual int CompareTo (object obj)
        {
        	if( obj is AbstractRecord )
        	{
        		AbstractRecord r = (AbstractRecord)obj;
        		return id.CompareTo( r.id );
        	}
        	return 0;
        }
    }
}