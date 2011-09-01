using System;
using System.Collections.Generic;

namespace EmergeTk.Model.Providers
{
	public class MockDataProvider : IDataProvider
	{
		Random rand = new Random ();
		
		Dictionary<Type, Dictionary<int, AbstractRecord>> db = new Dictionary<Type, Dictionary<int, AbstractRecord>> ();
		
		public MockDataProvider ()
		{
		}

		#region IDataProvider implementation
		public object ExecuteScalar (string sql)
		{
			return null;
		}

		public System.Collections.Generic.List<int> ExecuteVectorInt (string sql)
		{
			return null;
		}

		public void ExecuteNonQuery (string sql)
		{
			
		}

		public void ExecuteReader (string sql, ReaderDelegate r)
		{
			
		}

		public System.Data.DataTable ExecuteDataTable (string sql)
		{
			return null;
		}

		public System.Data.DataSet ExecuteDataSet (string sql)
		{
			return null;
		}

		System.Data.DataSet IDataProvider.ExecuteDataSet (string sql, System.Data.IDataParameter[] parms)
		{
			return null;
		}

		public System.Data.IDataParameter CreateParameter ()
		{
			return null;
		}

		public IRecordList<T> Load<T> () where T : AbstractRecord, new ()
		{
			return new RecordList<T> ();
		}

		IRecordList<T> IDataProvider.Load<T> (System.Collections.Generic.List<int> ids)
		{
			return new RecordList<T> ();
		}

		IRecordList<T> IDataProvider.Load<T> (params SortInfo[] sortInfos)
		{
			return new RecordList<T> ();
		}

		IRecordList<T> IDataProvider.Load<T> (params FilterInfo[] filterInfos) 
		{
			return new RecordList<T> ();
		}

		IRecordList<T> IDataProvider.Load<T> (FilterInfo[] filterInfos, SortInfo[] sortInfos) 
		{
			return new RecordList<T> ();
		}

		IRecordList<T> IDataProvider.Load<T> (string whereClause, string orderByClause) 
		{
			return new RecordList<T> ();
		}

		IRecordList<T> IDataProvider.Load<T> (string whereClause, string orderByClause, string selectColumns) 
		{
			return new RecordList<T> ();
		}

		public Type GetTypeForId (int id)
		{
			return null;
		}

		public int RowCount<T> () where T : AbstractRecord, new()
		{
			return 0;
		}
		
		int nextId = 1;
		public int GetNewId (string typeName)
		{
			return nextId++;
		}

		public int GetLatestVersion (AbstractRecord r)
		{
			return 0;
		}

		int IDataProvider.GetLatestVersion (string modelName, int id)
		{
			return 0;
		}

		public System.Data.Common.DbConnection CreateNewConnection ()
		{
			return null;
		}

		public void Save (AbstractRecord record, bool SaveChildren)
		{
			((IDataProvider)this).Save (record, SaveChildren, false, null);
		}

		void IDataProvider.Save (AbstractRecord record, bool SaveChildren, bool IncrementVersion, System.Data.Common.DbConnection conn)
		{
			Type t = record.GetType();
			if (!db.ContainsKey(t))
				db [t] = new Dictionary<int, AbstractRecord> ();
			record.EnsureId ();
			db[t][record.Id] = record;
		}

		public void SaveSingleRelation (string childTableName, string parent_id, string child_id)
		{
		
		}

		public void RemoveRelations (string childTableName, string ObjectId)
		{
			
		}

		public void RemoveSingleRelation (string childTableName, string parent_id, string child_id)
		{
			
		}

		public void Delete (AbstractRecord r)
		{
			Type t = r.GetType();
			if (!db.ContainsKey(t))
				db [t] = new Dictionary<int, AbstractRecord> ();
			if (db[t].ContainsKey (r.Id))
				db[t].Remove (r.Id);
		}

		public void SynchronizeModel (Type t)
		{
			
		}

		public void SynchronizeModelType<T> () where T :AbstractRecord, new ()
		{
			
		}

		public string GetSqlType (DataType dt)
		{
			return null;
		}

		public void CreateTable (Type modelType, string tableName)
		{
		}

		public string GetSqlTypeFromType (ColumnInfo col, string tableName)
		{
			return null;
		}

		public void CreateChildTable (string tableName, ColumnInfo col)
		{
		}

		public bool TableExists (string name)
		{
			return true;
		}

		public bool TableExistsNoCache (string name)
		{
			return true;
		}

		public System.Collections.Generic.List<string> ColumnList (string tableName)
		{
			return null;
		}

		public System.Collections.Generic.List<string> TableList ()
		{
			return null;
		}

		public System.Data.DataTable GetColumnTable (string tableName)
		{
			return null;
		}

		public string GenerateAddColStatement (ColumnInfo col, string table)
		{
			return null;
		}

		public string GenerateRemoveColStatement (string col, string table)
		{
			return null;
		}

		public string GenerateFixColTypeStatement (ColumnInfo col, string table)
		{
			return null;
		}

		public string CheckIdColumn (string table)
		{
			return null;
		}

		public string GenerateAddIdColStatement (string table)
		{
			return null;
		}

		public string GenerateModifyIdColStatement (string table)
		{
			return null;
		}

		public string GenerateAddTableStatement (Type addTable)
		{
			return null;
		}

		public string GenerateAddChildTableStatement (string tableName, bool isDerived)
		{
			return null;
		}

		public string GetCommentCharacter ()
		{
			return null;
		}

		public string BuildWhereClause (FilterInfo[] filters)
		{
			return null;
		}

		public string GetIdentityColumn ()
		{
			return null;
		}

		public string EscapeEntity (string entity)
		{
			return null;
		}

		public bool Synchronizing {
			get {
				return false;
			}
		}

		public bool OutOfSync {
			get {
				return false;
			}
		}
		#endregion
	}
}

