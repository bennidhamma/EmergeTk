// NumberSpinner.cs created with MonoDevelop
// User: ben at 10:35 P 22/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using EmergeTk;

namespace EmergeTk.Widgets.Html
{
	public class NumberSpinner : TextBox
	{
		
		public NumberSpinner()
		{
			SetClientAttribute("isSpinner",1);
		}
	}
}
