// RepeaterGroup.cs created with MonoDevelop
// User: ben at 23:23 07/03/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace EmergeTk.Widgets.Html
{
	public class RepeaterGroup
	{
		Generic header, body, footer;
		string field;
		
		public Generic Header {
			get {
				return header;
			}
			set {
				header = value;
			}
		}
		
		public Generic Body {
			get {
				return body;
			}
			set {
				body = value;
			}
		}

		public Generic Footer {
			get {
				return footer;
			}
			set {
				footer = value;
			}
		}

		public string Field {
			get {
				return field;
			}
			set {
				field = value;
			}
		}
		
		public RepeaterGroup()
		{
		}
	}
}
