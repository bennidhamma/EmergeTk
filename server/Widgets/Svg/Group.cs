/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Group.cs
 * Description: Svg group
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
	/// Summary description for SvgGroup.
	/// </summary>
	public class Group : Widget
	{
		private string transform = null;
        public Group() { }
		public Group( string uid )
		{
			this.Id = uid;
			this.ClientClass = "SvgGroup";
		}

		public void Rotate( float s )
		{
			appendTransform( string.Format( "rotate({0})", s ) );
		}

		public void Scale( float s )
		{
			appendTransform( string.Format( "scale({0})", s ) );
		}

		public void Scale( float sx, float sy )
		{
			appendTransform( string.Format( "scale({0},{1})", sx, sy ) );
		}

        public void SkewX(float x)
        {
            appendTransform(string.Format("skewX({0})", x));
        }

        public void SkewY(float y)
        {
            appendTransform(string.Format("skewX({0})", y));
        }

		public void Translate( int x, int y )
		{
			appendTransform(string.Format( "translate({0},{1})", x, y ));
		}

		private void appendTransform( string next_transform )
		{
			if( transform == null )
				transform = next_transform;
			else
				transform = transform + "," + next_transform;
		}

        public Text DrawText(int x, int y, string text, string align, int size )
        {
            Text t = RootContext.CreateWidget<Text>();
            t.X = x;
            t.Y = y;
            t.InnerText = text;
            t.Align = align;
            t.Size = size;
            Add(t);
            return t;
        }

        public Circle DrawCircle(float x, float y, float r, string fill, string stroke)
        {
            Circle c = RootContext.CreateWidget<Circle>();
            c.Id = getId();
            c.X = x;
            c.Y = y;
            c.R = r;
            c.Fill = fill;
            c.Stroke = stroke;
            Add(c);
            return c;
        }

        int auto = 0;
        private string getId()
        {
            return "c" + auto++;
        }

        public Line DrawLine(int x1, int y1, int x2, int y2, Color color )
        {
            Line l = new Line(getId(), x1, y1, x2, y2, color);
            Add(l);
            return l;
        }

        public Rect DrawRect(int x, int y, int width, int height, string fill)
        {
            Rect r = RootContext.CreateWidget<Rect>();
            r.X = x;
            r.Y = y;
            r.Width = width;
            r.Height = height;
            r.Fill = fill;
            Add(r);
            return r;
        }

        public Rect DrawPath()
        {
            return null;
        }

		public override bool Render(Surface surface)
		{
            ClientArguments["transform"] = Util.Quotize(transform);
			surface.Write( this.GetClientCommand() );
            return true;
		}
	}
}
