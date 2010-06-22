/**
 * Project: emergetk: stateful web framework for the masses
 * File name: DropDown.cs
 * Description: A drop down widget.
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
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	/// <summary>
	/// Summary description for DropDown.
	/// </summary>
	public class DropDown : Widget, IDataBindable
	{
        private List<string> ids;
        public List<string> Ids {
            get { return ids; }
            set { 
            	ids = value;
            	SetClientProperty( "ids", JSON.Default.Encode(value) );
            	RaisePropertyChangedNotification("Ids");
            }
        }

        public void UpdateClient()
        {
        	InvokeClientMethod("Update");
        }
        
        public string SelectedId {
            get { return ids[selectedIndex]; }
            set {
                SelectedIndex = ids.IndexOf(value);
                RaisePropertyChangedNotification("SelectedId");                
            }
        }
        
        private List<string> options;
        public List<string> Options {
			get { return this.options; }
			set	{
				options = value;
                if (Ids == null) Ids= options;
                SetClientProperty( "options", JSON.Default.Encode(value) );
                if (rendered)
                {
                    UpdateClient();
                }
                RaisePropertyChangedNotification("Options");
			}
		}
		
		public void SetOptions( Type enumType, bool humanize )
		{
			List<string> options = new List<string>(Enum.GetNames(enumType));
        
            for( int i = 0; i < options.Count; i++ )
            	options[i] = Util.PascalToHuman( options[i] );
            Options = options;
            DefaultProperty = "SelectedIndex";
		}

        public string OptionsAsString
        {
            get { return Util.Join(options) ; }
            set { Options = new List<string>(value.Split(','));
            	RaisePropertyChangedNotification("OptionsAsString");
            }
        }

        private bool isComboBox;

        public bool IsComboBox
        {
            get { return isComboBox; }
            set { isComboBox = value; SetClientAttribute("isComboBox", Convert.ToInt16(value)); 
            	RaisePropertyChangedNotification("IsComboBox");
            }
        }	
        
        public string SelectedOption {
            get { return options[selectedIndex]; }
            set { SelectedIndex = options.IndexOf(value); RaisePropertyChangedNotification("SelectedOption"); 
           	}
        }
		
        private int selectedIndex;
		public int SelectedIndex {
			get { return this.selectedIndex; }
			set {
				this.selectedIndex = value;
				if( ! rendered )
					SetClientElementAttribute("selectedIndex",selectedIndex.ToString());
				else
					InvokeClientMethod( "SetSelectedIndex", selectedIndex.ToString() );
                RaisePropertyChangedNotification("SelectedIndex");
			}
		}
		
        public override void HandleEvents(string evt, string args)
		{
			if( evt == "OnChanged" )
			{
				object old = Value;
                Value = args;
                InvokeChangedEvent(old, selectedIndex );
			}
            else
            {
                base.HandleEvents(evt, args);
            }
		}

        public void AddOption(string value, string id)
        {
            if (options == null) options = new List<string>();
            if (Ids == null) Ids = new List<string>();
            Options.Add(value);
            if (id != null)
                Ids.Add(id);
            else
                id = (options.Count - 1).ToString();
            InvokeClientMethod("AddOption", string.Format("'{0}','{1}'", value, id));
        }

        public void RecordAddedHandler(object sender, RecordEventArgs ea)
        {
            AddOption(ea.Record.ToString(), ea.Record.Id.ToString());
        }

		public override void ParseElement (System.Xml.XmlNode n)
		{
			if( n.LocalName == "Option" )
			{
				string id = n.Attributes["Id"] != null ? n.Attributes["Id"].Value : n.InnerText;
				bool selected = n["Selected"] != null && n["Selected"].Value == "True" ;
				AddOption( n.InnerText, id);
				if( selected )
				{
					SelectedId = id;
				}
			}
			else
				base.ParseElement (n);
		}
        
        #region IDataBindable Members

        private string defaultProperty = "SelectedId";
        override public string DefaultProperty
        {
            get { return defaultProperty; }
            set { defaultProperty = value; 
            	RaisePropertyChangedNotification("DefaultProperty");
            }
        }

        override public object Value
        {
            get
            {
                return this.SelectedOption;
            }
            set
            {
                int index = -1;
                if (value == null) return;
                if (ids != null && ids.Contains(value.ToString()))
                    SelectedId = value.ToString();
                else if(int.TryParse(value.ToString(),out index) )
                    SelectedIndex = index;
                else if( options.Contains( value.ToString() ) )
                    SelectedOption = value.ToString();
                RaisePropertyChangedNotification("Value");
            }
        }

        public bool IsPassThrough
        {
            get { return false; }
        }

        private Binding binding;
        public Binding Binding
        {
            get { return binding; }
            set { binding = value; }
        }


        #endregion
    }
}
