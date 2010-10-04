// FilterRecord.cs
//	
//
using System;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model
{
	public class FilterRecord : AbstractRecord
	{
		string columnName;
		string testValue;
		object testObject;
		FilterOperation operation;
		
		public string JsonValue {
			get {
				return testValue;
			}
			set {
				testValue = value;
			}
		}

		public FilterOperation Operation {
			get {
				return operation;
			}
			set {
				operation = value;
			}
		}
		
		public string ColumnName {
			get {
				return columnName;
			}
			set {
				columnName = value;
			}
		}

		public object TestObject
		{
			get
			{
				if( testObject != null )
					return testObject;
				else if( testValue != null )
				{
					testObject = JSON.Default.Decode(testValue);
				}
				return testObject;
			}
		}
		
		public FilterRecord()
		{
		}

		public override bool Equals(object obj)
		{
			if( obj is FilterRecord )
			{
				return columnName == (obj as FilterRecord).columnName;
			}
			return base.Equals (obj);
		}
		
		public override int GetHashCode() 
		{
			return (operation.ToString() + Id.ToString()).GetHashCode();
		}

		public void SetValue(object v)
		{
			testObject = v;
			//this.testValue = JSON.Default.Encode(v);
		}

		public override void Save (bool SaveChildren, bool IncrementVersion, System.Data.Common.DbConnection conn)
		{
			testValue = JSON.Default.Encode(testObject);
			base.Save(SaveChildren, IncrementVersion, conn);		
		}

		
		public FilterRecord(FilterInfo fi)
		{
			this.columnName = fi.ColumnName;
			this.operation = fi.Operation;
			this.testObject = fi.Value;
		}

		public FilterInfo ToFilterInfo()
		{
			log.Debug("filter value is ", TestObject, columnName );
			return new FilterInfo( columnName, TestObject, operation );
		}
	}
}
