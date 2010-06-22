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

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// Summary description for Pane.
	/// </summary>
	
	public class Pane : Widget
	{
		string top, left, height, width, label;

		public Pane()
		{
		}

		public Pane( string Id, string Top, string Left, string Height, string Width )
		{
			this.Id = Id;
			this.top = Top;
			this.left = Left;
			this.height = Height;
			this.width = Width;
		}

        public string Top { get { return top; } set 
            { 
                top = value;
                if (!rendered) ClientArguments["top"] = Util.Quotize(top);
                else SetClientElementStyle("top", top, true);
                RaisePropertyChangedNotification("Top");
            } 
        }
        public string Left { get { return left; } set 
            { 
                left = value;
                if (!rendered) ClientArguments["left"] = Util.Quotize(left);
                else SetClientElementStyle("left", left, true);
                RaisePropertyChangedNotification("Left");
            }
        }
        public string Height { get { return height; } set 
            { 
                height = value;
                if (!rendered) ClientArguments["height"] = Util.Quotize(height);
                else SetClientElementStyle("height", height, true);
                RaisePropertyChangedNotification("Height");
            }
        }
        public string Width { get { return width; } set 
            { 
                width = value;
                if (!rendered) ClientArguments["width"] = Util.Quotize(width);
                else SetClientElementStyle("width", width, true);
                RaisePropertyChangedNotification("Width");
            }
        }
      	public string Label { get { return label; } set {
        	label = value; 
        	ClientArguments["label"] = Util.ToJavaScriptString(label);
        	RaisePropertyChangedNotification("Label"); 
        	}
       }
	}
}