/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Line.cs
 * Description: Svg line
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
	/// Summary description for SvgLine.
	/// </summary>
	public class Line : Widget
	{
		protected int x1, x2, y1, y2;
		Color color;

		public Line()
		{
			this.ClientClass = "SvgLine";
		}

		public Line( string uid, int x1, int y1, int x2, int y2, Color color )
		{
			this.Id = uid;
			this.x1 = x1;
			this.x2 = x2;
			this.y1 = y1;
			this.y2 = y2;
			this.color = color;
		}

		public override bool Render(Surface surface)
		{
            ClientArguments["x1"] = x1.ToString();
            ClientArguments["y1"] = y1.ToString();
            ClientArguments["x2"] = x2.ToString();
            ClientArguments["y2"] = y2.ToString();
            ClientArguments["color"] = Util.Quotize(color.Name);
			surface.Write( GetClientCommand() );
            return true;
		}
	}
}
