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
using System.Net.Sockets;
using System.IO;

namespace EmergeTk
{
    public class FlashCometWriter : ICometWriter
    {
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(FlashCometWriter));	
		
        StreamWriter sw;
        NetworkStream ns;

        Context context;
        public Context Context
        {
            get { return context; }
            set { context = value; }
        }

        public FlashCometWriter(StreamWriter sw, NetworkStream ns)
        {
            this.sw = sw;
            this.ns = ns;
        }
        static byte[] zero = new byte[] { 0 };

        public void WriteSuccess()
        {
            //noop.
        }

        public void Write(string data)
        {
            sw.Write(data);
            sw.Flush();
            ns.Write(zero, 0, 1);
        }
    }
}
