// /home/ben/workspaces/Blendr/project/trunk/Model/HelpTextAttribute.cs created with MonoDevelop
// User: ben at 6:13 PM 7/24/2007
//

using System;

namespace EmergeTk.Model
{
	
	
	public class HelpTextAttribute : Attribute
	{
		string text;
		
		public virtual string Text {
			get {
				return text;
			}
			set {
				text = value;
			}
		}
		
		public HelpTextAttribute(string text)
		{
			this.text = text;
		}
	}
}
