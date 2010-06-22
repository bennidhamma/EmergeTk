// SortRecord.cs
//	
//

using System;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model
{
	public class SortRecord : AbstractRecord
	{
		string columnName;
		SortDirection direction;
		
		public SortDirection Direction {
			get {
				return direction;
			}
			set {
				direction = value;
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

		public SortInfo ToSortInfo()
		{
			return new SortInfo( columnName, direction );
		}
		
		public SortRecord()
		{
		}
	}
}
