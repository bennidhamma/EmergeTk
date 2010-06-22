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
using System.Threading;

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// Summary description for Poller.
	/// </summary>
	public class Poller : Widget
	{
		public int Interval
		{
			get { return interval; }
            set { interval = value; this.SetClientProperty("interval", value.ToString()); 
            	RaisePropertyChangedNotification("Interval");
            }
		}

		// default to 5 seconds
		private int interval = 5;

		public Poller(){}

		public Poller( string id, int interval )
		{
			this.Id = id;
			this.interval = interval;
		}

		public override bool Render(Surface surface)
		{
            base.Render(surface);
            //InvokeClientMethod("Start");
            return true;
		}

		public void Start()
		{
			InvokeClientMethod("Start");
		}

		public void Stop()
		{
			InvokeClientMethod("Stop");
		}

		public event System.EventHandler OnPoll;

		public override void HandleEvents(string evt, string args)
		{
			if( evt == "OnPoll" && OnPoll != null )
			{
				OnPoll( this, new EventArgs() );
			}
            else
            {
                base.HandleEvents(evt, args);
            }
		}
	}
}
