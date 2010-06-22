using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

#if MONO
using Mono.Data.SqliteClient;
#else
using System.Data.SQLite;
#endif

namespace EmergeTk.Model.Providers
{
    public enum SqlExecutionType
    {
        Scalar,
        NonQuery,
        Reader,
        DataTable,
		DataSet
    }
    public sealed class SQLiteProvider : DataProvider, IDataProvider
    {
        private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(SQLiteProvider));

        private static readonly SQLiteProvider provider = new SQLiteProvider();

        static public SQLiteProvider Provider { get { return provider; } }

        public bool Synchronizing
        {
            get { throw new NotImplementedException(); }
        }

        static bool outOfSync = false; 
        public bool OutOfSync
        {
            get { throw new NotImplementedException(); }
        }

        public DbConnection CreateNewConnection()
        {
            throw new NotImplementedException();
        }

        public void Save(AbstractRecord r, bool SaveChildren, bool IncrementVersion, DbConnection conn)
        {
            throw new NotImplementedException();
        }

        public IDataParameter CreateParameter()
        {
            throw new NotImplementedException();
        }


        public IRecordList<T> Load<T>(List<int> ids) where T : AbstractRecord, new()
        {
            throw new NotImplementedException();
        }

        public static T Execute<T>(string sql, SqlExecutionType mode) where T : class
        {
            if (outOfSync)
            {
                throw new OperationCanceledException("SQLiteRecord is OutOfSync.  Cannot continue.");
            }
            string dbPath = Util.RootPath + "data.db";
            IDbCommand cmd = null;
#if MONO
				SqliteConnection conn = new SqliteConnection("Data Source=" + dbPath);
#else
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbPath);
#endif
            try
            {
                //System.Console.WriteLine("dbPath:" + dbPath);
                conn.Open();
                cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                T retVal = null;

                log.Debug(string.Format("executing {0} in mode {1}", sql, mode));
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
#if MONO
	                		SqliteDataAdapter da = new SqliteDataAdapter(cmd as SqliteCommand);
#else
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd as SQLiteCommand);
#endif

