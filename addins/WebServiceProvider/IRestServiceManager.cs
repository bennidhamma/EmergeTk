
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
		void AuthorizeField( RestOperation op, AbstractRecord record, string property );
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

	public class RootOnlyServiceManager : IRestServiceManager
	{
		#region IRestServiceManager implementation
		public string GetHelpText()
		{
			return "Root Access Only service manager.";
		}


		public void Authorize(RestOperation operation, MessageNode recordNode, AbstractRecord record)
		{
			// root bypasses the Authorize call because of DoAuth so always throw exception
			throw new UnauthorizedAccessException("Not Authorized.");
		}


		public bool AuthorizeField(RestOperation op, AbstractRecord record, string property)
		{
			return true;
		}


		public AbstractRecord GenerateExampleRecord()
		{
			throw new System.NotImplementedException();
		}


		public string GenerateExampleFields(string method)
		{
			throw new System.NotImplementedException();
		}


		public List<RestTypeDescription> GetTypeDescriptions()
		{
			return null;
		}

		#endregion

	}

	public class AuthenticatedReadOnlyServiceManager : IRestServiceManager
	{
		#region IRestServiceManager implementation
		public string GetHelpText()
		{
			return "Authenticated Read-Only service manager.";
		}


		public void Authorize(RestOperation operation, MessageNode recordNode, AbstractRecord record)
		{
			User.AuthenticateUser();
			if (operation != RestOperation.Get)
			{
				throw new UnauthorizedAccessException("Authenticated users can only GET this service.");
			}
		}


		public bool AuthorizeField(RestOperation op, AbstractRecord record, string property)
		{
			return true;
		}


		public AbstractRecord GenerateExampleRecord()
		{
			throw new System.NotImplementedException();
		}


		public string GenerateExampleFields(string method)
		{
			throw new System.NotImplementedException();
		}


		public List<RestTypeDescription> GetTypeDescriptions()
		{
			return null;
		}

		#endregion

	}

	public class AuthenticatedPostOnlyServiceManager : IRestServiceManager
	{
		#region IRestServiceManager implementation
		public string GetHelpText()
		{
			return "Authenticated POST-Only service manager.";
		}


		public void Authorize(RestOperation operation, MessageNode recordNode, AbstractRecord record)
		{
			User.AuthenticateUser();
			if (operation != RestOperation.Post)
			{
				throw new UnauthorizedAccessException("Authenticated users can only POST to this service.");
			}
		}


		public bool AuthorizeField(RestOperation op, AbstractRecord record, string property)
		{
			return true;
		}


		public AbstractRecord GenerateExampleRecord()
		{
			throw new System.NotImplementedException();
		}


		public string GenerateExampleFields(string method)
		{
			throw new System.NotImplementedException();
		}


		public List<RestTypeDescription> GetTypeDescriptions()
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
