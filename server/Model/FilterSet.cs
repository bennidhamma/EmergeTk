// FilterSet.cs created with MonoDevelop
// User: ben at 2:52 P 03/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;

namespace EmergeTk.Model
{
	public enum FilterJoinOperator
	{
		And,
		Or
	}
	
	public class FilterSet : IFilterRule, IQueryInfo
	{		
		List<IFilterRule> rules;
		FilterJoinOperator joinOperator;
		
		public List<IFilterRule> Rules {
			get {
				if( rules == null )
					rules = new List<IFilterRule>();
				return rules;
			}
			set {
				rules = value;
			}
		}

		public FilterJoinOperator JoinOperator {
			get {
				return joinOperator;
			}
			set {
				joinOperator = value;
			}
		}
		
		public FilterSet(FilterJoinOperator joinOperator)
		{
			this.joinOperator = joinOperator;
		}
	}
}
