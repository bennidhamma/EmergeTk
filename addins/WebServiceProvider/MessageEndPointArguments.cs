
using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;

namespace EmergeTk.WebServices
{
	public class MessageEndPointArguments
	{
		public MatchCollection Matches;
		public NameValueCollection QueryString;
		public MessageNode InMessage;
		public Response Response;
		public System.Web.HttpFileCollection Files;
        public HttpCacheability Cacheability;
        public int Expires = -1;
	}
}
