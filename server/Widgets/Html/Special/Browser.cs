/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Browser.cs
 * Description: an html web browser widget.  Seem silly?  Maybe.  But it is useful if you want to embed and control a browser in
 * your application.
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
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace EmergeTk.Widgets.Html
{
    public class Browser : Pane
    {
        static Dictionary<string, object> urlLocks = new Dictionary<string, object>();

        private string source;

        public string Source
        {
            get { return source; }
            set 
            {
                source = value;
                Uri uri;
                if( !Uri.TryCreate(source,UriKind.Absolute, out uri ) )
                {
                    Paint("Invalid uri.");
                    return;
                }
                if( source != sourceOnClient )
                {
                    SetClientAttribute("source", Util.ToJavaScriptString(value));                                
                    sourceOnClient = value;
                }
                string html = string.Empty;
                if (RootContext.HttpContext.Cache[source] != null)
                {
                    html = RootContext.HttpContext.Cache[source] as string;
                }
                else
                {
                    urlLocks[source] = new object();
                    lock (urlLocks[source])
                    {
                        if (RootContext.HttpContext.Cache[source] == null)
                        {
                            System.Net.WebClient wc = new WebClient();
                            StreamReader sr = new StreamReader(wc.OpenRead(source));
                            html = sr.ReadToEnd();
                            RootContext.HttpContext.Cache.Add(source, html, null, DateTime.Now.AddMinutes(20),
                                System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal,
                                null);
                        }
                        else
                        {
                            html = RootContext.HttpContext.Cache[source] as string;
                        }
                    }
                }
                
                Paint(html);
                RaisePropertyChangedNotification("Source");  
            }
        }

        public void Paint(string html)
        {
            Regex r = new Regex(@"<!DOCTYPE .*?>|</?HTML.*?>|</?BODY.*?>|</?META.*?>|</?HEAD.*?>", 
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline );
            html = r.Replace(html, "", -1);
            InvokeClientMethod("Paint", Util.ToJavaScriptString(html));
        }

        private string sourceOnClient;
        public override void HandleEvents(string evt, string args)
        {
            if (evt == "OnChanged")
            {
                sourceOnClient = args;
                Source = args;
            }
            base.HandleEvents(evt, args);
        }
    }
}
