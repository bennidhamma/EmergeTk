// EnumDropDown.cs created with MonoDevelop
// User: ben at 11:04 A 29/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class EnumDropDown<T> : DropDown where T : struct
	{
		public EnumDropDown()
		{
			List<string> options = new List<string>(Enum.GetNames(typeof(T) ));
        
            for( int i = 1; i < options.Count; i++ )
            {
            	options[i] = Util.PascalToHuman( options[i] );
            }
			
			Options = options;
			DefaultProperty = "SelectedIndex";
		}
	}
}
