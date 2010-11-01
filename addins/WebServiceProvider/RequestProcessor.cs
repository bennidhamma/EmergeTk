using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace EmergeTk.WebServices
{
	public class RequestProcessor
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(RequestProcessor));
		
		public WebServiceAttribute WebServiceDetails { get; set; }

		public List<MessageServiceEndPointAttribute> EndPoints {
			get {
				return endPoints;
			}
		}
		
		public IMessageServiceManager ServiceManager { get; set; }
		
		public RequestProcessor (WebServiceAttribute attribute)
		{
			WebServiceDetails = attribute;
		}
		
		List<MessageServiceEndPointAttribute> endPoints = new List<MessageServiceEndPointAttribute>();
		
		public void AddMessageEndPoint(MessageServiceEndPointAttribute endPoint )
		{
			endPoints.Add( endPoint );
		}
		
		public Response 
        RouteRequest(
            RestOperation verb, 
            string subPath, 
            NameValueCollection args, 
            MessageNode message, 
            IMessageWriter writer, 
            HttpFileCollection files )
		{
			log.Debug("routing request", verb, subPath, args, message );
			Response response = new Response();
			if( HttpContext.Current != null && HttpContext.Current.Request.Headers["x-5to1-expires"] != null )
			{
				//string expire = request.QueryString["expire"];
				//context.Response.AddHeader("Cache-Control",string.Format("max-age={0}, public", expire ) );
				//context.Response.Expires = int.Parse( request.QueryString["expire"] );
				response.Expires = int.Parse( HttpContext.Current.Request.Headers["x-5to1-expires"] );
				response.Cacheability = HttpCacheability.Public;				
			}
			else
			{
				response.Expires = 0;
                if (HttpContext.Current != null)
				    HttpContext.Current.Response.Cache.SetMaxAge(new TimeSpan(0));
			}
			log.Debug("caching:", response.Expires, response.Cacheability);
			response.StatusCode = 404;
			response.StatusDescription = "Service not found.";
            response.Writer = writer;
			for( int i = 0; i < endPoints.Count; i++ )
			{
				log.DebugFormat("Testing verbs: {0}, pattern: {1}", endPoints[i].Verb, endPoints[i].Pattern);
				
				//first check to see if we are a match for verb
				if( (verb & endPoints[i].Verb) != verb )
				{
					continue;
				}
				
				//now check and see if we match on the pattern
				MatchCollection m = endPoints[i].Regex.Matches(subPath);
				if( m.Count > 0 )
				{
					//we found a valid match.  exit.
					log.Debug("Found match: " + verb + " " + m[0].Captures[0].Value);

                    if (WebServiceManager.DoAuth())
    					ServiceManager.Authorize( verb, endPoints[i].MethodName, message );

					//consider passing a response into the endpoint call as a ref.
					response.StatusCode = 200;
					response.StatusDescription = "OK";
					MessageEndPointArguments arguments = new MessageEndPointArguments();
					arguments.Cacheability = response.Cacheability;
					arguments.InMessage = message;
					arguments.Matches = m;
					arguments.QueryString = args;
					arguments.Response = response;
					arguments.Files = files;
					endPoints[i].EndPoint(arguments);
                    BuildResponse(response, arguments);
					break;
				}
				else
					log.Debug("no match");
			}
			
			return response;
		}

        private void BuildResponse(Response resp, MessageEndPointArguments arguments)
        {
            resp.Cacheability = arguments.Cacheability;;
            resp.Expires = arguments.Expires;
        }
	}
}