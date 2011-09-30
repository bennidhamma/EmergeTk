// Ownership.cs created with MonoDevelop
// User: ben at 9:19 A 16/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model.Security
{
	public class License : AbstractRecord
	{
		string objectType, licensedObjectId;
		
		RecordList<User> users;
		
		RecordList<Group> groups;
		
		public string ObjectType {
			get {
				return objectType;
			}
			set {
				objectType = value;
			}
		}

		public string LicensedObjectId {
			get {
				return licensedObjectId;
			}
			set {
				licensedObjectId = value;
			}
		}

		public EmergeTk.Model.RecordList<User> Users {
			get {
				if( users == null )
					lazyLoadProperty<User>("Users");
				return users;
			}
			set {
				users = value;
			}
		}

		public EmergeTk.Model.RecordList<Group> Groups {
			get {
				if( groups == null )
					lazyLoadProperty<Group>("Groups");
				return groups;
			}
			set {
				groups = value;
			}
		}
		
		public License()
		{
		}
	}
}
