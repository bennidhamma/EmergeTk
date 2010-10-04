using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using EmergeTk.Model;

namespace EmergeTk.Model.Providers
{
    public sealed class SqlServerProvider : DataProvider, IDataProvider
    {
        private SqlServerProvider()
        {
        }

        private static readonly SqlServerProvider provider = new SqlServerProvider();
        public static SqlServerProvider Provider
        {
            get { return provider; }
        }

        public bool Synchronizing
        {
        	get { throw new NotImplementedException(); }
        }

        public bool OutOfSync
        {
        	get { throw new NotImplementedException(); }
        }

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConfigurationManager.AppSettings["sqlServerConnectionString"]);
        }

        public IDataParameter CreateParameter()
        {
            throw new NotImplementedException();
        }

        public DbConnection CreateNewConnection()
        {
            throw new NotImplementedException();
        }

        public void Save(AbstractRecord r, bool SaveChildren, bool IncrementVersion, DbConnection conn)
        {
            throw new NotImplementedException();
        }

        T Execute<T>(string sql, SqlExecutionType mode) where T : class
        {
            SqlConnection conn = CreateConnection();
            conn.Open();
            SqlCommand cmd = new SqlCommand(sql, conn);
            //SqlTransaction trans = null;
            //if( wrapTransaction ) trans = conn.BeginTransaction();
            cmd.CommandText = sql;
            T retVal = null;
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
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable t= new DataTable();
                    da.Fill(t);
                    retVal = t as T;
                    break;
            }
            
            //if( wrapTransaction )trans.Commit();
            if (mode != SqlExecutionType.Reader)
            {
                cmd.Dispose();
                conn.Close();
                conn.Dispose();
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
            return Load<T>("","ROWID");
        }

        public IRecordList<T> Load<T>(List<int> ids) where T : AbstractRecord, new()
        {
            throw new NotImplementedException();
        }

        public IRecordList<T> Load<T>(params SortInfo[] sortInfos) where T : AbstractRecord, new()
        {
            return Load<T>(string.Empty, Util.Join(sortInfos));
        }

        public IRecordList<T> Load<T>(string whereClause, string orderByClause) where T : AbstractRecord, new()
        {
            return Load<T>(whereClause, orderByClause, false);
        }

        public IRecordList<T> Load<T>(string whereClause, string orderByClause, bool FastLoad ) where T : AbstractRecord, new()
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
            Type type = typeof(T);
            string name = type.Name;
            if (!TableExists(name))
                return records;

            string star = FastLoad ? ", *" : "";

            DataTable result = SqlServerProvider.Provider.ExecuteDataTable("SELECT ROWID" + star + " FROM " + name + whereClause + orderByClause);

            for (int i = 0; i < result.Rows.Count; i++)
            {
                T r;
                if( FastLoad )
                    r = AbstractRecord.LoadFromDataRow<T>(result.Rows[i]);
                else
                    r = AbstractRecord.Load<T>(Convert.ToInt32(result.Rows[i]["ROWID"]));

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
            return Load<T>(Util.Join(filterInfos), string.Empty);
        }
		
		public IRecordList<T> Load<T>(FilterInfo[] filterInfos, SortInfo[] sortInfos ) where T : AbstractRecord, new() 
        {
            return Load<T>(Util.Join(filterInfos), Util.Join(sortInfos));
        }
        
        public new int GetRowCount<T>()  where T : AbstractRecord, new()
        {
        	throw new NotImplementedException();
        }
        
        public int GetNewId (string type)
        {
        	throw new NotImplementedException();
        }

        public void Save (AbstractRecord record, bool SaveChildren)
        {
        	throw new NotImplementedException();
        }

        public void Delete (AbstractRecord r)
        {
        	throw new NotImplementedException();
        }

        public void SynchronizeModel (Type t)
        {
        	throw new NotImplementedException();
        }

        public void SynchronizeModelType<T> () where T : AbstractRecord, new()
        {
        	throw new NotImplementedException();
        }

        public string GetSqlType (DataType dt)
        {
        	throw new NotImplementedException();
        }

        public void CreateTable (Type modelType, string tableName)
        {
        	throw new NotImplementedException();
        }

        public string GetSqlTypeFromType (ColumnInfo col, string tableName)
        {
        	throw new NotImplementedException();
        }

        public void CreateChildTable (string tableName, ColumnInfo col)
        {
        	throw new NotImplementedException();
        }

        public bool TableExists (string name)
        {
        	throw new NotImplementedException();
        }

        public string BuildWhereClause (FilterInfo[] filters)
        {
        	throw new NotImplementedException();
        }

        public string GetIdentityColumn ()
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
		
		public void RemoveRelations( string childTableName, string ObjectId ) 
		{
			throw new NotImplementedException();	
		}
		public DataTable GetColumnTable( string tableName )
		{
			throw new NotImplementedException();	
		}

		public List<string> ColumnList( string tableName )
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
		
		public string GenerateAddTableStatement(Type addTable ) {
			throw new NotImplementedException();	
		}
		
		public string GenerateFixColTypeStatement( ColumnInfo col, string table )
		{
			throw new NotImplementedException();	
		}
		
		public List<string> TableList() {
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
		
		public int GetLatestVersion( AbstractRecord r )
		{
			throw new NotImplementedException();
		}
		
		public string GenerateAddIdColStatement(string table) {
			throw new NotImplementedException();		
		}
		
		public string GenerateModifyIdColStatement(string table) {
			throw new NotImplementedException();				
		}

		public int GetLatestVersion( string modelName, int id )
		{
			throw new NotImplementedException();
		}

		
		public string CheckIdColumn( string table ) {
			throw new NotImplementedException();	
		}

	}
}
