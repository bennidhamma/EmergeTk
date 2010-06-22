/**
 * Project: emergetk: stateful web framework for the masses
 * File name: .cs
 * Description:
 *   
 * @author Ben Joldersma, All-In-One Creations, Ltd. http://all-in-one-creations.net, Copyright (C) 2006.
 *   
 * @see The GNU Public License (GPL)
 */
/* 
 * This program is free software; you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation; either version 2 of the License, or 
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License along 
 * with this program; if not, write to the Free Software Foundation, Inc., 
 * 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EmergeTk
{
    class ContextHandler : IHttpHandler, IRequiresSessionState
    {
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ContextHandler));			
		
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
				//log.Debug("ContextHandler");
				
            	//log.Debug("processing request ", context, context.Request.Url, context.Request.Form.ToString());

				//context.Response.ContentType = "text/html; charset=utf8
         
                string type = context.Request.QueryString["type"];
                if (type == null)
                {
                    Regex r = new Regex(@"(\w+)\.(context|xml)");
                    Match m = r.Match(context.Request.Path);
                    if (m.Success)
                    {
                        type = m.Groups[1].Value;
                    }
                }

				if (type != null)
                    EmergeTk.Context.Connect(type, TypeLoader.GetType(type));
                else
                    context.Response.Write("Could not find a relevant context.");
            }			
            catch(Exception e)
            {
				String msg = string.Format("Context error for URL {0}\n, Post Data: {1}\n Error Info: {2}\n\n",
                	context.Request.Url, context.Request.Form.ToString(), Util.BuildExceptionOutput(e));
				
            	log.Error(msg);
				
				context.Response.Write("<html><body><pre>Error! \n\n" + msg + "\n\n</pre></body></html>");
				
                if( EmergeTk.Context.Current != null && EmergeTk.Context.Current.Surface != null )
                {
                	EmergeTk.Context.Current.Surface.Flush();
                }
                else
                {
					context.Response.Write( string.Format("Context error for URL {0}<BR>, Post Data: {1}<BR> Error Info: {2}\n\n",
                	context.Request.Url, context.Request.Form.ToString(), Util.BuildExceptionOutput(e)).Replace("\n","<BR>") );
				}
            }
            
        }

        #endregion
    }
}
