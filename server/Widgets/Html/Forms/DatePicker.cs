/**
 * Project: emergetk: stateful web framework for the masses
 * File name: DataPicker.cs
 * Description: A C# wrapper for the dojo datepicker.
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
    public class DatePicker : Widget
    {
    	DateTime date;
		bool isCalendar = false;
    	
    	public virtual System.DateTime Date {
    		get {
    			return date;
    		}
    		set {
				//System.Console.WriteLine("changing date from {0} to {1}", date, value);
    			date = value;

				if( ! rendered )
					SetClientAttribute("date", string.Format("new Date('{0}')",date.ToShortDateString()));
				else
					InvokeClientMethod("ChangeDate", Util.ToJavaScriptString(date.ToShortDateString()));
				RaisePropertyChangedNotification("Date");
    		}
    	}

		public bool IsCalendar
		{
			get
			{
				return isCalendar;
			}
			set
			{
				isCalendar = value;
				SetClientAttribute("isCalendar",value ? 1 : 0 );
			}
		}
    	
        public override void HandleEvents(string evt, string args)
        {
            if (evt == "SetDate")
            {
            	DateTime oldDate = Date;
				Date = DateTime.Parse(args);
                InvokeChangedEvent(oldDate, Date);
            }
            else
            {
                base.HandleEvents(evt, args);
            }
        }
		
		override public object Value
        {
            get
            {
                return this.Date;
            }
            set
            {
                this.Date = Convert.ToDateTime(value);
            }
        }

        override public string DefaultProperty
        {
            get { return "Date"; }
        }
    }
}
