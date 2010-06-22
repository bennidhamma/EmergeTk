/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Text.cs
 * Description: Svg text
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

namespace EmergeTk.Widgets.Svg
{
	/// <summary>
	/// Summary description for SvgText.
	/// </summary>
	public class Text : Widget
	{
		private int x, y, size;
		string text, align;

        public int X { get { return x; } set { x = value; } }
        public int Y { get { return y; } set { y = value; } }
        public int Size { get { return size; } set { size = value; ClientArguments["fontSize"] = size.ToString(); } }
        public string InnerText 
        { 
            get 
            { 
                return text; 
            } 
            set 
            { 
                text = value;
                if (rendered)
                    InvokeClientMethod("SetText", Util.Quotize(text));
            }
        }
        public string Align { get { return align; } set { align = value; } }

        public Text() { }

		public Text( string id, int x, int y, string text, string align, int size )
		{
			this.ClientClass = "SvgText";
			this.Id = id;
			this.x = x;
			this.y = y;
			this.text = text;
			this.align = align;
            ClientArguments["fontSize"] = size.ToString();
		}

		public override bool Render(Surface surface)
		{
            ClientArguments["x"] = x.ToString();
            ClientArguments["y"] = y.ToString();
            ClientArguments["text"] = Util.Quotize(Util.FormatForClient(text));
            ClientArguments["align"] = Util.Quotize(align);
			surface.Write( GetClientCommand() );
            return true;
		}

	}
}
