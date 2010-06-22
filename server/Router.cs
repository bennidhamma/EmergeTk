using System;
using System.Collections;
using System.Configuration;
using System.Reflection;
using System.Web;
using EmergeTk.Model;

namespace EmergeTk
{
	public class Router : IHttpModule
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Router));
		private static readonly string[] passThroughExtensions= new string[] {
                                            ".asmx", ".aspx", ".ashx", ".jsonx",
                                            ".js", ".html", ".htm", ".css",
                                            ".png", ".gif", ".jpg", ".jpeg",
                                            ".ico"
                                        };


		public Router()
		{
		}

		#region IHttpModule Members

		public void Init(HttpApplication context)
		{
            context.BeginRequest += new EventHandler(route);
		}

		public void Dispose()
		{
			// TODO:  Add Router.Dispose implementation
		}

		#endregion

		private void route(object sender, EventArgs e)
		{
			
			string[] segs = HttpContext.Current.Request.Url.Segments;
            string type = segs[ segs.Length -1 ];
				
			if( segs.Length > 1 && segs[1] == "api/" )
				return;
			
            if (String.IsNullOrEmpty(type) || type == "/")
            {
                string defaultDocument = ConfigurationManager.AppSettings["MonoServerDefaultIndexFiles"];
                if (String.IsNullOrEmpty(defaultDocument))
                {
                    defaultDocument = "index.aspx";
                }
                HttpContext.Current.Response.Redirect(defaultDocument, true);
                return;
            }

            string filePath= HttpContext.Current.Request.FilePath;
            string extension= System.IO.Path.GetExtension(filePath).ToLower();
            
			if(-1!=Array.IndexOf(passThroughExtensions, extension))
			{
			    //log.Debug("Passing through request for: " + filePath);
				return;
            }
            
			string query = HttpContext.Current.Request.Url.Query;
			if( query != null && query.Length > 0 )
			{
				query = '&' + query.Substring(1);
			}
			else
			{
				query = string.Empty;
			}

			if( ! string.IsNullOrEmpty( type ) )
			{
				//log.Debug("Testing for type", type, type.Replace(Setting.VirtualRoot,"") );
				if( Setting.VirtualRoot != "" )
					type = type.Replace(Setting.VirtualRoot,"");
			}
			
			if( string.IsNullOrEmpty( type ) )
				return;
			
            Type t = TypeLoader.GetType(type);
            
            if( t == null )
            {
            	type = HttpContext.Current.Request.Url.LocalPath;
            	if( Setting.VirtualRoot != "" )
					type = type.Replace(Setting.VirtualRoot,"");
				type = type.Replace('/','.').Trim('.');
            	t = TypeLoader.GetType(type);            	
            }
            if( t == null )
            {
            	t = TypeLoader.GetType(Setting.DefaultContext);
            	type = t.FullName;
            
            }
            
            log.Debug("Router routing to type ", t );
            
			if( t != null )
			{
				string path = Setting.VirtualRoot + "/index.context";
				query = "type=" + type + query;
				HttpContext.Current.RewritePath( path,null,query);
			}
			else
				log.Error("Could not find type for ", type );
		}
	}
}
