/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Button.cs
 * Description: a button widget.
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
	public interface IButton {
		string Label {get; set; }
		string Arg { get; set; }
	}
	/// <summary>
	/// Summary description for Button.
	/// </summary>
	public class Button : Widget, IButton
	{
		private const string UpdateFormat = "{0}.SetText({1});";

		private string label;
		public string Label
		{
			get { return label; }
			set 
			{ 
				label = value;
                ClientArguments["label"] = Util.ToJavaScriptString(label);
				Update();
				RaisePropertyChangedNotification("Label");
			}
		}

		private string arg;
		
		/// <summary>
		/// Property Arg (string)
		/// </summary>
		public string Arg
		{
			get
			{
				return this.arg;
			}
			set
			{
				this.arg = value;
				RaisePropertyChangedNotification("Arg");
			}
		}

		public Button()
		{
			//this.ClientClass = "Button";
		}

		public Button( string id, string label )
		{
			this.Id = id;
			//this.ClientClass = "Button";
			this.label = label;
		}

		public override void Update()
		{
			if( this.rendered ) 
				SendCommand( UpdateFormat, ClientId, Util.ToJavaScriptString(label) );
		}

		public override bool SetAttribute(string Name, string Value)
		{
			switch( Name )
			{
				case "Text":
					Label = Value;
					break;
				default:
					return base.SetAttribute( Name, Value );
			}

			return true;
		}

        public override string DefaultProperty
        {
            get
            {
                return "Label";
            }
        }
	}
}
