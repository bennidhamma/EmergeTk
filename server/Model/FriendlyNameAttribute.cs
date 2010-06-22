// FriendlyNameAttribute.cs created with MonoDevelop
// User: ben at 1:31 P 19/03/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace EmergeTk.Model
{
	
	public class FriendlyNameAttribute : Attribute
	{
		string name;
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public string[] FieldNames {
			get {
				return fieldNames;
			}
			set {
				fieldNames = value;
			}
		}
		
		//field names are used for generating friendly names for vector types, such as enums.
		
		string[] fieldNames;
		
		public FriendlyNameAttribute()
		{
		}
		
		
		public FriendlyNameAttribute( string name )
		{
			this.name = name;
		}
	}
}
