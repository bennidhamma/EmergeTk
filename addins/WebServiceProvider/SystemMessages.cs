using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using EmergeTk.Model;
using EmergeTk.Model.Search;
using EmergeTk.Model.Security;
using Jayrock.Json;
using System.Text;

namespace EmergeTk.WebServices
{
	[WebService("/api/", ServiceManager=typeof(SystemMessages))]
	public class SystemMessages : IMessageServiceManager
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(SystemMessages));
		
		[MessageServiceEndPoint("login",Verb=RestOperation.Post)]
		[MessageDescription("Create a session token for a specific user.")]
		public void Login(MessageEndPointArguments arguments)
		{	
			string username = (string)arguments.InMessage["username"];
			string password = (string)arguments.InMessage["password"];
			
			User u = User.LoginUser(username,password);

			if (u != null)
			{
				u.SetLoginCookie();
				arguments.Response.Writer.OpenRoot("response");
				arguments.Response.Writer.WriteProperty("token", u.SessionToken);
				arguments.Response.Writer.CloseRoot();
				log.InfoFormat("{0}, {1}, logged in", u.Id, u.Name);
			}
			else
			{
				log.WarnFormat("{0} ATTEMPTED unsuccessfully to log in", username);
				throw new UnauthorizedAccessException("Invalid credentials");
			}
		}
		
		[MessageServiceEndPoint("logout",Verb=RestOperation.Post)]
		[MessageDescription("Logs out the current user.  Unsets the session token from the data model and removes any cookies.")]
		public void Logout(MessageEndPointArguments arguments)
		{	
			User u = User.FindBySessionToken();
			if( u != null )
			{
				log.InfoFormat("{0}, {1}, logged out", u.Id, u.Name);
				u.SessionToken = string.Empty;
				u.Save();
			}
			else
				throw new UnauthorizedAccessException("Not currently logged in.");
		}
		
		[MessageServiceEndPoint("index/optimize",Verb=RestOperation.Post)]
		[MessageDescription("Index all objects of the specified type.")]
		public void OptimizeIndex(MessageEndPointArguments arguments)
		{
			IndexManager.Instance.Optimize();
		}		
		
		[MessageServiceEndPoint("index/delete/(.+)",Verb=RestOperation.Post)]
		[MessageDescription("Index all objects of the specified type.")]
		public void DeleteDocmentsOfType(MessageEndPointArguments arguments)
		{
			string typeName = arguments.Matches[0].Groups[1].Value;
			Type type = TypeLoader.GetType(typeName);
			log.Debug("Found type to delete indexes: ", type, typeName);
			TypeLoader.InvokeGenericMethod(typeof(SystemMessages),"DeleteDocumentsOfTypeT",new Type[]{type},this,new object[]{});
		}
		
		[MessageServiceEndPoint("index/(.+)",Verb=RestOperation.Post)]
		[MessageDescription("Index all objects of the specified type.")]
		public void IndexType(MessageEndPointArguments arguments)
		{
			string typeName = arguments.Matches[0].Groups[1].Value;
			Type type = TypeLoader.GetType(typeName);
			TypeLoader.InvokeGenericMethod(typeof(SystemMessages),"IndexRecordsOfType",new Type[]{type},this,new object[]{});
		}		
			
		private void DeleteDocumentsOfTypeT<T>() where T : AbstractRecord, new()
		{
			IndexManager.Instance.DeleteAllOfType<T>();
		}		
		
		private void IndexRecordsOfType<T>() where T : AbstractRecord, new()
		{
			IndexManager.Instance.GenerateIndex( DataProvider.LoadList<T>() );
		}
		
		[MessageServiceEndPoint("help",Verb=RestOperation.Get)]
		[MessageDescription("This documentation page.")]
		public void Help(MessageEndPointArguments arguments)
		{
			log.Info("Help requested.");
			write("<h1>API Documentation</h1>");			
			
			foreach( string route in WebServiceManager.Manager.RouteMap.Keys )
			{
				if( route == "/api/model" )
					continue;
				write("<h2>Service: " + route + "</h2>");
				RequestProcessor r = WebServiceManager.Manager.RouteMap[route];
				
				foreach( MessageServiceEndPointAttribute endPoint in r.EndPoints )
				{
					//write( "<h3>" + endPoint.MethodName + " (method)</h3>");
					//write("<div><strong>Verb:</strong> " + endPoint.Verb + "</div>");
					//write("<div><strong>Pattern:</strong> " + endPoint.Pattern + "</div>");
					write("<h3>" + endPoint.Verb.ToString().ToUpper() + " " + route + endPoint.Pattern + "</h3>");
					if( !string.IsNullOrEmpty(endPoint.Description) ) 
						write("<p>" + Util.Textalize( endPoint.Description ) + "</p>");

                    MemoryStream xmlReqStream = new MemoryStream();
                    MemoryStream jsonReqStream = new MemoryStream();
                    MemoryStream xmlRespStream = new MemoryStream();
                    MemoryStream jsonRespStream = new MemoryStream();

                    XmlMessageWriter xmlReqWriter = XmlMessageWriter.Create(xmlReqStream);
                    JsonMessageWriter jsonReqWriter = JsonMessageWriter.Create(jsonReqStream);
                    XmlMessageWriter xmlRespWriter = XmlMessageWriter.Create(xmlRespStream);
                    JsonMessageWriter jsonRespWriter = JsonMessageWriter.Create(jsonRespStream);

                    StreamReader xmlReqReader = null;
                    StreamReader xmlRespReader = null;
                    StreamReader jsonReqReader = null;
                    StreamReader jsonRespReader = null;

                    try
                    {
                        xmlReqReader = new StreamReader(xmlReqStream);
                        xmlRespReader = new StreamReader(xmlRespStream);
                        jsonReqReader = new StreamReader(jsonReqStream);
                        jsonRespReader = new StreamReader(jsonRespStream);

                        r.ServiceManager.GenerateExampleRequestNode(endPoint.MethodName, xmlReqWriter);
                        r.ServiceManager.GenerateExampleResponseNode(endPoint.MethodName, xmlRespWriter);

                        r.ServiceManager.GenerateExampleRequestNode(endPoint.MethodName, jsonReqWriter);
                        r.ServiceManager.GenerateExampleResponseNode(endPoint.MethodName, jsonRespWriter);

                        write("<h4> Example Request: </h4>");
                        write("<b>XML:</b><pre>" + HttpUtility.HtmlEncode(xmlReqReader.ReadToEnd()) + "</pre>");
                        write("<b>JSON:</b><pre>" + jsonReqReader.ReadToEnd() + "</pre>");

                        write("<h4> Example Response: </h4>");
                        write("<b>XML:</b><pre>" + HttpUtility.HtmlEncode(xmlRespReader.ReadToEnd()) + "</pre>");
                        write("<b>JSON:</b><pre>" + jsonRespReader.ReadToEnd() + "</pre>");
                    }
                    finally
                    {
                        if (xmlReqReader != null)
                            xmlReqReader.Close();
                        if (xmlRespReader != null)
                            xmlRespReader.Close();
                        if (jsonReqReader != null)
                            jsonReqReader.Close();
                        if (jsonRespReader != null)
                            jsonRespReader.Close();
                    }
				}
				
				write("<hr>");
			}
		}

