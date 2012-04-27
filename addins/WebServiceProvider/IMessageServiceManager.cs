using System;
using EmergeTk.Model.Security;
using SimpleJson;

namespace EmergeTk.WebServices
{
	public interface IMessageServiceManager
	{
		/// <summary>
		/// Throw a AccessViolationException if Authorize fails.
		/// </summary>
		/// <param name="operation">
		/// A <see cref="RestOperation"/>
		/// </param>
		/// <param name="user">
		/// A <see cref="User"/>
		/// </param>
		void Authorize(RestOperation operation, string method, JsonObject message);
		string GenerateHelpText();
		void GenerateExampleRequestNode(string method, IMessageWriter writer);
		void GenerateExampleResponseNode(string method, IMessageWriter writer);
	}
}
