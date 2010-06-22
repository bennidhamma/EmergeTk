using System;
using System.Collections;
using System.Collections.Generic;

namespace EmergeTk.Model.Providers
{
	public class MySqlFilterFormatter
	{
		public MySqlFilterFormatter()
		{
		}
		
		public static string BuildWhereClause( IFilterRule[] filters )
		{
			FilterSet fs = new FilterSet(FilterJoinOperator.And);
			fs.Rules.AddRange( filters );
			return FilterString( fs );
		}
		
		public static string FilterString( FilterSet fs )
		{
			if( fs.Rules.Count == 0 )
				return null;
			List<string> items = new List<string>();
			foreach( IFilterRule r in fs.Rules )
			{
				if( r is FilterInfo ) 
				{
					items.Add( FilterString( r as FilterInfo) );
				}
				else if( r is FilterSet )
				{
					items.Add( "(" + FilterString( r as FilterSet ) + ")" );
				}
			}
			return items.Join( fs.JoinOperator == FilterJoinOperator.And ? " AND " : " OR " );	
		}
		
		public static string FilterString( FilterInfo fi )
		{
			object oVal = fi.Value is AbstractRecord ? (fi.Value as AbstractRecord).ObjectId : fi.Value;
            string v = oVal != null ? oVal.ToString() :null;
			bool stringProcessed = false;
			if( oVal is Enum )
			{
				v = Convert.ToInt32( oVal ).ToString();	
			}

			if (oVal == null)
			{
				if (fi.Operation == FilterOperation.Equals)
				{
					return fi.ColumnName + " IS NULL";
				}
				if (fi.Operation == FilterOperation.DoesNotEqual)
				{
					return fi.ColumnName + " IS NOT NULL";
				}
				if (fi.Operation == FilterOperation.LessThanOrEqual ||
					fi.Operation == FilterOperation.GreaterThanOrEqual)
				{
					throw new Exception(
						String.Format("MySQLFilterFormatter: Cannot express {0} operation on null argument for column name {1}",
										FilterInfo.FilterOperationToString(fi.Operation),
										fi.ColumnName));
				}
			}
            if( fi.Operation == FilterOperation.Contains || fi.Operation == FilterOperation.NotContains )
            {
            	v = Util.Surround(v,"%");
            }
            else if( fi.Operation == FilterOperation.In || fi.Operation == FilterOperation.NotIn)
            {
				if( oVal is IList )
				{
					IList list = (IList)oVal;
					string inList = list.Count > 0 ? Util.Join( list, ",", false ) : "NULL";
            		v = string.Format("({0})",inList );
				}
				else if( oVal is IRecordList )
				{
					IRecordList irl = (IRecordList)oVal;
					string inList = irl.Count > 0 ? Util.Join( irl.ToIdArray(), ",", false ) : "NULL";
					v = string.Format("({0})", inList );
				}
				else if( oVal is string )
				{
					//assume to be a subquery.
					v = string.Format("({0})", oVal );
					stringProcessed = true;
				}
            }
            if( oVal is DateTime )
            {
            	DateTime d = (DateTime)oVal;
            	v = "'" + d.ToString("s") + "'";
            }
            else if( oVal is bool )
            {
            	v = (bool)oVal ? "1" : "0";
            }
			else if ( ( oVal is string || oVal is Enum ) && ! stringProcessed )
            {
                v = "'" + v.ToString().Replace("'", "''") + "'";
            }
            return string.Format("{0} {1} {2}", fi.ColumnName, FilterInfo.FilterOperationToString(fi.Operation), v);
		}
	}
}
