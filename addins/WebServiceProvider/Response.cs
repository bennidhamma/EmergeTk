
using System;

namespace EmergeTk.WebServices
{
	public class Response
	{
        private int expires = -1;

		public int StatusCode { get; set; }
		public string StatusDescription { get; set; }
		public IMessageWriter Writer { get; set; }
        public String CacheControl { get; set; }
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
