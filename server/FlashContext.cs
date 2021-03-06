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
using System.Threading;

namespace EmergeTk
{
    public class FlashContext : Context
    {
		private new static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(FlashContext));	
		
        private Dictionary<string, string> initialVariables = new Dictionary<string, string>();

        public FlashContext()
        {
            DocumentHeader = string.Empty;
            DocumentFooter = string.Empty;
        }

        public void SetVariable(string key, string val)
        {
            initialVariables[key] = HttpContext.Server.UrlEncode(val);
        }

        public void RemoveVariable(string key)
        {
            initialVariables.Remove(key);
        }

        public override void Initialize()
        {
            initialVariables["sessionId"] = Util.ToJavaScriptString(HttpContext.Session.SessionID);
            SendCommand(JSON.Default.HashToJSON(initialVariables));
        }

        public override void SocketDisconnected()
        {
            TimerCallback tc = new TimerCallback(socketDisconnectedCallback);
            new Timer(tc, null, 60000, Timeout.Infinite);
        }

        private void socketDisconnectedCallback(object o)
        {
            try
            {
            	log.Debug("socketDisconnectedCallback");
                Unregister();
            }
            catch ( Exception e )
            {
            	log.Error( "Socket disconnected", Util.BuildExceptionOutput( e ) );
            }
        }
    }
}