#if (IM_LOSING_MY_MIND)
        [MessageServiceEndPoint("test", Verb = RestOperation.Get)]
        [MessageDescription("Debug hack test")]
        public void Test(MessageEndPointArguments arguments)
        {
            StopWatch watchRespStm = new StopWatch("Timing time to write to response stream");
            watchRespStm.Start();
            this.write("Starting direct stream to output stream.");
            DateTime start = DateTime.UtcNow;
            this.WriteStm("{\"testList\":[0");

            for (int i = 1; i < 100000; i++)
            {
                this.WriteStm("," + i.ToString());
            }
            this.WriteStm("]}");
            watchRespStm.Stop();
            TimeSpan ts = DateTime.UtcNow - start;
            this.write(String.Format("Time for streaming to output stream {0} ms.", ts.TotalMilliseconds));

            StopWatch watchMsgList = new StopWatch("Timing time to write to MessageList");
            watchMsgList.Start();
            this.write("Starting to write to messagelist");

            start = DateTime.UtcNow;
            MessageNode node = new MessageNode("response");
            MessageList list = new MessageList();
           
            for (int i = 0; i < 100000; i++)
            {
                list.Add(i);
            }
            node["testList"] = list;
            String s = JSON.Default.Encode(node);
            write(JSON.Default.Encode(node));
            watchMsgList.Stop();
            ts = DateTime.UtcNow - start;
            this.write(String.Format("Time for writing to MsgList, then JSON.Default.Encoding, and then writing to output = {0} ms.", ts.TotalMilliseconds));

            IMessageWriter writer = arguments.Response.Writer;
            StopWatch stopWatchMsgWriter = new StopWatch("Timing time to write using IMessageWriter");
            stopWatchMsgWriter.Start();
            this.WriteStm("Starting write to messagewriter");
            start = DateTime.UtcNow;

            writer.OpenRoot("response");
            writer.OpenProperty("testList");
            writer.OpenList("listitem");
            for (int i = 0; i < 100000; i++)
            {
                writer.WriteScalar(i);
            }
            writer.CloseList();
            writer.CloseRoot();

            ts = DateTime.UtcNow - start;
            this.write(String.Format("Time for writing using MessageWriter = {0} ms.", ts.TotalMilliseconds));
            stopWatchMsgWriter.Stop();
        }

        private void WriteStm(String s)
        {
            HttpContext.Current.Response.OutputStream.Write(UTF8Encoding.Default.GetBytes(s), 0, s.Length);
        }

#endif

        private string FormatJson(string input)
		{
			StringWriter output = new StringWriter();
			using (JsonTextReader reader = new JsonTextReader(new StringReader(input)))
            using (JsonTextWriter writer = new JsonTextWriter(output))
            {
                writer.PrettyPrint = true;
                writer.WriteFromReader(reader);
            }
			return output.ToString();
		}
				
		private void write(string msg)
	    {
			HttpContext.Current.Response.Write(msg);		
		}



		#region IMessageServiceManager implementation
		public void Authorize (RestOperation operation, string method, MessageNode message)
		{
			//throw new System.NotImplementedException();
		}
		
		public string GenerateHelpText ()
		{
			return "General system level functions.";
		}
		
		public void GenerateExampleRequestNode (string method, IMessageWriter writer)
		{
			switch(method)
			{
			case "Login":
                writer.OpenRoot("request");
                writer.WriteProperty("username", "john.doe");
                writer.WriteProperty("password", "secret");
                writer.CloseRoot();
				break;
			default:
				break;
			}
		}
		
		public void GenerateExampleResponseNode (string method, IMessageWriter writer)
		{
			switch(method)
			{
			case "Login":
                writer.OpenRoot("response");
                writer.WriteProperty("token", "1b13db15525d2f6a4b9ad320ae039e66");
				break;
			default:
				break;
			}
		}
		#endregion

	}
}
