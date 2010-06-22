// ArrowPager.cs
//	
//

using System;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class ArrowBoxPager : Pager
	{
		public override void Refresh ()
		{
			ClearChildren();
			if( PageSet.PageCount <= 1 )
				return;
			Label label = RootContext.CreateWidget<Label>(this);
			label.Text = PageSet.Count + " results";
			Image prevArrow = RootContext.CreateWidget<Image>(this);
			Label label2 = RootContext.CreateWidget<Label>(this);
			label2.Text = "Page";
			TextBox pageBox = RootContext.CreateWidget<TextBox>(this);
			pageBox.Text = PageSet.CurrentPage.ToString();
			pageBox.OnChanged += delegate {
				int pageNum = int.Parse(pageBox.Text);
				if( pageNum <= 0 )
					pageNum = 1;
				else if( pageNum > PageSet.PageCount )
					pageNum = PageSet.PageCount;
				PageSet.CurrentPage = pageNum;
				pageBox.Text = pageNum.ToString();
			};
			Label label3 = RootContext.CreateWidget<Label>(this);
			label3.Text = "of " + PageSet.PageCount;
			Image nextArrow = RootContext.CreateWidget<Image>(this);
			
			if( PageSet != null && PageSet.CurrentPage > 1 )
			{
				prevArrow.Url = "page-arrow-left.png";
				prevArrow.OnClick += delegate {
					PageSet.CurrentPage--;
				};
			}
			else
				prevArrow.Url = "page-arrow-left-inactive.png";
			prevArrow.Init();
			
			if( PageSet != null && PageSet.CurrentPage < PageSet.PageCount )
			{
				nextArrow.Url = "page-arrow-right.png";
				nextArrow.OnClick += delegate {
				PageSet.CurrentPage++;
				};
			}
			else
				nextArrow.Url = "page-arrow-right-inactive.png";
			nextArrow.Init();
			
			
		}

	}
}
