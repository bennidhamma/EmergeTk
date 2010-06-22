// Unpack.cs created with MonoDevelop
// User: ben at 3:58 P 14/03/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Tests
{
	public class Unpack : Context
	{
		public Unpack()
		{
		}
		
		public override void Initialize ()
		{
			TextBox tb = CreateWidget<TextBox>(this);
			tb.Rows = 10;
			tb.Columns = 50;
			Button b = CreateWidget<Button>(this);
			b.Label = "Deserialize";
			b.OnClick += new EventHandler<ClickEventArgs>( delegate( object send7er, ClickEventArgs ea )
			{
				object json = JSON.Default.Decode( tb.Text );
				Widget w = json as Widget;
				if( w != null )
				{
					ClearChildren();
					Add(w);
				}
			});			
		}

	}
}
