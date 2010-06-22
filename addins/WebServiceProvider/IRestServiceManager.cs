
using System;
using System.Collections.Generic;
using EmergeTk.Model;
using EmergeTk.Model.Security;

namespace EmergeTk.WebServices
{
	public interface IRestServiceManager
	{
		string GetHelpText();
		void Authorize(RestOperation operation, MessageNode recordNode, AbstractRecord record);
		bool AuthorizeField( RestOperation op, AbstractRecord record, string property );
		AbstractRecord GenerateExampleRecord();
		string GenerateExampleFields(string method);
		List<RestTypeDescription> GetTypeDescriptions();
	}
	
	public class DefaultServiceManager : IRestServiceManager
	{
		#region IRestServiceManager implementation
		public string GetHelpText ()
		{
			return "Default service manager.  Useful for developer purposes.  No security.";
		}
		
		
		public void Authorize (RestOperation operation, MessageNode recordNode, AbstractRecord record)
		{
			return;
		}
		
		
		public bool AuthorizeField (RestOperation op, AbstractRecord record, string property)
		{
			return true;
		}
		
		
		public AbstractRecord GenerateExampleRecord ()
		{
			throw new System.NotImplementedException();
		}
		
		
		public string GenerateExampleFields (string method)
		{
			throw new System.NotImplementedException();
		}
		
		
		public List<RestTypeDescription> GetTypeDescriptions ()
		{
			return null;
		}
		
		#endregion
			
	}
	
	public struct RestTypeDescription		
	{
		public Type RestType;
		public string ModelName;
		public string ModelPluralName;
		public RestOperation Verb;
		
		public override string ToString ()
		{
			return string.Format ("[RestTypeDescription: RestType={0}, ModelName={1}]", RestType, ModelName);
		}
	}
}
