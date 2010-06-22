/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Rect.cs
 * Description: Svg rectangle (rect)
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
	/// Summary description for SvgRect.
	/// </summary>
	public class Rect : Widget
	{
		int x, y, width, height;
		string color;

        public int X { get { return x; } set { x = value; } }
        public int Y { get { return y; } set { y = value; } }
        public int Width { get { return width; } set { width = value; } }
        public int Height { get { return height; } set { height = value; } }
        public string Fill { get { return color; } set { color = value; } }

        public Rect() { }

		public Rect( string uid, int x, int y, int width, int height, string color )
		{
			this.ClientClass = "SvgRect";		
			this.Id = uid;
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
			this.color = color;
		}

		public override bool Render(Surface surface)
		{
            ClientArguments["x"] = x.ToString();
            ClientArguments["y"] = y.ToString();
            ClientArguments["width"] = width.ToString();
            ClientArguments["height"] = height.ToString();
            ClientArguments["color"] = Util.Quotize(color);
			surface.Write( GetClientCommand() );
            return true;
		}
	}
}
