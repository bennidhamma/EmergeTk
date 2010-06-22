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

namespace EmergeTk
{
	/// <summary>
	/// Summary description for Surface.
	/// </summary>
	public abstract class Surface
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Surface));
		
		protected int bytesSent = 0;
		public int BytesSent { get { return bytesSent; } }

		public Surface()
		{
		}

		public abstract void Write( string data );
		
		protected bool started = false, finished = false;
		public virtual void Start(){
			started = true;
		}		
		
		public virtual void End(){
			finished = true;
		}
		
		public virtual void Flush() {
		
		}
	}
}
