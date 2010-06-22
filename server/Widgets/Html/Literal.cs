using System;

namespace EmergeTk.Widgets.Html
{
	public class Literal : Widget
	{
		private string html;
		public string Html
		{
			get { return this.html; }
			set
			{
				//log.DebugFormat("setting html from {0} to {1} on {2}", html, value, this );
				if( value != html )
				{
					this.html = value;
					string toClient = Util.ToJavaScriptString( textalize ? Util.Textalize(html) : html );
	                if( rendered )
	                	InvokeClientMethod("SetHtml", toClient );
	                else
	                	ClientArguments["html"] = toClient;
	               	RaisePropertyChangedNotification("Html");
	            }
			}
		}
		
		bool textalize = false;
		public bool Textalize { get { return textalize; } 
			set { 
				textalize = value;
				RaisePropertyChangedNotification("Textalize");
			}
		}

		public Literal(){}

		public Literal( string html ){ this.Html = html; }
	}
}
