/**
 * Project: emergetk: stateful web framework for the masses
 * File name: FadingAlert.cs
 * Description: An alert that will disappear after a specified period.
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
using System.Threading;

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// Summary description for FadingAlert.
	/// </summary>
	public class FadingAlert : Widget
	{
		private string message;

		public FadingAlert( string uid, string message, int timeout )
		{
			this.ClientClass = "FadingAlert";
			this.Id = uid;
			this.message = message;
			TimerCallback cb = new TimerCallback( Remove );
			new Timer( cb, this, timeout, Timeout.Infinite );
		}

		public void Remove( object o )
		{
			this.RootContext.RemoveWidget( this );
		}

		public override bool Render(Surface surface)
		{
            ClientArguments["html"] = Util.Quotize(message);
			surface.Write( this.GetClientCommand() );
            return true;
		}

	}
}
