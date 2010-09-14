using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace EmergeTk.Model
{
	public delegate void ReaderDelegate(IDataReader r);
	
	public interface IDataProvider
	{
		object ExecuteScalar(string sql);
		List<int> ExecuteVectorInt(string sql);
        void ExecuteNonQuery(string sql);
        void ExecuteReader(string sql, ReaderDelegate r);
        DataTable ExecuteDataTable(string sql);
        DataSet ExecuteDataSet(String sql);
        DataSet ExecuteDataSet(string sql, IDataParameter[] parms);
        IDataParameter CreateParameter();
		IRecordList<T> Load<T>() where T : AbstractRecord, new();
        IRecordList<T> Load<T>(List<int> ids) where T : AbstractRecord, new();
        IRecordList<T> Load<T>(params SortInfo[] sortInfos) where T : AbstractRecord, new();
        IRecordList<T> Load<T>(params FilterInfo[] filterInfos) where T : AbstractRecord, new();
        IRecordList<T> Load<T>(FilterInfo[] filterInfos, SortInfo[] sortInfos) where T : AbstractRecord, new();
        IRecordList<T> Load<T>(string whereClause, string orderByClause) where T : AbstractRecord, new();
        IRecordList<T> Load<T>(string whereClause, string orderByClause, String selectColumns) where T : AbstractRecord, new();
		
		Type GetTypeForId( int id );
        int RowCount<T>() where T : AbstractRecord, new();
        int GetNewId(string typeName);
        int GetLatestVersion( AbstractRecord r );
		int GetLatestVersion( string modelName, int id );
        DbConnection CreateNewConnection();
        
        //IoC methods
        
        void Save(AbstractRecord record, bool SaveChildren);
        void Save(AbstractRecord record, bool SaveChildren, bool IncrementVersion, DbConnection conn);

    	void SaveSingleRelation( string childTableName, string parent_id, string child_id );
		void RemoveRelations( string childTableName, string ObjectId );
		void RemoveSingleRelation( string childTableName, string parent_id, string child_id );
				
		void Delete( AbstractRecord r );
		void SynchronizeModel(Type t);
		void SynchronizeModelType<T>() where T : AbstractRecord, new();
        string GetSqlType( DataType dt );
        void CreateTable(Type modelType, string tableName);
        string GetSqlTypeFromType(ColumnInfo col, string tableName);
		void CreateChildTable(string tableName, ColumnInfo col);
		
		bool TableExists( string name );
		bool TableExistsNoCache( string name );
		List<string> ColumnList( string tableName );
		List<string> TableList();
		DataTable GetColumnTable( string tableName );
		string GenerateAddColStatement( ColumnInfo col, string table );
		string GenerateRemoveColStatement( string col, string table );
		string GenerateFixColTypeStatement( ColumnInfo col, string table );
		string CheckIdColumn( string table );
		string GenerateAddIdColStatement(string table);
		string GenerateModifyIdColStatement(string table);
		string GenerateAddTableStatement( Type addTable );
		string GenerateAddChildTableStatement( string tableName, bool isDerived );
		string GetCommentCharacter();
		
        string BuildWhereClause(FilterInfo[] filters );
        string GetIdentityColumn();
        string EscapeEntity(string entity);
        
		void SetConnectionString(String cString);
		
        //IoC props
        bool Synchronizing { get; }
        bool OutOfSync { get; }
	}
}
