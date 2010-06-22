// /home/ben/workspaces/emergeTk/trunk/Widgets/Html/Pager.cs created with MonoDevelop
// User: ben at 4:27 PM 7/6/2007
//

using System;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{	
	public class Pager : Generic
	{		
		public Pager()
		{
		}
		
		IPageable pageSet;
		LinkButton currentPageButton;
		
		public virtual IPageable PageSet {
			get {
				return pageSet;
			}
			set {
				pageSet = value;
				if( Initialized )
					Refresh();
				RaisePropertyChangedNotification("PageSet");
			}
		}
		
		public virtual void Refresh()
		{
			ClearChildren();
			for( int i = 0; i < pageSet.PageCount; i++ )
 			{
 				Add( createPageButton( i+1, i+1 == pageSet.CurrentPage ) );
 			}
		}
		
 		public override void Initialize()
 		{
 			this.ClassName = "pager";
 			if( pageSet != null )
 				Refresh();
 		}
 		
 		public Widget createPageButton( int number, bool selected )
 		{
 			LinkButton lb = RootContext.CreateWidget<LinkButton>();
 			lb.Label = number.ToString();
 			if( selected )
 			{
 				lb.AppendClass("selected");
 				currentPageButton = lb;
 			}
 			lb.OnClick += new EventHandler<ClickEventArgs>( delegate( object sender, ClickEventArgs ea )
 				{
 					if( currentPageButton == lb )
 						return;
 					if( currentPageButton != null )
 						currentPageButton.RemoveClass("selected");
 					currentPageButton = (LinkButton)ea.Source;
 					currentPageButton.AppendClass("selected");
 					pageSet.CurrentPage = int.Parse(currentPageButton.Label);
 				});
 			return lb;
 		}
	}
}
