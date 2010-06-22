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
	/// Summary description for TextBox.
	/// </summary>
    [DefaultProperty("Text")]
	public class Label : Generic
	{
		public override string ClientClass { get { return "Label"; } }
		private const string UpdateFormat = "{0}.SetText({1});";
		private const string AppendFormat = "{0}.AppendText({1});";
		private string text = string.Empty;
		public virtual string Text
		{
			get { return text; }
			set 
			{ 
				text = value;
                if (RootContext != null && RootContext.IsBot)
                {
                    RootContext.HttpContext.Response.Write(this.Text);
                    return;
                }
                
                string output = Util.ToJavaScriptString( textalize && ! string.IsNullOrEmpty( text ) ? Util.Textalize( text ) : text );
                	
				if( this.rendered ) 
					SendCommand( UpdateFormat, ClientId, output );
                else
                	this.SetClientAttribute("html",output);
                RaisePropertyChangedNotification("Text");
			}
		}

        private bool bold;

        public bool Bold
        {
            get { return bold; }
            set 
            { 
                bold = value; 
                SetClientElementStyle("fontWeight",bold ? "'bold'" : "'normal'" ) ;
				RaisePropertyChangedNotification("Bold");
            }
        }
        
        public void MakeLabelFor( Widget w )
        {
        	TagName = "label";
			SetClientElementAttribute("for", "'" + w.ClientId + "'" );
        }
		
		Widget forWidget;
		public Widget For
		{
			get { return forWidget; }
			set { forWidget = value; MakeLabelFor(forWidget); }
		}
	
		private bool textalize = false;
		public bool Textalize { get { return textalize; } 
			set { textalize = value;
				RaisePropertyChangedNotification("Textalize");
			}
		}

		public void Append( string text )
		{
			SendCommand( AppendFormat, ClientId, Util.ToJavaScriptString(text) );
		}
        public Label() { }
		public Label( string id, string text )
		{
			this.Id = id;
			this.Text = text;
		}

		private bool inline;
		public bool Inline
		{
			get { return inline; }
			set { inline = value; SetClientElementStyle("display",inline?"'inline'":"''"); 
				RaisePropertyChangedNotification("Inline");
			}
		}

		public override string ToString()
		{
			return Text;
		}

        public override string DefaultProperty
        {
            get
            {
                return "Text";
            }
        }
        
        public static Label InsertLabel( Widget parent, string text )
		{
			return Label.InsertLabel(parent, "div", text );
		}

		public static Label InsertLabel( Widget parent, string tag, string text )
		{
			Label l = parent.RootContext.CreateWidget<Label>( parent );
			l.TagName = tag;
			l.Text = text;
			return l;
		}
    }
}