                        DataTable t = new DataTable();
                        da.Fill(t);
                        retVal = t as T;
                        break;
                }

                if (mode != SqlExecutionType.Reader)
                {
                    cmd.Dispose();
                    conn.Close();
                    conn.Dispose();
                }
                return retVal;
            }
            catch (Exception e)
            {
                if (cmd != null) cmd.Dispose();
                if (conn != null) { conn.Close(); conn.Dispose(); }
                throw new Exception(string.Format("Error {0}, executing SQLite query '{1}'", e.Message, sql), e);
            }
            //TODO: need to release resources after reader is complete.
        }

        public object ExecuteScalar(string sql)
        {
            return Execute<object>(sql, SqlExecutionType.Scalar);
        }
		
		public List<int> ExecuteVectorInt(string sql)
		{
			throw new NotImplementedException();
		}

        public void ExecuteNonQuery(string sql)
        {
            Execute<object>(sql, SqlExecutionType.NonQuery);
        }

        public void ExecuteReader(string sql, ReaderDelegate del)
        {
			throw new NotImplementedException();
        }

        public DataTable ExecuteDataTable(string sql)
        {
            return Execute<DataTable>(sql, SqlExecutionType.DataTable);
        }
		
		public DataSet ExecuteDataSet(string sql)
        {
            throw new NotImplementedException();
        }

        public DataSet ExecuteDataSet(string sql, IDataParameter[] parms)
        {
            throw new NotImplementedException();
        }

        public IRecordList<T> Load<T>() where T : AbstractRecord, new()
        {
            return Load<T>("", "ROWID");
        }

        public IRecordList<T> Load<T>(params SortInfo[] sortInfos) where T : AbstractRecord, new()
        {
            return Load<T>(string.Empty, Util.Join(sortInfos));
        }

        public IRecordList<T> Load<T>(string whereClause, string orderByClause) where T : AbstractRecord, new()
        {
            return Load<T>(whereClause, orderByClause, false);
        }

        public IRecordList<T> Load<T>(string whereClause, string orderByClause, bool FastLoad) where T : AbstractRecord, new()
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
            T t = new T();

            if (!TableExists(t.ModelName))
                return records;

            string star = FastLoad ? ", *" : "";

            DataTable result = SQLiteProvider.Provider.ExecuteDataTable("SELECT ROWID" + star + " FROM " + t.ModelName + whereClause + orderByClause);

            for (int i = 0; i < result.Rows.Count; i++)
            {
                T r;
                if (FastLoad)
                    r = AbstractRecord.LoadFromDataRow<T>(result.Rows[i]);
                else
                    r = AbstractRecord.Load<T>("ROWID", Convert.ToInt32(result.Rows[i]["ROWID"]));

                records.Add(r);
            }

            return records;
        }

        public IRecordList<T> Load<T>(string whereClause, string orderByClause, String selectColumns) where T : AbstractRecord, new()
        {
            throw new NotImplementedException();
        }

        public IRecordList<T> Load<T>(params FilterInfo[] filterInfos) where T : AbstractRecord, new()
        {
            return Load<T>(Util.Join(filterInfos, " AND "), string.Empty);
        }

        public IRecordList<T> Load<T>(FilterInfo[] filterInfos, SortInfo[] sortInfos) where T : AbstractRecord, new()
        {
            return Load<T>(Util.Join(filterInfos, " AND "), Util.Join(sortInfos));
        }

        public int GetNewId(string type)
        {
            throw new NotImplementedException();
        }

        public void Save(AbstractRecord record, bool SaveChildren)
        {
            throw new NotImplementedException();
        }

        public void Delete(AbstractRecord r)
        {
            throw new NotImplementedException();
        }

        public void SynchronizeModel(Type t)
        {
            throw new NotImplementedException();
        }

        public void SynchronizeModelType<T>() where T : AbstractRecord, new()
        {
            throw new NotImplementedException();
        }

        public string GetSqlType(DataType dt)
        {
            throw new NotImplementedException();
        }

        public void CreateTable(Type modelType, string tableName)
        {
            throw new NotImplementedException();
        }

        public string GetSqlTypeFromType(ColumnInfo col, string tableName)
        {
            throw new NotImplementedException();
        }

        public void CreateChildTable(string tableName, ColumnInfo col)
        {
            throw new NotImplementedException();
        }

        public bool TableExists(string name)
        {
            throw new NotImplementedException();
        }

        public string BuildWhereClause(FilterInfo[] filters)
        {
            throw new NotImplementedException();
        }

        public string GetIdentityColumn()
        {
            throw new NotImplementedException();
        }

        public string EscapeEntity(string entity)
        {
            throw new NotImplementedException();
        }
		
		public void SaveSingleRelation( string childTableName, string parent_id, string child_id ) 
		{
			throw new NotImplementedException();
		}
		
		public void RemoveSingleRelation( string childTableName, string parent_id, string child_id ) 
		{
			throw new NotImplementedException();
		}
		
		public List<string> ColumnList( string tableName )
		{
			throw new NotImplementedException();	
		}
		
		public void RemoveRelations( string childTableName, string ObjectId ) 
		{
			throw new NotImplementedException();	
		}	
	 	
		public DataTable GetColumnTable( string tableName )
		{
			throw new NotImplementedException();	
		}
		
		public string GenerateAddColStatement( ColumnInfo col, string table )
		{
			throw new NotImplementedException();	
		}
		
		public string GenerateRemoveColStatement( string col, string table )
		{
			throw new NotImplementedException();	
		}
		
		public string GenerateFixColTypeStatement( ColumnInfo col, string table )
		{
			throw new NotImplementedException();	
		}
		
		public string GenerateAddTableStatement( Type addTable ) {
			throw new NotImplementedException();	
		}

		public string GenerateAddChildTableStatement( string tableName, bool isDerived ) {
			throw new NotImplementedException();		
		}
		
		public string GetCommentCharacter() {
			throw new NotImplementedException();	
		}
		
		public bool TableExistsNoCache( string name ) {
			throw new NotImplementedException();	
		}
		
		public void SetConnectionString( string cString ) {
			throw new NotImplementedException();				
		}
		
		public string GenerateAddIdColStatement(string table) {
			throw new NotImplementedException();				
		}
		
		public string GenerateModifyIdColStatement(string table) {
			throw new NotImplementedException();				
		}
		
		public string CheckIdColumn( string table ) {
			throw new NotImplementedException();	
		}
		
		public List<string> ExpectedRelationTableColumns { get{ throw new NotImplementedException(); } }
		
		public int GetLatestVersion( AbstractRecord r )
		{
			throw new NotImplementedException();
		}
		
		public int GetLatestVersion( string modelName, int id )
		{
			throw new NotImplementedException();
		}
		
		public List<string> TableList() 
		{
			throw new NotImplementedException();	
		}
    }
}
