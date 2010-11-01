using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web;
using EmergeTk.Model;
using System.Text;

namespace EmergeTk.WebServices
{
    public enum WebServiceFormat
    {
        Json = 0,
        Xml = 1
    };

	public class ServiceRouter : IHttpHandler
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ServiceRouter));
		const int API = 1;

        private static readonly Dictionary<String, WebServiceFormat> formats =
            new Dictionary<string, WebServiceFormat>(StringComparer.CurrentCultureIgnoreCase)
            {
                {"json", WebServiceFormat.Json},
                {"xml", WebServiceFormat.Xml}
            };
		
		static ServiceRouter()
		{
			WebServiceManager.Manager.Startup();	
		}

		#region IHttpHandler implementation
		public void ProcessRequest (HttpContext context)
		{
			//path format: /api/model|message
			HttpRequest request = HttpContext.Current.Request;
			string[] segs = request.Url.Segments;
			string path = request.Url.LocalPath;
			log.Debug(0, segs);
			if( segs.Length < 2 || segs[API] != "api/" )
				return;
			
			StopWatch watch = new StopWatch("ServiceRouter");
			watch.Start();
			
			string verb = request.HttpMethod;
			RestOperation op = RestOperation.Get;

            WebServiceFormat format = WebServiceFormat.Xml;

            if (request.QueryString["format"] != null)
                format = formats[request.QueryString["format"]];
            else if (request.Headers["Accept"] != null)
            {
                if (request.Headers["Accept"].Contains("application/json")
                   || request.Headers["Accept"].Contains("text/javascript"))
                    format = WebServiceFormat.Json;
            }
			
			MessageNode requestMessage = null;
			
			switch( verb.ToUpper() )
			{
			case "GET":
				op = RestOperation.Get;
				break;
			case "POST":
				op = RestOperation.Post;
				break;
			case "PUT":
				op = RestOperation.Put;
				break;
			case "DELETE":
				op = RestOperation.Delete;
				break;
			case "COPY":
				op = RestOperation.Copy;
				break;
			}
			
			HttpFileCollection files = null;
			//if a file is uploaded, we do not plan on supoprting parsing the InputStream in any other way.
			if( request.Files.Count == 0 )
			{
				if( request.InputStream != null && request.InputStream.Length > 0 )
				{
					switch( format )
					{
                    case WebServiceFormat.Xml:
						requestMessage = XmlSerializer.DeserializeXml(request.InputStream);
						break;
                    case WebServiceFormat.Json:
						StreamReader reader = new StreamReader(request.InputStream);
                        String s = reader.ReadToEnd();
						//requestMessage = MessageNode.ConvertFromRaw( (Dictionary<string,object>)JSON.Default.Decode( reader.ReadToEnd() ) );
                        requestMessage = MessageNode.ConvertFromRaw((Dictionary<string, object>)JSON.Default.Decode(s));
						break;
					}
				}
			}
			else
			{
				files = request.Files;
			}

			RequestProcessor processor = WebServiceManager.Manager.GetRequestProcessor(segs);

			if( processor != null )
			{
                IMessageWriter messageWriter = MessageWriterFactory.Create(format, HttpContext.Current.Response.OutputStream);
				log.InfoFormat("Found processor {0} for request {1}", processor, request.Url);
				
				try
				{
                    try
                    {
                        String headerFields = request.Headers["x-5to1-fields"];
                        String queryStringFields = request.QueryString["fields"];
                        NameValueCollection queryStringPlusHeaders = new NameValueCollection(request.Headers);
                        queryStringPlusHeaders.Add(request.QueryString);
                        if (!String.IsNullOrEmpty(headerFields) && String.IsNullOrEmpty(queryStringFields))
                            queryStringPlusHeaders["fields"] = headerFields;
                        
                        Response response = processor.RouteRequest
                            (op,
                             path.Substring(processor.WebServiceDetails.BasePath.Length),
                             queryStringPlusHeaders,
                             requestMessage,
                             messageWriter,
                             files);

                        HttpContext.Current.Response.StatusCode = response.StatusCode;
                        HttpContext.Current.Response.StatusDescription = response.StatusDescription;		
						log.Debug( "response.Cacheability:", response.Cacheability );
						if( (int)response.Cacheability > 0 )
						{							
							HttpContext.Current.Response.Cache.SetCacheability(response.Cacheability );
						}
                        if (response.Expires != -1)
                            HttpContext.Current.Response.Expires = response.Expires;
                    }
                    catch (ValidationException ve)
                    {
                        HttpContext.Current.Response.StatusCode = 409;
                        HttpContext.Current.Response.StatusDescription = "Validation Error";

                        // this is kind of an interesting problem;  we may have some already partially written stuff in here,
                        // some of it may even have been flushed, and here we're going to write some new
                        // stuff.   It's the same problem whether we use IMessageWriter, or the old 
                        // MessageNode paradigm, I think.
                        messageWriter.OpenRoot("error");
                        messageWriter.OpenProperty("validationErrors");
                        messageWriter.OpenList("validationError");
                        foreach (ValidationError error in ve.Errors)
                        {
                            messageWriter.OpenObject();

                            messageWriter.WriteProperty("keyPath", error.Path);
                            messageWriter.WriteProperty("description", error.Problem);
                            messageWriter.WriteProperty("recoverySuggestion", error.Suggestion);

                            messageWriter.CloseObject();
                        }
                        log.Error("Error processing service request", ve);

                        messageWriter.CloseList();
                        messageWriter.CloseProperty();
                        messageWriter.CloseRoot();
                    }

                    switch (format)
                    {
                        case WebServiceFormat.Json:
                            if (null == files)
                                HttpContext.Current.Response.ContentType = "application/json";
                            else
                                HttpContext.Current.Response.ContentType = "text";
                            break;
                        case WebServiceFormat.Xml:
                            HttpContext.Current.Response.ContentType = "text/xml";
                            break;
                    }
				}
				catch(UnauthorizedAccessException ex)				
				{
					HttpContext.Current.Response.ContentType = "text";
					HttpContext.Current.Response.Write( Util.BuildExceptionOutput(ex) );
					HttpContext.Current.Response.StatusCode = 401;
					HttpContext.Current.Response.StatusDescription = "Unauthorized (" + ex.Message + ")";	
					log.Error("Security Error processing service request", ex);
				}
				catch(Exception ex)
				{
					HttpContext.Current.Response.Write( Util.BuildExceptionOutput(ex) );
					HttpContext.Current.Response.StatusCode = 500;
					HttpContext.Current.Response.StatusDescription = ex.Message;
					HttpContext.Current.Response.ContentType = "text";
					log.Error("Error processing service request", ex);
				}
			}
			else
			{
				log.Warn("No processor found for ", request.Url);
				HttpContext.Current.Response.StatusCode = 404;
				HttpContext.Current.Response.StatusDescription = "Not Found (Service Endpoint unknown)";
			}
			watch.Lap("Finished processing request.  Flushing to client.");
			watch.Stop();

            log.InfoFormat("Finished request verb = {0}, url = {1}, totaltime = {2} ms.", verb.ToUpper(), request.Url, (DateTime.Now - watch.StartTime).TotalMilliseconds);
            StopWatch.Summary(log);
		}

		public bool IsReusable {
			get {
				return true;
			}
		}
		#endregion

	}
}
