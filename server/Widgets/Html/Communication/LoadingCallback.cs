using EmergeTk;
using System;

namespace EmergeTk.Widgets.Html
{
	public class LoadingCallback : Pane
	{		
		public override string ClientClass { get { return "Pane"; } }
		string imageUrl;
		public string ImageUrl { get { return imageUrl; } set { imageUrl = value; RaisePropertyChangedNotification("ImageUrl");} }
		
		override public void Initialize()
		{
			Image i = RootContext.CreateWidget<Image>();
			i.Url = imageUrl ?? ThemeManager.Instance.RequestClientPath("/Images/loading.gif");
			Add(i);
			InvokeClientMethod("InvokeEvent","'OnCallback'");
			ClassName = "loading";
		}
		
		public EventHandler<WidgetEventArgs> OnCallback;
		
		override public void HandleEvents(string evt, string args)
		{
			if( evt == "OnCallback" )
			{
				Remove();
				if( OnCallback != null )
				{
					OnCallback(this, new WidgetEventArgs( this, args, null ) );
				}
			}
			else
				base.HandleEvents(evt,args);
		}
	}
}
