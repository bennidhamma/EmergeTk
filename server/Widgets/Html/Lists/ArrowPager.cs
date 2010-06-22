// ArrowPager.cs
//	
//

using System;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public enum PagerDirection
	{
		Next,
		Previous
	}
	
	public class ArrowPager : Pager
	{
		PagerDirection direction;
		
		public PagerDirection Direction {
			get {
				return direction;
			}
			set {
				direction = value;
			}
		}

		public override void Refresh ()
		{
			log.Debug( "Refreshing arrow pager", Direction, PageSet.CurrentPage  , PageSet.PageCount ); 
			ClearChildren();
			if( PageSet.PageCount <= 1 )
				return;

			Image arrow = RootContext.CreateWidget<Image>(this);
			if( Direction == PagerDirection.Next )
			{
				
				if( PageSet != null && PageSet.CurrentPage < PageSet.PageCount )
				{
					arrow.Url = "page-arrow-right.png";
					arrow.OnClick += delegate {
						PageSet.CurrentPage++;
					};
				}
				else
					arrow.Url = "page-arrow-right-inactive.png";
			}
			else
			{
				if( PageSet != null && PageSet.CurrentPage > 1 )
				{
					arrow.Url = "page-arrow-left.png";
					arrow.OnClick += delegate {
						PageSet.CurrentPage--;
					};
				}
				else
					arrow.Url = "page-arrow-left-inactive.png";
			}
			arrow.Init();
		}

	}
}
