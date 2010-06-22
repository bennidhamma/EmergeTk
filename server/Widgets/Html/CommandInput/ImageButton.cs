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
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Widgets.Html
{
    public class ImageButton : Button
    {
        string url;

        public string Url
        {
            get { return url; }
            set { url = value; SetClientElementAttribute("src", Util.ToJavaScriptString(url));
            	RaisePropertyChangedNotification("Url");
            }
        }

        private string width;

        public string Width
        {
            get { return width; }
            set 
            {
                width = value; 
                SetClientElementAttribute("width", Util.ToJavaScriptString(width));  
                RaisePropertyChangedNotification("Width");
            }
        }

        private string height;

        public string Height
        {
            get { return height; }
            set 
            { 
                height = value;
                SetClientElementAttribute("width", Util.ToJavaScriptString(width));
                RaisePropertyChangedNotification("Height");
            }
        }
	

        public ImageButton()
        {
            this.ClassName = "ImageButton";
        }
    }
}
