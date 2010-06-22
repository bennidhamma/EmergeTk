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
using System.ComponentModel;

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// Summary description for Link.
	/// </summary>
    [DefaultProperty("Text")]
	public class Link : Widget
	{
		private string text;
		
		/// <summary>
		/// Property Label (string)
		/// </summary>
		public string Text
		{
			get
			{
				return this.text;
			}
			set
			{
				this.text = value;
				if( this.rendered )
					SetClientElementProperty( "innerHTML", Util.Quotize(Util.FormatForClient(text)));
				else
                SetClientAttribute("label",Util.Quotize(Util.FormatForClient(text)));
                RaisePropertyChangedNotification("Text");
			}
		}

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
				this.url = value;
				
				if( this.rendered )
					SetClientElementAttribute( "href", Util.Quotize(Util.FormatForClient(url)));
				else
					SetClientAttribute("url",Util.Quotize(Util.FormatForClient(url)));
				if (rendered)
					SetClientElementAttribute("href",Util.ToJavaScriptString(url));
				RaisePropertyChangedNotification("Url");
			}
		}

		public bool OpenInNewWindow {
			get {
				return openInNewWindow;
			}
			set {
				openInNewWindow = value;
				SetClientElementAttribute("target", value ? "'_blank'" : "" );
				RaisePropertyChangedNotification("OpenInNewWindow");
			}
		}
		
		private bool openInNewWindow = true;
	}
}
