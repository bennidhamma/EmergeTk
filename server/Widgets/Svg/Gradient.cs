/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Gradient.cs
 * Description: Svg gradient
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
using EmergeTk;

namespace EmergeTk.Widgets.Svg
{
    public enum GradientType
    {
        linearGradient,
        radialGradient
    }
	/// <summary>
	/// Summary description for SvgRadialGradient.
	/// </summary>
	public class Gradient : Widget
	{
		private string cx;
		
		/// <summary>
		/// Property Cx (int)
		/// </summary>
		public string Cx
		{
			get
			{
				return this.cx;
			}
			set
			{
				this.cx = value;
                SetClientAttribute("cx", Util.ToJavaScriptString(cx));
			}
		}

		private string cy;
		
		/// <summary>
		/// Property Cy (int)
		/// </summary>
		public string Cy
		{
			get
			{
				return this.cy;
			}
			set
			{
                this.cy = value;
                SetClientAttribute("cy", Util.ToJavaScriptString(cy));
			}
		}

		private string gradientId;
		
		/// <summary>
		/// Property GradientId (string)
		/// </summary>
		public string GradientId
		{
			get
			{
				return this.gradientId;
			}
			set
			{
				this.gradientId = value;
                SetClientAttribute("gradientId", Util.ToJavaScriptString(gradientId));
			}
		}

		private string stops;
		
		/// <summary>
		/// Property Stops (string)
		/// </summary>
		public string Stops
		{
			get
			{
				return this.stops;
			}
			set
			{
                this.stops = value;
                SetClientAttribute("stops", Util.ToJavaScriptString(stops));
			}
		}

        private GradientType type;

        public GradientType Type
        {
            get { return type; }
            set
            {
                type = value; SetClientAttribute("gradientType", Util.ToJavaScriptString(type.ToString()));
            }
        }

        private Vector direction;

        public Vector Direction
        {
            get { return direction; }
            set { 
                direction = value;
                SetClientAttribute("x1", 0);
                SetClientAttribute("y1", 0);
                SetClientAttribute("x2", Util.ToJavaScriptString(value.X.ToString()));
                SetClientAttribute("y2", Util.ToJavaScriptString(value.Y.ToString()));
            }
        }
	}
}
