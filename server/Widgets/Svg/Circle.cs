/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Circle.cs
 * Description: Svg circle
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
	/// Summary description for SvgCircle.
	/// </summary>
	public class Circle : Widget
	{
		float x, y, r;
		string fill, stroke;

        virtual public float X { get { return x; } set { x = value; SetClientElementAttribute("cx",x.ToString("F0")); } }
        virtual public float Y { get { return y; } set { y = value; SetClientElementAttribute("cy", y.ToString("F0")); } }
        virtual public float R { get { return r; } set { r = value; SetClientElementAttribute("_r", r.ToString("F0")); } }
        virtual public string Fill { get { return fill; } set { fill = value; SetClientElementAttribute("fill", Util.Quotize(fill)); } }
        virtual public string Stroke { get { return stroke; } set { stroke = value; SetClientElementAttribute("stroke", Util.Quotize(stroke)); } }

        public Circle() { }

        public Circle(string uid, float X, float Y, float R, string Fill, string Stroke)
		{
			this.ClientClass = "SvgCircle";
			//this.ClassName = "foo";
			this.Id = uid;
			x = X;
			y = Y;
			r = R;
			fill = Fill;
			stroke = Stroke;
		}

		public override bool Render(Surface surface)
		{
            ClientArguments["x"] = ((int)x).ToString();
            ClientArguments["y"] = ((int)y).ToString();
            ClientArguments["_r"] = ((int)r).ToString();
            ClientArguments["fill"] = Util.Quotize(fill);
            ClientArguments["stroke"] = Util.Quotize(stroke);
			surface.Write(this.GetClientCommand());
            return true;
		}
	}
}
