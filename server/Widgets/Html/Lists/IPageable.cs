// /home/ben/workspaces/emergeTk/trunk/Widgets/Html/IPageable.cs created with MonoDevelop
// User: ben at 4:34 PM 7/6/2007
//

using System;

namespace EmergeTk.Widgets.Html
{	
	public interface IPageable
	{
		int CurrentPage { get; set; }
		int PageCount { get; }
		int Count {get;}
	}
}
