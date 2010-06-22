/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Image.cs
 * Description: An image widget.
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
	/// Summary description for Image.
	/// </summary>
	public class Image : Widget
	{
		public override string ClientClass { get { return "emergetk.Image"; } }
		
		private string url;
		
		/// <summary>
		/// Property Url (string)
		/// </summary>
		public string Url
		{
			get
			{
				return this.url;
			}
			set
			{
				string themePath = ThemeManager.Instance.RequestClientPath("Images/" + value );
				if( ! string.IsNullOrEmpty( themePath ) )
				{
					this.url = themePath;
				}
				else
					this.url = value;
                SetClientElementAttribute("src",Util.ToJavaScriptString(url));                
                RaisePropertyChangedNotification("Url");
			}
		}
		
		private bool preload = false;
		public bool Preload { get { return preload; } set { preload = value; SetClientAttribute("pl","1"); } }
		
		private string tip;
		public string Tip 
		{
			get { return tip; }
			set {
				tip = value;
				SetClientElementAttribute("alt",Util.ToJavaScriptString(value));
				SetClientElementAttribute("title",Util.ToJavaScriptString(value));
				RaisePropertyChangedNotification("Tip");
			}
		}
		
		public override string DefaultProperty
		{
			get { return "Url"; }
		}

        private string height;

        public string Height
        {
            get { return height; }
            set { 
            	height = value; 
            	SetClientElementStyle("height", Util.ToJavaScriptString(value));
            	//this.SetClientElementStyle
            	RaisePropertyChangedNotification("Height");
            }
        }

        private string width;

        public string Width
        {
            get { return width; }
            set { width = value; SetClientElementStyle("width", Util.ToJavaScriptString(value)); 
            	RaisePropertyChangedNotification("Width");
            }
        }
        
        public event EventHandler<WidgetEventArgs> OnLoad;
        
        public override void HandleEvents(string evt, string args)
        {
        	if( evt == "OnLoad" )
        	{
        		if( OnLoad != null )
        		{
        			OnLoad( this, new WidgetEventArgs( this, args, null ) );
        		}
        	}
        	else
        		base.HandleEvents(evt,args);
        }
	}
}
