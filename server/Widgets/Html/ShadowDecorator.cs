// ShadowDecorator.cs created with MonoDevelop
// User: ben at 2:15 P 18/06/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace EmergeTk.Widgets.Html
{
	public class ShadowDecorator : Generic
	{
		public ShadowDecorator()
		{
		}
		
		Pane front;
		
		public override void Add (Widget c)
		{
			if( front == null )
			{
				front = Find<Pane>("Front");
				if( front == null )
				{
					base.Add( c );
					return;
				}
			}
			front.Add (c);
		}

	}
}
