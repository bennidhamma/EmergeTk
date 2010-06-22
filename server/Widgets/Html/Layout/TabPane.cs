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
    public class TabPane : Pane
    {
    	public override string ClientClass { get { return "Pane"; } }
    	
        private Pane selectedTab;
		
		bool useClearFix = false;
		bool closeable;
		string labelClass;
		

        public Pane SelectedTab
        {
            get { return selectedTab; }
            set { 
            	if( selectedTab != value )
            	{
            		if( selectedTab != null && tabLiByPane.ContainsKey( selectedTab ) )
            		{
            			selectedTab.Visible = false;
            			HtmlElement oldLi = tabLiByPane[selectedTab];
            			if( oldLi != null )
            				oldLi.SetClientElementAttribute("id","''");
            		}            			
	                selectedTab = value;
	                HtmlElement newLi = tabLiByPane[selectedTab];
	                newLi.SetClientElementAttribute("id","current",true);
	                //InvokeClientMethod("SelectTab", selectedTab.ClientId);	                
	                SetupPanes();
               		selectedTab.Visible = true;
	            }
	            if (OnTabSelected != null) OnTabSelected(this, new TabPaneSelectedEventArgs( selectedTab ) );
	            RaisePropertyChangedNotification("SelectedTab");
            }
        }

        public List<LinkButton> Labels {
        	get {
        		return labels;
        	}
        }

        public Pane TabDisplayPane {
        	get {
        		return tabDisplayPane;
        	}
        }

        public bool UseClearFix {
        	get {
        		return useClearFix;
        	}
        	set {
        		useClearFix = value;
        	}
        }

        public bool Closeable {
        	get {
        		return closeable;
        	}
        	set {
        		closeable = value;
        	}
        }

        public string LabelClass {
        	get {
        		return labelClass;
        	}
        	set {
				log.Debug("setting label class", value );
				if( tabDisplayPane != null && ! string.IsNullOrEmpty( labelClass ) )
					tabDisplayPane.RemoveClass( labelClass );
        		labelClass = value;
				if( tabDisplayPane != null  && ! string.IsNullOrEmpty( value )  )
					tabDisplayPane.AppendClass( value );
        	}
        }

        public Dictionary<Pane,HtmlElement> tabLiByPane;
        public Dictionary<Widget,Pane> tabsByLabels;
		public List<LinkButton> labels;
		
		public LinkButton GetLinkButtonByLabel(string name)
		{
			foreach (LinkButton lb in Labels)
			{
				if ( lb == null ) log.Debug("lb is null");
				if (lb.Label == name ) return lb;				
			}
			return null;
		}				
		
        bool panesSetup = false;
        private void SetupPanes()
        {
        	if( ! panesSetup )
        	{
	        	tabLabelsPane = RootContext.CreateWidget<Pane>();
	        	tabDisplayPane = RootContext.CreateWidget<Pane>();
	        	tabList = RootContext.CreateWidget<HtmlElement>();
	        	tabList.TagName = "ul";
	        	tabLabelsPane.Id = "labels";
	        	tabDisplayPane.Id = "display";
				tabLabelsPane.ClassName = "TabLabels";
				log.Debug("setting up tab panes, labelclass: ", labelClass );
				if( labelClass != null )
					tabLabelsPane.AppendClass(labelClass);
	        	tabDisplayPane.ClassName = "TabContent";
				if ( UseClearFix ) tabDisplayPane.AppendClass("clearfix");
	        	tabLabelsPane.Add(tabList);
	        	panesSetup = true;
	        	base.Add( tabLabelsPane );
	        	base.Add( tabDisplayPane );
	        }
        }        	
        
        private Pane tabLabelsPane, tabDisplayPane;
        private HtmlElement tabList;
        public override void Initialize()
        {
        	SetupPanes();
        	if( selectedTab != null )
        		tabDisplayPane.Add( selectedTab );
			this.AppendClass("tab-pane");			
        }
        
        public event EventHandler<TabPaneSelectedEventArgs> OnTabSelected;
        /// <summary>
        /// Only allows a pane to be added.  Can't create a new signature, because then other 
        /// widget types could still be added.
        /// </summary>
        /// <param name="c">The paoverridene to add.</param>
        public override void Add(Widget c)
        {
        	if( IsCloning || deserializing )
        	{
        		base.Add(c);
        		return;
        	}
        	
        	if( c == null )
        	{
        		throw new ArgumentNullException();
        	}
        	
            if (!(c is Pane))
                throw new System.ArgumentException("Only panes can be children of a TabPane", "c");
            if( tabLiByPane == null ) tabLiByPane = new Dictionary<Pane, HtmlElement>();
            if( tabsByLabels == null ) tabsByLabels = new Dictionary<Widget,Pane>();
			if( labels == null ) labels = new List<LinkButton>();
            SetupPanes();
            Pane p = (Pane)c;			
            LinkButton lb = RootContext.CreateWidget<LinkButton>();
            lb.Bind("Label",p,"Label");
            lb.ClassName = "TabLabel";
			
			labels.Add(lb);
			
            HtmlElement liElem = RootContext.CreateWidget<HtmlElement>();
            liElem.TagName = "li";
            liElem.OnClick += tabLabel_OnClick;
            lb.OnClick += tabLabel_OnClick;
            liElem.Add( HtmlElement.Create("span") );

            tabsByLabels[liElem] = p;
			tabsByLabels[lb] = p;
            liElem.VisibleTo = c.VisibleTo;
            liElem.Bind("VisibleTo", c, "VisibleTo");
            liElem.Add(lb);
            liElem.AppendClass(p.ClassName);

			if( closeable )
			{
				EmergeTk.Widgets.Html.Image closeImage = RootContext.CreateWidget<EmergeTk.Widgets.Html.Image>(liElem);
				closeImage.Url = "Close.png";
				closeImage.OnClick += delegate{
					RemoveTab( p );			
				};
			}
			
			tabList.Add(liElem);
            tabLiByPane[p] = liElem;
            tabDisplayPane.Add(p);            
            if( SelectedTab == null )
            	SelectedTab = p;
            else
            	p.Visible = false;   
        }
        
        public void RemoveTab(Pane tab)
        {
        	if( !tabLiByPane.ContainsKey(tab) )
        		return;
        	tabLiByPane[tab].Remove();
			tabLiByPane.Remove(tab);
        	if( tab == this.selectedTab )
        	{
        		tab.Visible = false;
        		foreach( Pane t in tabsByLabels.Values )
        		{
        			//this is weird, i know, but it's easier than testing for non-null tab in tabsByLinkButton.values.
        			this.SelectedTab = t;
        			break;
        		}
        	}        	
        }

        public void tabLabel_OnClick(object sender, ClickEventArgs ea)
        {
			log.Debug("tabLabel_OnClick", ea.Source );
			if( tabsByLabels.ContainsKey( ea.Source ) )
				SelectedTab = tabsByLabels[ ea.Source ];        	
        }
    }
}
