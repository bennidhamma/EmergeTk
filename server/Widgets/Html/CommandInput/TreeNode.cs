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
    public class TreeNode : Widget
    {
        private string title;
        int childCount;
        public event EventHandler OnExpand;
        string type;

        public string Title
        {
            get { return title; }
            set { title = value; SetClientAttribute("l", Util.ToJavaScriptString(value));
                    RaisePropertyChangedNotification("Title");
        	}
        }

        public int ChildCount {
        	get {
        		return childCount;
        	}
        	set {
        		childCount = value;
        		SetClientAttribute("c",value);
        	}
        }

        public bool Expanded {
        	get {
        		return expanded;
        	}
        	set {
        		expanded = value;
        	}
        }

        public string Type {
        	get {
        		return type;
        	}
        	set {
        		type = value;
        		SetClientAttribute("t",Util.ToJavaScriptString(value));
        	}
        }
        
        bool expanded = false;

        public override string ToString()
        {
            return string.Format("Id:{0}, Title: {1}", UID, title);
        }
        
        public override void Add (Widget c)
        {
        	base.Add (c);        	
        }
        
        public override void Add (params Widget[] widgets)
        {
        	foreach( Widget w in widgets )
        		Add( w );
        }
        
        public override void HandleEvents (string evt, string args)
        {
        	if( evt == "OnExpand" )
        	{
        		if( OnExpand != null )
        			OnExpand( this, null );				
        	}
        	else
        		base.HandleEvents (evt, args);
        }
        
		public void SendReceiveNotification()
		{
			InvokeClientMethod("receiveChildren");
		}
		
		public void Select()
		{
			InvokeClientMethod("Select");
		}

		public void Expand()
		{
			if( this.Widgets != null && this.Widgets.Count > 0 )
			{
				InvokeClientMethod("Expand");
			}
		}
	}
}
