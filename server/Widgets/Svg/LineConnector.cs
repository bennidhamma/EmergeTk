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
using EmergeTk.Widgets.Svg;

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// Summary description for LineConnector.
	/// </summary>
	public class LineConnector : Line
	{
		private ILocatable first;
		
		/// <summary>
		/// Property First (ILocatable)
		/// </summary>
		public ILocatable First
		{
			get
			{
				return this.first;
			}
			set
			{
				this.first = value;
			}
		}

		private ILocatable second;
		
		/// <summary>
		/// Property Second (ILocatable)
		/// </summary>
		public ILocatable Second
		{
			get
			{
				return this.second;
			}
			set
			{
				this.second = value;
			}
		}

		public LineConnector( ILocatable first, ILocatable second )
		{
			this.first = first;
			this.second = second;

			this.x1 = first.Point.X;
			this.y1 = first.Point.Y;
			this.x2 = second.Point.X;
			this.y2 = second.Point.Y;
		}
	}
}
