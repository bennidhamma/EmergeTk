using EmergeTk;
using EmergeTk.Widgets.Html;
using System;

namespace EmergeTk.Widgets.Html
{
	public class ProgressButton : Button
	{
		public override string ClientClass {
			get { return "Button"; }
		}

		private string imageUrl = "Images/loading_button.gif";
		public string ImageUrl { get { return imageUrl; } 
			set { 
				imageUrl = value;
				RaisePropertyChangedNotification("ImageUrl");
			}
		}
		
		private Image img;
		
		public override void Initialize ()
		{
			ClassName = "progressButton";
			img = RootContext.CreateWidget<Image>();
			img.Url = imageUrl;
			img.ClassName = "progressImage";
			img.StyleArguments["display"] = "'none'";
			Parent.Add( img );
			OnClick += new EventHandler<ClickEventArgs>(Clicked);
			this.SetClientProperty("clientClickHandler",
				string.Format( @"function(){{ {0}.elem.style.display='';}}",img.ClientId)); 
		}
		
		public void Clicked(object sender, ClickEventArgs ea )
		{
			img.Remove();
		}
	}
}
