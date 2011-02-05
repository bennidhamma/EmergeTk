
using System;
using System.Collections.Generic;

namespace EmergeTk.WebServices
{
	public class RestServiceAttribute : Attribute
	{
		string modelName;
		public string ModelName {
			get { return modelName; }
			set { modelName = value; }
		}
		
		string modelPluralName;
		public string ModelPluralName {
			get {
				return modelPluralName ?? ModelName + "s";
			}
			set
			{
				modelPluralName = value;	
			}
		}
		
		Type serviceManager = typeof (DefaultServiceManager);
		public Type ServiceManager { get { return serviceManager; } set { serviceManager = value; } }
		
		public RestOperation Verb {
			get;
			set;
		}
		
		public RestServiceAttribute ()
		{
			Verb = RestOperation.Delete | RestOperation.Get | RestOperation.Post | RestOperation.Put;
		}
		
		public static Dictionary<Type,RestServiceAttribute> attributes = new Dictionary<Type, RestServiceAttribute> ();
		public static RestServiceAttribute GetAttributeForType (Type t)
		{
			if (!attributes.ContainsKey (t)	)
			{
				object[] atts = t.GetCustomAttributes (typeof(RestServiceAttribute), false);
				if (atts.Length > 0)
				{
					var r = (RestServiceAttribute)atts[0];
					attributes[t] = r;
					return r;
				}
			}
			else
				return attributes[t];
			
			return null;
		}
	}
	
	public class RestIgnoreAttribute : Attribute
	{
	}
}
