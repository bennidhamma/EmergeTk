using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Web;
using MySql.Data.MySqlClient;
using EmergeTk.Model;
using System.Web.Script.Serialization;

namespace EmergeTk.Model.Providers
{
    public class MySqlProvider : DataProvider, IDataProvider
    {
        private static readonly EmergeTkLog queryLog = EmergeTkLogManager.GetLogger("MySqlQueries");
    	bool synchronizing = false;
    	bool outOfSync = false;
    	public bool Synchronizing {
        	get {
        		return synchronizing;
        	}
        }
        
        public bool OutOfSync {
        	get {
        		return outOfSync;
        	}
        }
        
        const string UidGenTableName = "uid";
	    const string LongStringDataType = "text";
		const string ShortStringDataType = "varchar(20)";
		const string StringDataType = "varchar(500)";
		const string IntDataType = "int";

		//IVersioned stmts
		const string IdentityColumnSignature = "`ROWID` INTEGER UNSIGNED NOT NULL PRIMARY KEY";
        const string CreateTableFormat = "CREATE TABLE `{0}` ( " + IdentityColumnSignature + " {1} )";
		
        const string InsertTableFormat = "REPLACE `{0}`({2}) VALUES( {1} );";
		const string DeleteFormat = "DELETE FROM `{0}` WHERE ROWID = {1}";
		 
		const string CreateTableNoRowIdFormat = "CREATE TABLE `{0}` ( {1} )";		
		const string UpdateTableFormat = "UPDATE `{0}` SET {1} WHERE ROWID = {2}";
        
        //Non-IVersioned stmts
		const string VersionedCreateTableFormat = "CREATE TABLE `{0}` ( `ROWID` INTEGER UNSIGNED NOT NULL, Version INTEGER UNSIGNED NOT NULL {1},PRIMARY KEY(`ROWID`) )";
		const string VersionedUpdateTableFormat = "UPDATE `{0}` SET {1} WHERE ROWID = {2} AND Version = {3}";
        
        const string DropFormat = "DROP TABLE `{0}`";
        const string RenameFormat = "ALTER TABLE `{0}` RENAME TO {1}";
        const string SelectColumnFormat = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}' AND COLUMN_NAME = '{2}'";

        // private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(MySqlProvider));
		
        private static readonly MySqlProvider provider = new MySqlProvider();
       
        static public MySqlProvider Provider { 
			get 
        	{
				if( DataProvider.DefaultProvider is MySqlProvider )
					return DataProvider.DefaultProvider as MySqlProvider;
        		return provider; 
        	}
        }

		string connectionString = ConfigurationManager.AppSettings["mysqlConnectionString"];
		public string ConnectionString {
			get {
				//log.Debug("got connection string: ", connectionString, ConfigurationManager.AppSettings["mysqlConnectionString"] );
				return connectionString;
			}
			set
			{
				connectionString = value;
			}
		}

		public void SetConnectionString(String cString) {
			connectionString = cString;
		}
		
