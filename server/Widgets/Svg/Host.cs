/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Host.cs
 * Description: generates a root SVG element.
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
using System.Drawing;

namespace EmergeTk.Widgets.Svg
{

	/// <summary>
	/// Summary description for SvgHost.
	/// </summary>
	public class Host : Widget
	{
		private string width;
		
		/// <summary>
		/// Property Width (int)
		/// </summary>
		public string Width
		{
			get
			{
				return this.width;
			}
			set
			{
				this.width = value;
			}
		}

		private string height;
		
		/// <summary>
		/// Property Height (int)
		/// </summary>
		public string Height
		{
			get
			{
				return this.height;
			}
			set
			{
				this.height = value;
			}
		}

		private Rectangle viewBox;
		private bool vbSet = false;
		
		/// <summary>
		/// Property ViewBox (string)
		/// </summary>
		public Rectangle ViewBox
		{
			get
			{
				return this.viewBox;
			}
			set
			{
				vbSet = true;
				this.viewBox = value;
			}
		}

		private Widget pannableElement;
		
		/// <summary>
		/// Property IsPannable (bool)
		/// </summary>
		public Widget PannableElement
		{
			get
			{
				return this.pannableElement;
			}
			set
			{
				this.pannableElement = value;
			}
		}

		public event EventHandler<SvgPanEventArgs> OnPan;

		public override void HandleEvents(string evt, string args)
		{
			if( evt == "OnPan" && OnPan != null )
			{
				OnPan( this, new SvgPanEventArgs() );
			}
            else
            {
                base.HandleEvents(evt, args);
            }
		}

		public Host()
		{
		}

		public override bool Render(Surface surface)
		{
			string vb = vbSet ? string.Format( "{0} {1} {2} {3}", viewBox.X.ToString(), viewBox.Y, viewBox.Width, viewBox.Height ) :  string.Empty ;
            ClientArguments["width"] = Util.Quotize(width);
            ClientArguments["height"] = Util.Quotize(height);
            ClientArguments["pannableElement"] = pannableElement != null ? pannableElement.UID : "null" ;
            ClientArguments["vb"] = Util.Quotize(vb);
			surface.Write( GetClientCommand() );
            return true;
		}

	}
}
