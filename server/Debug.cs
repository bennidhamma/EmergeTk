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
using System.Web;
using System.Diagnostics;

namespace EmergeTk
{
    public class Debug
    {
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Debug));	
		
    	public static void DumpStack()
    	{
    		Trace( System.Environment.StackTrace );
    	}
    	
    	public static void Trace(object o)
    	{
    		StackTrace st = new StackTrace( true );
			Trace(string.Format("Tracing object {0} of type {1} :: {2}", o != null ? o.ToString() : "NULL", o != null ? o.GetType() : null, st.GetFrame(1) ) );
    	}
    	
        public static void Trace(string message, params object[] args)
        {
        	#if DEBUG
        	try
        	{
	        	if( args != null )
	        		for(int i = 0; i < args.Length; i++ )
	        			if( args[i] == null )
	        				args[i] = "NULL";
	       		message = string.Format( "TRACE: " + message, args);
	       }
	       catch
	       {
	       }
	       System.Diagnostics.Trace.Write( message );
	       log.Debug(message);	       	
        	#endif
        }
    }
}
