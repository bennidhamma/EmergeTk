
using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;
using SimpleJson;

namespace EmergeTk.WebServices
{
	public class MessageEndPointArguments
	{
		public MatchCollection Matches;
		public NameValueCollection QueryString;
		public JsonObject InMessage;
		public Response Response;
		public System.Web.HttpFileCollection Files;
        public HttpCacheability Cacheability;
        public int Expires = -1;
	}
}
