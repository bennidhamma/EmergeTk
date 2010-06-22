
using System;

namespace EmergeTk.WebServices
{
	
	
	public class WebServiceAttribute : Attribute
	{
		public string BasePath { get; set; }
		public Type ServiceManager { get;set; }
		
		public WebServiceAttribute(string basePath)
		{
			BasePath = basePath;
		}
	}
}
