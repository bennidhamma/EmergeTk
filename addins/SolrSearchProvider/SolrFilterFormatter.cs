using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using EmergeTk;


namespace EmergeTk.Model.Search
{
	/// <summary>
	/// Default search filter formatting.  Intended to be compatible with Lucene (the default search provider for
	/// EmergeTk, and as much as possible with Solr (for scalable search.)
	/// </summary>
	public class SolrFilterFormatter : ISearchFilterFormatter
	{
		public SolrFilterFormatter()
		{
		}
		
		public string BuildQuery( params IFilterRule[] filters )
		{
			FilterSet fs = new FilterSet(FilterJoinOperator.And);
			fs.Rules.AddRange( filters );
			return FilterString( fs );
		}
		
		public virtual string FilterString( FilterSet fs )
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
				else if( r is FilterSet && ((FilterSet)r).Rules.Count > 0 )
				{
                    String filterString = FilterString(r as FilterSet);
                    if (!String.IsNullOrEmpty(filterString))
                    {
                        items.Add("(" + filterString + ")");
                    }
				}
			}
			return items.Join( fs.JoinOperator == FilterJoinOperator.And ? " AND " : " OR " );	
		}
		
		protected virtual string serializeValueForSearch(object input)
		{
            string v = input != null ? input.ToString() :null;
			if (v == null)
				return null;

			if( input is Enum )
			{
				v = input.ToString();	
			}
			else if( input is DateTime )
            {
            	DateTime d = (DateTime)input;
            	v = d.ToString("s") + "Z";
            }
            else if( input is bool )
            {
            	v = (bool)input ? "true" : "false";
            }

			return v;
		}
		
		public virtual string FilterString( FilterInfo fi )
		{
			object oVal = fi.Value;
			string v = serializeValueForSearch( fi.Value );
			
            if( fi.Operation == FilterOperation.Equals && oVal == null ||
            	fi.Operation == FilterOperation.DoesNotEqual && oVal == null )
            {
            	throw new NotImplementedException("Cannot filter for null in DefaultSearchFilterFormatter");
            }
			// BUGBUG - don't think these two (Contains and Not Contains) actually work yet.
            else if( fi.Operation == FilterOperation.Contains)
            {
            	v = v + "*";
            }
			else if (fi.Operation == FilterOperation.NotContains)
			{
				v = "-" + v + "*";
			}
			else if (fi.Operation == FilterOperation.In)
			{
				if (oVal is IRecordList)
				{
					IRecordList irl = (IRecordList)oVal;
					FilterSet inSet = new FilterSet(FilterJoinOperator.Or);
					foreach (IRecord r in irl)
					{
						inSet.Rules.Add(new FilterInfo(fi.ColumnName, r.Id));
					}
					v = string.Format("({0})", FilterString(inSet));
				}
				else if (oVal is IList)
				{
					IList il = (IList)oVal;
					FilterSet inSet = new FilterSet(FilterJoinOperator.Or);
					foreach (object o in il)
					{
						inSet.Rules.Add(new FilterInfo(fi.ColumnName, o));
					}
					v = string.Format("({0})", FilterString(inSet));
				}
			}
			else if (fi.Operation == FilterOperation.LessThan)
			{
				return String.Format("{0}:{{* TO {1}}}", fi.ColumnName, v);
			}
			else if (fi.Operation == FilterOperation.GreaterThan)
			{
				return String.Format("{0}:{{{1} TO *}}", fi.ColumnName, v);
			}
			else if (fi.Operation == FilterOperation.LessThanOrEqual)
			{
				return String.Format("{0}:[* TO {1}]", fi.ColumnName, v);
			}
			else if (fi.Operation == FilterOperation.GreaterThanOrEqual)
			{
				return String.Format("{0}:[{1} TO *]", fi.ColumnName, v);
			}
			else if (fi.Operation == FilterOperation.Equals)
			{
				return String.Format("{0}:{1}", fi.ColumnName, v);
			}
			else
			{
				throw new NotImplementedException("Operation " + fi.Operation + " not in DefaultSearchFilterFormatter");
			}
           
            return string.Format("{0}:{2}", fi.ColumnName, FilterInfo.FilterOperationToString(fi.Operation), v);
		}
	}
}