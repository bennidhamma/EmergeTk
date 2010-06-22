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
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// Summary description for LabeledTextBox.
	/// </summary>
	public class LabeledWidget<T> : HtmlElement, IDataBindable, IWidgetDecorator where T : Widget, new()
	{
		HtmlElement label;
		Label error;
		Literal labelText;
		T widget;
        IDataBindable db;        

        bool added = false;

        public override void Add(Widget c)
        {
            if (!(c is Label))
            {
                if (added)
                {
                    RemoveChild(widget);
                }
                widget = c as T;
                db = widget as IDataBindable;
                added = true;
            }
            base.Add(c);
        }

		public HtmlElement Label { get {
			setupLabel();
			return label; } }
			
		private void setupLabel()
		{
			if( label == null )
			{
				label = RootContext.CreateWidget<HtmlElement>();
				Insert(label, 0);
				labelText = RootContext.CreateWidget<Literal>(label);
			}
		
		}
			
		public string LabelText {
			get {
				setupLabel();
				return labelText.Html;				
			}
			set {
				setupLabel();
				labelText.Html = value;
				RaisePropertyChangedNotification("LabelText");
			}
		}
		
        public T Widget
        {
            get
            {
                if (widget == null) 
                {
                    widget = RootContext.CreateWidget<T>();
                    Label.SetClientElementAttribute("for", "'" + widget.ClientId + "'" );
                    Add(widget);
                    added = true;
                }
                return widget;
            }
            set
            {              
                Add(value);
                Label.SetClientElementAttribute("for", "'" + widget.ClientId + "'" );            
                RaisePropertyChangedNotification("Widget");
            }
        }

        public override void Initialize()
        {
        	TagName = "div";
        	
			Label.Id = "_label";
			Label.TagName = "label";
            
            if (!added)
            {
                widget = RootContext.CreateWidget<T>();
                Add(widget);
                Label.SetClientElementAttribute("for", "'" + widget.ClientId + "'" );
                added = true;
            }
        }
		
		public void SetError( string text )
		{
			if( text == null && error != null )
			{
				error.Remove();
			}
			else if( text != null && error != null )
			{
				error.Text = text;
			}
			else if( text != null && error == null )
			{
				error = RootContext.CreateWidget<Label>();
				error.ClassName = "error";
				error.Text = text;
				error.Inline = true;
				this.Add(error);
			}
		}

		public override bool SetAttribute(string Name, string Value)
		{
			switch( Name )
			{
				case "Label":
					labelText.Html = Value;
					//Label.Text = Value;
					break;
				case "Id":
					this.Id = Value;
					break;
                case "Model":
                    break;
                case "Visible":
                    this.Visible = bool.Parse(Value);
                    break;
				default:
					return Widget.SetAttribute( Name, Value );
			}
			return true;
		}
		
		public override object this[string k]
		{
			get { try { return base[k]; } catch { if( db != null ) return db[k]; } return null; }
			set { base[k] = value; }
		}

        override public object Value
        {
            get
            {
            	if( db != null )
                	return db.Value;
                else
                	return null;
            }
            set
            {
            	if( db != null )
                	db.Value = value.ToString();
            }
        }

        override public string DefaultProperty
        {
            get { if( db != null ) return db.DefaultProperty; else return "Value"; }
        }

        #region IWidgetDecorator Members

        Widget IWidgetDecorator.Widget
        {
            get
            {
                return widget;
            }
            set
            {
                if (value is T)
                    widget = value as T;
                RaisePropertyChangedNotification("Widget");
            }
        }

        #endregion
    }
}
