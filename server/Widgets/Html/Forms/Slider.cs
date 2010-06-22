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
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
    public class Slider : Widget, IDataBindable
    {
        private int val,min=0,max=100,currentValueOnClient;

        public int SelectedValue
        {
            get { return val; }
            set { 
                val = value; 
                if( val != currentValueOnClient ) SetClientAttribute("val", val);
                currentValueOnClient = val;
                RaisePropertyChangedNotification("SelectedValue");
            }
        }

        public int Min
        {
            get { return min; }
            set { min = value; SetClientAttribute("min", min); RaisePropertyChangedNotification("Min"); }
        }

        public int Max
        {
            get { return max; }
            set { max = value; SetClientAttribute("max", max); RaisePropertyChangedNotification("Max"); }
        }
	
        public override string ClientClass
        {
            get
            {
                return "SliderWidget";
            }
        }

        private Orientation orientation = Orientation.Horizontal;
        public Orientation Orientation
        {
            get { return orientation; }
            set { orientation = value; SetClientAttribute("orientation", Util.ToJavaScriptString(orientation.ToString())); }
        }

        public override void HandleEvents(string evt, string args)
        {
            if (evt == "OnChanged")
            {
                currentValueOnClient = int.Parse(args);
                int oldVal = SelectedValue;
                SelectedValue = currentValueOnClient;
                InvokeChangedEvent(oldVal, val);
            }
            else
                base.HandleEvents(evt, args);
        }

        #region IDataBindable Members


        override public object Value
        {
            get
            {
                return SelectedValue;
            }
            set
            {
                SelectedValue = (int)PropertyConverter.Convert(value,typeof(int));
                RaisePropertyChangedNotification("Value");
            }
        }

        override public string DefaultProperty
        {
            get { return "SelectedValue"; }
        }

        #endregion
    }
}
