// IJSONSerializable.cs created with MonoDevelop
// User: ben at 5:17 P 17/03/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;

namespace EmergeTk
{
	public interface IJSONSerializable
	{
		bool IsDeserializing { get; set; }
		Dictionary<string,object> Serialize();
		void Deserialize(Dictionary<string,object> json);
	}
}
