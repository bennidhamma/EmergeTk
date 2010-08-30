
using System;
using System.Web;

namespace EmergeTk.WebServices
{
	public class Response
	{
        private int expires = -1;

		public int StatusCode { get; set; }
		public string StatusDescription { get; set; }
		public IMessageWriter Writer { get; set; }
        public HttpCacheability Cacheability { get; set; }
        public int Expires
        {
            get
            {
                return expires;
            }
            set
            {
                expires = value;
            }
        }
		
		public Response ()
		{
		}
	}
}
