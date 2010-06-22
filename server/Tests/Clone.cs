// Clone.cs created with MonoDevelop
// User: ben at 3:56 P 23/12/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using EmergeTk;
using EmergeTk.Widgets.Html;


namespace EmergeTk.Tests
{	
	public class Clone : Context
	{		
		public Clone()
		{
		}
		
		public override void Initialize ()
		{
			TextBox tb = CreateWidget<TextBox>(this);
			tb.Id = "foo";
			tb.OnChanged += new EventHandler<ChangedEventArgs>( delegate( object sender, ChangedEventArgs ea ) {
					Debug.Trace("UID is {0}", ea.Source.Id );
				}); 
			TextBox tb2 = tb.Clone() as TextBox;
			tb2.Id = "tb2";
			Add( tb2 );
		}
		
		

	}
}
