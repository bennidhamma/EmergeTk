
using System;

namespace EmergeTk.WebServices
{
	public class RestServiceAttribute : Attribute
	{
		public string ModelName {
			get;
			set;
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
		
		public Type ServiceManager { get; set; }
		
		public RestOperation Verb {
			get;
			set;
		}
		public RestServiceAttribute ()
		{
			Verb = RestOperation.Delete | RestOperation.Get | RestOperation.Post | RestOperation.Put;
		}
	}
	
	public class RestIgnoreAttribute : Attribute
	{
	}
}
