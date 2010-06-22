using System;
using System.Text.RegularExpressions;

namespace EmergeTk.WebServices
{


	public class MessageServiceEndPointAttribute : Attribute
	{
		/// <summary>
		/// Regular Expression pattern to append to the web service base path to determine if the current
		/// request should be routed to this endpoint.
		/// </summary>
		public string Pattern {
			get;
			set;
		}

		/// <summary>
		/// Default specification is to allow all verbs.
		/// </summary>
		public RestOperation Verb {
			get;
			set;
		}
		
		public string Description {
			get;
			set;
		}
		
		internal Regex Regex
		{
			get;
			set;
		}
		
		internal MessageEndPoint EndPoint
		{
			get;
			set;
		}
		
		internal string MethodName
		{
			get;
			set;
		}

		public MessageServiceEndPointAttribute (string pattern)
		{
			Pattern = pattern;
			Verb = RestOperation.Get | RestOperation.Post | RestOperation.Put | RestOperation.Delete;
		}
	}
	
	public class MessageDescriptionAttribute : Attribute
	{
		public string Description { get; set; }
		
		public MessageDescriptionAttribute(string description)
		{
			Description = description;
		}
	}
}
