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
    public class SelectItem : Widget, IDataBindable
    {
    	public override void Initialize()
    	{
    		Mode = mode;
    	}
    	
        private SelectionMode mode = SelectionMode.Multiple;

        public SelectionMode Mode
        {
            get { return mode; }
            set { mode = value; SetClientElementAttribute("type", mode == SelectionMode.Single ? "'radio'" : "'checkbox'"); 
            	RaisePropertyChangedNotification("Mode");
            }
        }

        private string group;

        public string Group
        {
            get { return group; }
            set { group = value; 
            	SetClientElementAttribute("name", Util.ToJavaScriptString(value) );
            	RaisePropertyChangedNotification("Group");
            }
        }

        private bool selected;

        public bool Selected
        {
            get { return selected; }
            set {
				
                if (selected != value)
                {
                    selected = value;
                    //SetClientElementAttribute("checked", selected ? "true" : "null");
                    InvokeClientMethod("SetValue", selected ? "1" : "0");
                    RaisePropertyChangedNotification("Selected");
                }
            }
        }
	

        public override void HandleEvents(string evt, string args)
        {
			//System.Console.WriteLine("HandleEvents evt: " + evt + " args: "+ args);
            if (evt == "OnChanged" )
            {
				bool oldSelected = selected;
                Selected = args == "true"||args=="on"||args=="1";
                InvokeChangedEvent(oldSelected, selected);
            }
            base.HandleEvents(evt, args);
        }

        #region IDataBindable Members


        override public object Value
        {
            get
            {
                return Selected;
            }
            set
            {
                Selected = (bool)value;
            }
        }

        override public string DefaultProperty
        {
            get { return "Selected"; }
        }

        #endregion
    }
}
