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
using System.Text;
using System.IO;

namespace EmergeTk
{
    class HtmlCometWriter : ICometWriter
    {
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(HtmlCometWriter));		
		
        #region ICometWriter Members

        StreamWriter sw;
        Context context;
        public Context Context
        {
            get { return context; }
            set { context = value; }
        }

        public HtmlCometWriter(StreamWriter sw )
        {
            this.sw = sw;
        }

        public void Write(string data)
        {
            sw.WriteLine("<SCRIPT>parent.eval(" + Util.ToJavaScriptString(data) + ")</SCRIPT>");
            sw.Flush();
        }

        public void WriteSuccess()
        {
            sw.WriteLine("HTTP/1.1 200 OK");
            sw.WriteLine("Content-Type: text/html");
            sw.WriteLine("Connection: close");
            sw.WriteLine();
            sw.WriteLine("<SCRIPT>document.domain='" + context.Host + "';</SCRIPT>");
            Write("console.debug('Comet socket loaded.');");
        }

        #endregion
    }
}