		string dbName = null;
		public string DatabaseName {
			get {
				if( dbName == null )
				{
					MySqlConnection c = CreateConnection();
					dbName = c.Database;
					log.DebugFormat("Connection: {0}, dbname: {1}", c, dbName);
				}
				return dbName;
			}
		}

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(connectionString);
        }

        public DbConnection CreateNewConnection()
        {
            return CreateConnection();
        }

        public IDataParameter CreateParameter()
        {
            return new MySqlParameter();
        }
        
        T Execute<T>(string sql, SqlExecutionType mode) where T : class
        {
        	return Execute<T>(sql, mode, CreateConnection() );
        }

        public T Execute<T>(string sql, SqlExecutionType mode, MySqlConnection conn, params IDataParameter[] parms) where T : class
        {
#if DEBUG
        	log.Debug(string.Format("executing {0} in mode {1}", sql, mode ) );
           // queryLog.Debug(string.Format("executing {0} in mode {1}", sql, mode));
#endif
            StopWatch watch = new StopWatch("Execute<T>", this.GetType().Name);
            watch.Start();

			if( conn.State != ConnectionState.Open )
				conn.Open();
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.CommandText = sql;

            if (parms != null && parms.GetLength(0) > 0)
            {
                if (mode != SqlExecutionType.DataSet)
                {
                    watch.Stop();
                    throw new NotImplementedException(String.Format("MySqlProvider.Execute<T> not implemented with parameters for execution mode {0}",
                                                                    mode.ToString()));
                }

                cmd.CommandType = CommandType.StoredProcedure;

                foreach (IDataParameter parm in parms)
                {
                    MySqlParameter myParm = new MySqlParameter();
                    myParm.SourceColumn = parm.SourceColumn;
                    myParm.Direction = parm.Direction;
                    myParm.ParameterName = "?" + parm.ParameterName;
                    myParm.DbType = parm.DbType;
                    myParm.Value = parm.Value;
                    cmd.Parameters.Add(myParm);
                }

            }

            T retVal = null;
			try
			{
                watch.Lap("Done setting up, now executing");
	            switch (mode)
	            {
	                case SqlExecutionType.Scalar:
	                    retVal = (T)cmd.ExecuteScalar();
	                    break;
	                case SqlExecutionType.NonQuery:
	                    cmd.ExecuteNonQuery();
	                    break;
	                case SqlExecutionType.Reader:
	                    retVal = cmd.ExecuteReader() as T;
	                    break;
	                case SqlExecutionType.DataTable:
	                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
	                    DataTable t= new DataTable();
	                    da.Fill(t);
	                    retVal = t as T;
	                    break;
					 case SqlExecutionType.DataSet:
	                    da = new MySqlDataAdapter(cmd);
	                    DataSet ds = new DataSet();
	                    da.Fill(ds);
	                    retVal = ds as T;
	                    break;
				}
			}
			catch(Exception e )
			{
				log.Error( sql, Util.BuildExceptionOutput(e) );
                if (e is MySqlException && ((MySqlException)e).Number == 1062)
                {
                    // we want to distinguish and catch the duplicateRecordException.
                    throw new DuplicateRecordException(e as MySqlException);
                }
                throw new Exception("error executing sql", e);
            }
			finally
			{            
	            if (mode != SqlExecutionType.Reader)
	            {
	                cmd.Dispose();
	                conn.Close();
	                conn.Dispose();
	            }
                watch.Stop();
			}
            //TODO: need to release resources after reader is complete.
            return retVal;
        }

        public object ExecuteScalar(string sql)
        {
            return Execute<object>(sql, SqlExecutionType.Scalar);
        }
		
		public List<int> ExecuteVectorInt(string sql)
		{
			MySqlConnection conn = CreateConnection();
			IDataReader r = ExecuteReader(sql,conn);
			List<int> ts = new List<int>();
			while( r.Read() )
			{
				ts.Add(  r.GetInt32(0) );
			}
			conn.Dispose();
			return ts;
		}
        
        public object ExecuteScalar(string sql, MySqlConnection conn)
        {
            return Execute<object>(sql, SqlExecutionType.Scalar, conn);
        }

        public void ExecuteNonQuery(string sql)
        {
            Execute<object>(sql, SqlExecutionType.NonQuery);
        }
		
		public void ExecuteNonQuery(string sql, MySqlConnection conn)
        {
            Execute<object>(sql, SqlExecutionType.NonQuery, conn);
        }
		
		public void ExecuteReader(string sql, ReaderDelegate del)
		{
			using( MySqlConnection conn = CreateConnection() )
			{
				conn.Open();
				using( MySqlCommand comm = new MySqlCommand(sql, conn) )
				{
					log.Debug ("ExecuteReader exeucting sql: ", sql);
					using(IDataReader r = comm.ExecuteReader())
					{
						while(r.Read())
						{
							del(r);
						}
					}
					conn.Close();
				}
			}
		}
		
		public void ExecuteReader(string sql, MySqlConnection conn, ReaderDelegate del)
		{
			using( MySqlCommand comm = new MySqlCommand(sql, conn) )
			{
				using(IDataReader r = comm.ExecuteReader())
				{
					while(r.Read())
					{
						del(r);
					}
				}
			}
		}
        
        public IDataReader ExecuteReader(string sql, MySqlConnection conn)
        {
			return Execute<MySqlDataReader>(sql, SqlExecutionType.Reader, conn);
        }

        public DataTable ExecuteDataTable(string sql)
        {
            return Execute<DataTable>(sql, SqlExecutionType.DataTable);
        }
        
        public DataTable ExecuteDataTable(string sql, MySqlConnection conn )
        {
            return Execute<DataTable>(sql, SqlExecutionType.DataTable, conn);
        }

        public DataSet ExecuteDataSet(string sql)
        {
            return Execute<DataSet>(sql, SqlExecutionType.DataSet, CreateConnection());
        }
		
		public DataSet ExecuteDataSet(string sql, IDataParameter[] parms)
        {
            return Execute<DataSet>(sql, SqlExecutionType.DataSet, CreateConnection(), parms);
        }
		
		public DataSet ExecuteDataSet(string sql, MySqlConnection conn, IDataParameter[] parms)
        {
            return Execute<DataSet>(sql, SqlExecutionType.DataSet, conn, parms);
        }

        public IRecordList<T> Load<T>() where T : AbstractRecord, new()
        {
            return Load<T>("","ROWID");
        }

        public IRecordList<T> Load<T>(params SortInfo[] sortInfos) where T : AbstractRecord, new()
        {
            return Load<T>(string.Empty, Util.Join(sortInfos));
        }

        public IRecordList<T> Load<T>(string whereClause, string orderByClause) where T : AbstractRecord, new()
        {
            return Load<T>(whereClause, orderByClause, "*");
        }

        public IRecordList<T> Load<T>(List<int> ids) where T : AbstractRecord, new()
        {
            //preserve order
            //get all records from cache first
            //then get records that aren't in cache as ONE database call
            //
            RecordList<T> records = new RecordList<T>();
			bool isDerived = AbstractRecord.IsDerived(typeof(T));
			string name = null;
			if( ! isDerived )
			{
	            name = GetTableNameT<T>();
	            if (name == null)
	            {
	                return records;
	            }
			}
			else
			{
				//name only used for caching at this point.
				name = typeof(T).FullName;
			}	

            // keep a map of who got found in cache, and who didn't.
            Dictionary<int, T> recordMap = new Dictionary<int, T>();

            List<int> unCachedIds = new List<int>();

            foreach (int id in ids)
            {
                String cacheKey = name + ".rowid = " + id.ToString();
                T r = (T) CacheProvider.Instance.GetLocalRecord(cacheKey);
                recordMap[id] = r;  // add to the recordMap, whether it's a hit or miss.
                if (r == null)
                {
                    unCachedIds.Add(id);
                }
            }

            if (unCachedIds.Count > 0)
            {
				if (AbstractRecord.IsDerived(typeof(T)))
				{
					foreach( int id in unCachedIds )
					{
						Type t = GetTypeForId(id);
						if( t == null )
						{
							log.Warn("Did not find type for id " + id);
							continue;
						}
						T r = AbstractRecord.Load(t,id) as T;
						
						if( r != null )
						{
							recordMap[r.Id] = r;
							AbstractRecord.PutRecordInCache(r);
						}
						else
							log.WarnFormat("Did not load a record for id {0} and type {1}", id, t);
					}
				}
				else
				{
	                String sql = String.Format("SELECT * FROM {0} WHERE ROWID IN ({1});", EscapeEntity(name), Util.JoinToString<int>(unCachedIds, ","));
	                DataTable result = MySqlProvider.Provider.ExecuteDataTable(sql);
	
	                for (int i = 0; i < result.Rows.Count; i++)
	                {
	                    T r = AbstractRecord.LoadFromDataRow<T>(result.Rows[i]);
	                    recordMap[r.Id] = r;
	                    AbstractRecord.PutRecordInCache(r);
	                }
				}
            }

            foreach (KeyValuePair<int, T> kvp in recordMap)
            {
                if (kvp.Value != null)
                    records.Add(kvp.Value);
            }

            return records;
        }

        private String GetTableNameT<T>() where T : AbstractRecord, new()
        {
			string name = AbstractRecord.GetDbSafeModelName(typeof(T));
            if (!TableExists(name))
            {
                log.Warn("TablenotFoundException ModelName:" + name );
                return null;
            }
            return name;
        }

        public IRecordList<T> Load<T>(string whereClause, string orderByClause, String selectColumns ) where T : AbstractRecord, new()
        {
            if (whereClause != null && whereClause.Length > 0)
            {
                whereClause = " WHERE " + whereClause;
            }
            if (orderByClause != null && orderByClause.Length > 0)
            {
                orderByClause = " ORDER BY " + orderByClause;
            }
            RecordList<T> records = new RecordList<T>();
            String name = GetTableNameT<T>();
            if (name == null)
                return records;  // we've already logged this.

			bool wildcard = selectColumns == "*";
			string sql = null;
			string selectCols = String.IsNullOrEmpty(selectColumns) ? String.Empty : selectColumns + ", ";
           
			if( wildcard )
				sql = "SELECT " + selectCols + " ROWID FROM " + EscapeEntity(name) + whereClause + orderByClause;
			else
				sql = "SELECT * FROM " + EscapeEntity(name) + whereClause + orderByClause;
			
			DataTable result = ExecuteDataTable(sql);

            for (int i = 0; i < result.Rows.Count; i++)
            {
                T r;
				string cacheKey = name + ".rowid = " + result.Rows[i]["ROWID"];

				r = (T)CacheProvider.Instance.GetLocalRecord(cacheKey);
				if( r == null )
				{
	                if( selectColumns != String.Empty )
					{
				       r = AbstractRecord.LoadFromDataRow<T>(result.Rows[i]);
					}
	                else
					{
				        r = AbstractRecord.Load<T>(Convert.ToInt32(result.Rows[i]["ROWID"]));
					}
					if( wildcard || selectCols == string.Empty )
					{
						//store this object in the local cache if we have a full version of the objcet.
						//format is name.rowid = N
						//CacheProvider.Instance.Set(name + ".rowid = " + r.Id, r);
                        AbstractRecord.PutRecordInCache(r);
					}
				}

				if( r != null )
                	records.Add(r);
            }
			records.Clean = true;
            return records;
        }
        
		public IRecordList<T> Load<T>(params FilterInfo[] filterInfos) where T : AbstractRecord, new() 
        {
            return Load<T>(MySqlFilterFormatter.BuildWhereClause(filterInfos), string.Empty);
        }

        public IRecordList<T> Load<T>(FilterInfo[] filterInfos, SortInfo[] sortInfos ) where T : AbstractRecord, new()
        {
            return Load<T>(MySqlFilterFormatter.BuildWhereClause(filterInfos), Util.Join(sortInfos));
        }
        
        public new int GetRowCount<T>()  where T : AbstractRecord, new()
        {
        	throw new NotImplementedException();
        }
        
        public int GetNewId(string typeName)
        {
        	string table = UidGenTableName;
        	if( string.IsNullOrEmpty(typeName ) )
        	{
        		throw new ArgumentNullException("Must specify a valid type name.");
        	}
			int newId = Convert.ToInt32( ExecuteScalar( string.Format( "insert into `{0}` (type) values ('{1}'); SELECT LAST_INSERT_ID();", UidGenTableName, typeName ) ) );
			
			log.Debug("GetNewId: ", newId );
			
			return newId;
        }
		
		public Type GetTypeForId( int id )
		{
			string type = (string) ExecuteScalar("SELECT type FROM uid WHERE ID = " + id);
			if( type == null )
				return null;
			else
			{
				return AbstractRecord.GetTypeFromDbSafeName(type);
			}
		}
        
        public int GetLatestVersion( AbstractRecord r )
        {
			if( r == null )
				throw new ArgumentNullException("r", "Record cannot be null." );
        	return Convert.ToInt32( ExecuteScalar(string.Format( "SELECT Version FROM `{0}` WHERE ROWID = {1}", r.DbSafeModelName, r.Id ) ) );
        }
		
		public int GetLatestVersion( string modelName, int id )
		{
			return Convert.ToInt32( ExecuteScalar(string.Format( "SELECT Version FROM `{0}` WHERE ROWID = {1}", modelName, id ) ) );
		}

        
        //INVERSION OF CONTROL METHODS

        public void Save(AbstractRecord record, bool SaveChildren)
        {
            using (MySqlConnection conn = CreateConnection())
            {
                conn.Open();
                this.Save(record, SaveChildren, true, conn);
            }
        }
        public void Save(AbstractRecord record, bool SaveChildren, bool IncrementVersion, DbConnection conn)
		{
			log.Debug("saving record", this );
            if( record.Id == 0 && record.TableIdentityColumn != "ROWID" )
			{
				 AbstractRecord r = AbstractRecord.Load(record.GetType(),new FilterInfo(
					record.TableIdentityColumn, record.Value, FilterOperation.Equals ) );
				if( r != null )
				{
					throw new Exception(string.Format("Cannot save {0}({{1}:{2}) because it will collide with existing record {4}({{5}:{6}) }",
						record.DbSafeModelName, record.TableIdentityColumn, record.DefaultProperty,
						r.DbSafeModelName, r.TableIdentityColumn, r.DefaultProperty ));
				}
			}
			
			string sql = "";
			List<string> parameterKeys = new List<string>();
			List<string> keys = new List<string>();
			bool invalidateCache = false;
			if( record.Id == 0 )
			{
				record.EnsureId();
			}
			else
			{				
				invalidateCache = true;
			}
			
			MySqlCommand comm = new MySqlCommand();

			keys.Add( "ROWID" );
			parameterKeys.Add( record.Id.ToString() );
			
			if( record is IVersioned && IncrementVersion)
			{
				keys.Add( "Version" );
				parameterKeys.Add( (record.Version + 1).ToString() );
			}
			         
            bool persisted = record.Persisted;   
        	foreach( ColumnInfo col in record.Fields )
			{
				if (col.ReadOnly || col.DataType == DataType.Volatile || col.DataType == DataType.RecordList || GetSqlTypeFromType(col, record.DbSafeModelName) == null )
                    continue;
                
				//log.Debug("adding column for SAVE ", record.DbSafeModelName, col.Name );
				keys.Add( Util.Surround(col.Name, "`" ) );
				
                if (col.Type.IsSubclassOf(typeof(AbstractRecord)))
                {
                    AbstractRecord r = record[col.Name] as AbstractRecord;
					record.AddToLoadedProperties (col.Name);
                    //TODO: there should be an option to decide if they want to allow adding nulls or not here.
                    if (r != null)
                    {
                        if( SaveChildren )                
							r.Save();
						comm.Parameters.Add( new MySqlParameter("?"+ col.Name,r.ObjectId) );
						parameterKeys.Add("?" + col.Name);
                        record.SetOriginalValue(col.Name, r.Id);
                    }
                    else
					{
						comm.Parameters.Add( new MySqlParameter("?"+ col.Name,null) );
						parameterKeys.Add("?" + col.Name);
					}
           	    }
                else //string, enum, etc.
                {
					object v = record[col.Name];
					if (col.DataType != DataType.Json || v == null)
					{
						comm.Parameters.Add( new MySqlParameter("?"+ col.Name,v ) );
					}
					else
					{
						comm.Parameters.Add( new MySqlParameter("?"+ col.Name, JSON.Serializer.Serialize (v) ) );
					}
					parameterKeys.Add("?" + col.Name);
                    record.SetOriginalValue(col.Name, record[col.Name]);
                }
			}

			if( ! persisted || synchronizing )
            	sql = string.Format(InsertTableFormat, record.DbSafeModelName, Util.Join(parameterKeys, ","), Util.Join(keys) );
            else
            {            	
            	List<string> updateFields = new List<string>();
            	for( int i = 0; i < keys.Count; i++ )
            	{
            		updateFields.Add(string.Format( "{0} = {1}", keys[i], parameterKeys[i] ) );
            	}
            	if( ! ( record is IVersioned ) || !IncrementVersion)
            	{
            		sql = string.Format(UpdateTableFormat, record.DbSafeModelName, string.Join( ",", updateFields.ToArray() ), record.Id  );
            	}
            	else
            	{
            		sql = string.Format(VersionedUpdateTableFormat, record.DbSafeModelName, string.Join( ",", updateFields.ToArray() ), record.Id, record.Version  );
            	}
            }
            StopWatch watch = new StopWatch("MySqlProvider.Save", this.GetType().Name);
            watch.Start();
			comm.CommandText = sql;
			//log.Debug( sql ); 
            comm.Connection = (MySqlConnection) conn;
			comm.Prepare();
			log.Debug( comm.CommandText );
			
			foreach( MySqlParameter p in comm.Parameters )
			{
				log.Debug(string.Format("{0}: {1}", p.ParameterName, p.Value) );	
			}
			
			int result = -1;
            try
            {
                //we may get an exception here if the model has changed. if so, resynchronize.
                result = comm.ExecuteNonQuery();
                //log.Debug("result is ", result );
            }
            catch (Exception e)
            {
                log.Error("Error saving record", record, Util.BuildExceptionOutput(e));

                string preserveAtAllCosts = Setting.Get("preserveAtAllCosts").DataValue;
                if (preserveAtAllCosts != null && !bool.Parse(preserveAtAllCosts))
                {
                    SynchronizeModel(record.GetType());
                    //try again, if another exception, let it throw out.
                    result = comm.ExecuteNonQuery();
                }
                else
                {
                    //we are past the point where it's acceptable to re-create the tables.
                    throw new Exception("error saving record", e);
                }

            }
            finally
            {
                watch.Stop();
            }
			
			if( record is IVersioned && IncrementVersion)
			{
				if( result < 1 )
				{
					log.Error("No record was modified.  ", result, record, record.Version );
					throw new VersionOutOfDateException( "No record was modified.  Probably attempting to save an old version of record."  );
				}
				record.Version++;
            }
            
			if( SaveChildren )
			{
				foreach( ColumnInfo col in record.Fields )
			    {
				    if( AbstractRecord.TypeIsRecordList(col.Type) )
				    {
						TypeLoader.InvokeGenericMethod
							(typeof(AbstractRecord),"SaveChildRecordList",new Type[]{col.ListRecordType},record,new object[]{col.Name,record[col.Name]});
				    }
			    }
			}

            if (invalidateCache)
                CacheProvider.Instance.Remove(record);

            record.UnmarkAsStale();
			
			string cacheKey = record.CreateStandardCacheKey();
       	    CacheProvider.Instance.PutLocal(cacheKey, record);
		}

	
		public void SaveSingleRelation( string childTableName, string parent_id, string child_id ) 
		{
			// TODO: check for already saved?
			try
			{	
				
				provider.ExecuteNonQuery(string.Format("INSERT IGNORE INTO {0} VALUES( '{1}', '{2}' )", childTableName, parent_id, child_id));
			}
			catch( Exception ex )
			{
			
				if( ex.InnerException != null && ex.InnerException.Message.Contains("Column count doesn't match value count" ) )
				{
					if( ColumnList(childTableName).Contains("ROWID") )
					{
						provider.ExecuteNonQuery(GenerateRemoveColStatement("ROWID", childTableName));					
						SaveSingleRelation(childTableName, parent_id, child_id);
					}
				}
			}
		}
		
		public void RemoveSingleRelation( string childTableName, string parent_id, string child_id ) 
		{
			provider.ExecuteNonQuery(string.Format("DELETE FROM {0} WHERE Parent_Id = '{1}' AND Child_Id = '{2}'", childTableName, parent_id, child_id )); 
		}
		
		public void RemoveRelations( string childTableName, string parent_id )
		{
			provider.ExecuteNonQuery(string.Format("DELETE FROM {0} WHERE Parent_Id = '{1}'", childTableName, parent_id));	
		}

		public void Delete( AbstractRecord r )
		{
			ExecuteNonQuery(string.Format(DeleteFormat, r.DbSafeModelName, r.Id));
		}
		
		public void SynchronizeModel(Type t)
        {
        	log.Debug("attempting to synchronize model ", t );
        	MethodInfo mi = this.GetType().GetMethod("SynchronizeModelType", BindingFlags.Public | BindingFlags.Instance);
        	mi = mi.MakeGenericMethod(t);
        	mi.Invoke(this, null);
        }
		
		public void SynchronizeModelType<T>() where T : AbstractRecord, new()
        {
        	synchronizing = true;
        	int oldTimeout = 0;
        	if( HttpContext.Current != null )
        	{
        		oldTimeout = HttpContext.Current.Server.ScriptTimeout;
        		HttpContext.Current.Server.ScriptTimeout = 10000;
        	}
        	T record = new T();
            record.EnsureTablesCreated();
            //TODO: flush cache
            IRecordList<T> oldRecords = Load<T>();
            try
            {
            	ExecuteNonQuery(string.Format(RenameFormat, record.DbSafeModelName, record.DbSafeModelName + "_backup"));
            }           
            catch( Exception e )
            {
            	throw new Exception("BAD ERROR! Attempting to rename table on backup, and a failure occurred (is there already a backup table?)!  Please shutdown the server and examine table: " + record.DbSafeModelName,e );            	
            }
            existingTables[record.DbSafeModelName] = false;
            try
            {
            	record.EnsureTablesCreated();
	            if (oldRecords != null && oldRecords.Count > 0)
	            {
	                foreach (T r in oldRecords)
	                {
	                	
	                    r.Save();
	                }
	            }
	        }
	        catch( Exception e )
	        {
	        	//need to restore
	        	ExecuteNonQuery(string.Format(DropFormat, record.DbSafeModelName));
	        	ExecuteNonQuery(string.Format(RenameFormat, record.DbSafeModelName + "_backup", record.DbSafeModelName));	     
	        	outOfSync = true;
	        	
	        	log.Error( "BACKUP ERROR!", Util.BuildExceptionOutput( e ) );
	        	throw new Exception( "Synchronization failed. Database may be out of sync.  Backup attempted.", e );
	        }
	        finally
	        {
	        	if( HttpContext.Current != null )
	        	{
	        		HttpContext.Current.Server.ScriptTimeout = oldTimeout;
	        	}
	        }
	        //everything went well.  drop the backup table.
	        ExecuteNonQuery(string.Format(DropFormat, record.DbSafeModelName + "_backup"));
            record.EnsureTablesCreated();
            synchronizing = false;
        }
        
        public string GetSqlType( DataType dt )
        {
        	switch( dt )
        	{
        	case DataType.Text:
        		return "varchar(256)";
        	}
        	return null;
        }
        
        public void CreateTable(Type modelType, string tableName)
        {
			if ( LowerCaseTableNames ) tableName = tableName.ToLower();
            ArrayList pairs = new ArrayList();
            ColumnInfo[] columns = ColumnInfoManager.RequestColumns(modelType);
            foreach (ColumnInfo col in columns )
            {
                if (AbstractRecord.TypeIsRecordList(col.Type))
                {
                    CreateChildTable(tableName + "_" + col.Name, col);
                }
                else
                {	
					AddColumnPair( pairs, tableName, col );
                }
            }
				
            string propString = pairs.Count == 0 ? String.Empty : "," + Util.Join(pairs, ", ");
            if( modelType.GetInterface("IVersioned") != null )
				ExecuteNonQuery(string.Format(VersionedCreateTableFormat, tableName, propString));            
            else
            	ExecuteNonQuery(string.Format(CreateTableFormat, tableName, propString));
            
           	RegisterTable(tableName);
        }
		
		private void AddColumnPair( ArrayList pairs, string tableName, ColumnInfo col ) 
		{
			string dataType = GetSqlTypeFromType(col, tableName);
			//log.Debug("adding column for CREATE ", tableName,  col.Name, dataType );
			if (dataType != null) 
			{
				pairs.Add(Util.Surround(col.Name, "`") + " " + dataType);
			}	
		}
        
        public void RegisterTable( string tableName )
        {
            existingTables[tableName] = true;
        }

        public string EscapeEntity(string entity)
        {
            return Util.Surround(entity, "`");
        }
        
        public string GetSqlTypeFromType (ColumnInfo col, string tableName)
        {
        	if (LowerCaseTableNames)
        		tableName = tableName.ToLower ();
        	string dataType = null;
        	//"int";
        	if (col.Type == typeof(string))
            {
        		if (col.DataType == DataType.LargeText || col.DataType == DataType.Xml)
        			dataType = LongStringDataType;
                else if (col.DataType == DataType.SmallText)
        			dataType = ShortStringDataType;
        		else
        			dataType = StringDataType;
        	}
            else if (col.Type == typeof(int) || col.Type == typeof(int?))
            {
        		dataType = "int(11)";
        	}
			else if (col.Type == typeof(decimal) || col.Type == typeof(decimal?))
            {
        		dataType = "decimal(20,2)";
        	}
			else if (col.Type == typeof(float) || col.Type == typeof(float?))
            {
        		dataType = "float";
        	}
			else if (col.Type == typeof(double) || col.Type == typeof(double?))
            {
        		dataType = "double";
        	}
			else if (col.Type == typeof(DateTime) || col.Type == typeof(DateTime?))
            {
        		dataType = "datetime";
        	}
			else if (col.Type == typeof(TimeSpan) || col.Type == typeof(TimeSpan?))
            {
            	dataType = "bigint(20)";
            }
			else if (col.Type.IsSubclassOf(typeof(Enum)))
            {
                dataType = "varchar(128)";
            }
            else if (col.Type.IsSubclassOf(typeof(AbstractRecord)))
            {
                dataType = "int(11)";
            }
			else if (col.Type == typeof(bool) || col.Type == typeof(bool?))
            {
        		dataType = "tinyint(1)";
        	}
			else if (col.Type == typeof(long) || col.Type == typeof(long?))
			{
        		dataType = "bigint";
        	}
            else if (AbstractRecord.TypeIsRecordList (col.Type))
            {
        		// TODO: this may be bad to create a child table here
        		CreateChildTable (tableName + "_" + col.Name, col);
            }
			else
			{
				dataType = "text";
			}			

            return dataType;
        }

		public void CreateChildTable(string tableName, ColumnInfo col)
        {
		   if ( LowerCaseTableNames ) tableName = tableName.ToLower();
           if (!TableExists(tableName) )
           {
				ExecuteNonQuery(string.Format(CreateTableNoRowIdFormat, tableName, "Parent_Id int, Child_Id int"));		
				existingTables[tableName] = true;

                CreateChildTableIndex(tableName);
            }
        }

        #region publicly_exposed_jointable_index_helpers
        // N.B. these are exposed publicly even though they are NOT part of the DataProvider interface, because it makes
        // it easy to write tests to verify the SQL Unique Index generating code for the
        // ORM.
        public String GenerateChildTableIndexName(String tableName)
        {
            return String.Format("IX_{0}_PARENTID_CHILDID", tableName.ToUpper());
        }

        public bool ChildTableIndexExists(String tableName, String indexName)
        {
            long indexExists = 1;
            try
            {

                String sql = String.Format("SELECT COUNT(1) FROM information_schema.statistics WHERE table_name = '{0}' AND index_name = '{1}';", tableName, indexName);
                indexExists = (long)this.ExecuteScalar(sql);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error trying to determine if index {0} exists on table {1}, err = {2}, stack = {3}, will refrain from creating it since we don't know",
                                indexName, tableName, ex.Message, ex.StackTrace);

                indexExists = 1;  // just pretend the index exists; the main thing is not to hold things up.
            }

            return indexExists == 0 ? false : true;
        }
        #endregion

        private void CreateChildTableIndex(String tableName)
        {
            String indexName = GenerateChildTableIndexName(tableName);
            if (!ChildTableIndexExists(tableName, indexName))
            {
                try
                {
                    this.ExecuteNonQuery(String.Format("ALTER IGNORE TABLE `{0}` ADD UNIQUE INDEX `{1}` (Parent_Id, Child_Id);", tableName, indexName));
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("Error creating index {0} on table {1}, error = {2}, stack = {3}, continuing on",
                                     indexName, tableName, ex.Message, ex.StackTrace);
                }
            }
        }

        
        Dictionary<string, bool> existingTables;
		public bool TableExists( string name )
		{
			if ( LowerCaseTableNames ) name = name.ToLower();
            if (existingTables == null) 
            	existingTables = new Dictionary<string, bool>();
            else if (existingTables.ContainsKey(name) ) 
            	return existingTables[name];
            return existingTables[name] = Convert.ToBoolean(ExecuteScalar("SELECT COUNT(*) FROM information_schema.TABLES WHERE Table_Schema = '" + DatabaseName + "' and Table_Name = '" + name + "'"));
		}
		
		public bool TableExistsNoCache( string name ) {
			if ( LowerCaseTableNames ) name = name.ToLower();
			return Convert.ToBoolean(ExecuteScalar("SELECT COUNT(*) FROM information_schema.TABLES WHERE Table_Schema = '" + DatabaseName + "' and Table_Name = '" + name + "'"));
		}
		
		public List<string> ColumnList( string tableName )
		{
			if ( LowerCaseTableNames ) tableName = tableName.ToLower();
			if ( ! TableExists( tableName ) )
				return null;
			else
			{
				DataTable dt = ExecuteDataTable("SELECT SQL_NO_CACHE Column_Name FROM information_schema.COLUMNS WHERE Table_Schema = '" 
				                 + DatabaseName + "' and Table_Name = '" + tableName + "'");
				List<string> result = new List<string>();
				foreach( DataRow r in dt.Rows )
				{
					result.Add( r[0].ToString() );	
				}
				return result;
			}
		}

		public DataTable GetColumnTable( string tableName )
		{
			if ( LowerCaseTableNames ) tableName = tableName.ToLower();
			if ( ! TableExists( tableName ) )
				return null;
			else
			{
				DataTable dt = ExecuteDataTable("SELECT Column_Name, Column_Type FROM information_schema.COLUMNS WHERE Table_Schema = '" 
				                 + DatabaseName + "' and Table_Name = '" + tableName + "'");
				return dt;
			}
		}
		
		public List<string> TableList() 
		{
			DataTable dt = ExecuteDataTable("SELECT SQL_NO_CACHE Table_Name FROM information_schema.TABLES WHERE Table_Schema = '" + DatabaseName + "'");			
			List<string> result = new List<string>();
			foreach ( DataRow r in dt.Rows )
			{
				result.Add( r[0].ToString() );
			}
			return result;
		}
		
		// wrap this, because we don't have DatabaseName at construction time
		private string AlterStatement {
			get {
				return "ALTER TABLE " + DatabaseName + ".`{0}` {1}{2}{3};";	
			}
		}
		
		public string GenerateAddColStatement( ColumnInfo col, string table )
		{
			if ( LowerCaseTableNames ) table = table.ToLower();
			return String.Format(AlterStatement, table, "ADD ", "`" + col.Name + "`", " " + GetSqlTypeFromType(col, table));
		}
		
		public string GenerateRemoveColStatement( string col, string table )
		{
			if ( LowerCaseTableNames ) table = table.ToLower();
			return String.Format(AlterStatement, table, "DROP ", "`" + col + "`", "");
		}
		
		public string GenerateFixColTypeStatement( ColumnInfo col, string table )
		{
			if ( LowerCaseTableNames ) table = table.ToLower();
			return String.Format(AlterStatement, table, "MODIFY ", "`" + col.Name + "`", " " + GetSqlTypeFromType(col, table));
		}
				
		public string GenerateAddIdColStatement(string table) {
			if ( LowerCaseTableNames ) table = table.ToLower();
			return String.Format(AlterStatement, table, "ADD", " ", IdentityColumnSignature);
		}
		
		public string GenerateModifyIdColStatement(string table) {
			if ( LowerCaseTableNames ) table = table.ToLower();
			return String.Format(AlterStatement, table, "MODIFY", " ", IdentityColumnSignature);
		}
		
		private string GenerateTableFormat 
		{
			get {
				return "CREATE TABLE " + DatabaseName + ".`{0}` ( " + IdentityColumnSignature + ", {1} );";
			}
		}
		
		public string GenerateAddTableStatement( Type addTable ) 
		{	
			string result = "";
			ArrayList pairs = new ArrayList();
			string tableName = ( Activator.CreateInstance( addTable ) as AbstractRecord ).DbSafeModelName;
			
			ColumnInfo[] columns = ColumnInfoManager.RequestColumns( addTable );
			foreach ( ColumnInfo c in columns ) 
			{
				// ignore record lists
				if ( ! AbstractRecord.TypeIsRecordList(c.Type) )	
					AddColumnPair( pairs, tableName, c );
			}
			string propString = Util.Join(pairs, ", ");
				
			return result + string.Format(GenerateTableFormat, tableName, propString);
		}
		
		private string GenerateChildTableFormat
		{
			get {
				return "CREATE TABLE " + DatabaseName + ".`{0}` ( {1} );";
			}	
		}
		
		public string GenerateAddChildTableStatement( string tableName, bool isDerived )
		{
			if ( LowerCaseTableNames ) tableName = tableName.ToLower();
			return string.Format(GenerateChildTableFormat, tableName, "Parent_Id int, Child_Id int");
		}
		
		public string CheckIdColumn( string table ) 
		{
			DataTable dt = ExecuteDataTable("SELECT Column_Type, Is_Nullable, Column_Key, Extra FROM information_schema.COLUMNS WHERE Table_Schema = '" 
			                 + DatabaseName + "' and Table_Name = '" + table + "' and Column_Name='ROWID'");
			
			if (dt.Rows.Count == 0)
				return GenerateAddIdColStatement( table );
		
			DataRow r = dt.Rows[0];
			
			bool goodIdCol = (r[0] as string).ToLower().Equals("int(10) unsigned") 
				&& (r[1] as string).ToLower().Equals("no") 
					&& (r[2] as string).ToLower().Equals("pri") 
					&& (r[3] as string).ToLower().Equals("auto_increment");
			
			if ( ! goodIdCol )
				return GenerateModifyIdColStatement( table );
			else
				return "";
		}
		  
        public string BuildWhereClause(FilterInfo[] filters )
        {
        	return MySqlFilterFormatter.BuildWhereClause( filters );
        }
        
        public string GetIdentityColumn()
        {
        	return "ROWID";
        }
		
		public string GetCommentCharacter() 
		{
			return "#";	
		}
	}
}
