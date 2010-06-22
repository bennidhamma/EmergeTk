
using System;
using System.Collections.Generic;

namespace EmergeTk.Model.Search
{
	public interface ISearchFilterFormatter
	{
		string BuildQuery( params IFilterRule[] filters );
		string FilterString( FilterSet fs );
		string FilterString( FilterInfo fi );
	}
}
