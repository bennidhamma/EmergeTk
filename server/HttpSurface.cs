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
using System.Web;
using System.Text;

namespace EmergeTk
{
	/// <summary>
	/// Summary description for HtmlSurface.
	/// </summary>
	public class HttpSurface : Surface
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(HttpSurface));
		
		HttpContext context;
		public HttpSurface( HttpContext context )
		{
			this.context = context;
		}

		StringBuilder writeBuffer;
		
		public override void Write(string data)
		{
			if( HttpContext.Current == null )
				return;
				
            if (string.IsNullOrEmpty(data))
                return;

			if( !started )
			{
				if( writeBuffer == null )
					writeBuffer = new StringBuilder();
				writeBuffer.Append(data);
				return;
			}
			if( writeBuffer != null )
				Flush();
			bytesSent += data.Length;
            HttpContext.Current.Response.Write(data);
		}
		
		public override void Flush()
		{
			if( writeBuffer != null && context != null && writeBuffer.Length > 0 )
			{
				bytesSent += writeBuffer.Length;
				HttpContext.Current.Response.Write( writeBuffer.ToString() );
				writeBuffer = null;
			}
		}

	}
}
