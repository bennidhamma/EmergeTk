using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
    public class Repeater<T> : Generic, IDataSourced, IPageable, IGroupable where T : AbstractRecord, new()
	{
        public event EventHandler<RowEventArgs<T>> OnRowAdded;
        public event EventHandler<RowEventArgs<T>> OnRowClicked;

        private bool delegatesSubscribed = false;
        private List<EventHandler<RecordEventArgs>> onAddedHandlers = new List<EventHandler<RecordEventArgs>>();
        private List<EventHandler<RecordEventArgs>> onRemovedHandlers = new List<EventHandler<RecordEventArgs>>();
        private Pager pager;
		private int columnCount = 1;

		//TODO: seems like we may be double adding templates here. investigate.
        private Template template;
        public Template Template
        {
            get
            {
                if( template == null )
			    {
				    template = RootContext.CreateWidget<Template>();
				    template.BindsTo = typeof(T);
				    template.Parent = this;
				    template.RootContext = this.RootContext;
			    }
                return template;
            }
            set
            {
                template = value;
                template.Parent = this;
                template.RootContext = this.RootContext;
                template.BindsTo = typeof(T);
                RaisePropertyChangedNotification("Template");
            }
        }

        private IRecordList<T> dataSource;
        public IRecordList<T> DataSource
        {
            get { return dataSource; }
            set
            {
                if (delegatesSubscribed && dataSource != null)
                {
                    foreach (EventHandler<RecordEventArgs> r in onAddedHandlers)
                        dataSource.OnRecordAdded -= r;
                    foreach (EventHandler<RecordEventArgs> r in onRemovedHandlers)
                        dataSource.OnRecordRemoved -= r;
                    onAddedHandlers.Clear();
                    onRemovedHandlers.Clear();
                }
                dataSource = value;
                delegatesSubscribed = false;
                RaisePropertyChangedNotification("DataSource");
            }
        }

        private List<Template> items;
        public List<Template> Items
        {
            get { return items; }
        }

        public string ItemClassName
        {
            get { return Template.ClassName; }
            set
            {
                Template.ClassName = value;
                RaisePropertyChangedNotification("ItemClassName");
            }
        }

		string viewTemplate;
		public string ViewTemplate
		{
			get
			{
				return viewTemplate;
			}
			set
			{
				viewTemplate = value;
			}
		}

		public int ColumnCount
		{
			get
			{
				return columnCount;
			}
			set
			{
				columnCount = value;
			}
		}

		public int Count
		{
			get
			{
				return EndIndex + 1;
			}
		}

        public int PageCount
        {
            get { return (EndIndex / PageSize) + 1; }
        }

        public int CurrentPage
        {
            get { return (startIndex / PageSize) + 1; }
            set
            {
                DataBind(value);
                RaisePropertyChangedNotification("CurrentPage");
            }
        }

        private int startIndex = 0;
        public int StartIndex
        {
            get { return startIndex; }
            set
            {
                startIndex = value;
                RaisePropertyChangedNotification("StartIndex");
            }
        }

        private int pageSize = 1000;
        public int PageSize
        {
            get { return pageSize; }
            set
            {
                pageSize = value;
                RaisePropertyChangedNotification("PageSize");
            }
        }

        private int endIndex = -1;
        public int EndIndex
        {
            get
            {
                if (dataSource != null && endIndex == -1)
                    return dataSource.Count - 1;
                else
                    return endIndex;
            }
            set
            {
                endIndex = value;
                RaisePropertyChangedNotification("EndIndex");
            }
        }

        private string templateTagName = "div";
        public virtual string TemplateTagName
        {
            get { return templateTagName; }
            set
            {
                templateTagName = value;
                Template.TagName = value;
                RaisePropertyChangedNotification("TemplateTagName");
            }
        }

        private bool usePager;
        public virtual bool UsePager
        {
            get { return usePager; }
            set
            {
                usePager = value;
                RaisePropertyChangedNotification("UsePager");
            }
        }

        private string propertySource;
        public virtual string PropertySource
        {
            get { return propertySource; }
            set
            {
                propertySource = value;
                RaisePropertyChangedNotification("PropertySource");
            }
        }

        bool dataBound = false;
        public bool IsDataBound
        {
            get { return dataBound; }
            set { dataBound = value; }
        }

        IRecordList IDataSourced.DataSource
        {
            get { return DataSource as IRecordList; }
            set
            {
                DataSource = value as IRecordList<T>;
                RaisePropertyChangedNotification("DataSource");
            }
        }

        private Generic header;
        public Generic Header
        {
            get { return header; }
            set { header = value; }
        }

        private Generic body;
        public Generic Body
        {
            get
            {
                if (body == null)
                {
                    body = RootContext.CreateWidget<Generic>();
                    body.TagName = bodyTagName ?? "div";
                    body.Id = "body";
                    body.AppendClass("body");
                }
                return body;
            }
        }

        private Generic footer;
        public Generic Footer
        {
            get { return footer; }
            set { footer = value; footer.AppendClass("footer");}
        }

		private Generic separator;
		public Generic Separator
		{
			get
			{
				return separator;
			}
			set
			{
				separator = value;
				separator.AppendClass("separator");
			}
		}
        
        private Generic emptyTemplate;
        public Generic EmptyTemplate
        {
            get { return emptyTemplate; }
            set { emptyTemplate = value; }
        }

        private bool ensureDataBound = false;
        public bool EnsureDataBound
        {
            get { return ensureDataBound; }
            set { ensureDataBound = value; }
        }

        private string bodyTagName = "div";
        public string BodyTagName
        {
            get { return bodyTagName; }
            set { bodyTagName = value; }
        }

        private string pagerTagName = "div";
        public string PagerTagName
        {
            get { return pagerTagName; }
            set { pagerTagName = value; }
        }

        public AbstractRecord Selected {
        	get {
        		throw new NotImplementedException();
        	}
        	set {
        		throw new NotImplementedException();
        	}
        }

        public List<RepeaterGroup> Groups {
        	get {
        		return groups;
        	}
        	set {
        		groups = value;
        	}
        }

        public int CurrentGroupLevel {
        	get {
        		return currentGroupLevel;
        	}
        }

        public AbstractRecord GroupDeterminant {
        	get {
        		return groupDeterminant;
        	}
        }

        public Repeater()
        {
            this.TagName = "div";
            //log.Debug( "ctor repeater of type " + typeof(T) )
        }

        public override void Initialize()
        {
        	if( Initialized )
        		return;
           	base.Initialize();
           	if( header != null )
           	{
           		base.Add( header );
           		header.Id = "header";
           		header.AppendClass("header");
           		if( header.TagName == "div" && templateTagName != "div" )
           			header.TagName = templateTagName;
           	}
			base.Add( Body );
			if( footer != null )
				base.Add( footer );
            if (this.EmptyTemplate != null)
            {
                this.BaseAdd(this.EmptyTemplate);
            }

	    }

        public override void ParseElement(System.Xml.XmlNode n)
        {
            switch (n.LocalName)
            {
                case "Item":
                case "ItemTemplate":
                    ParseItemTemplate(n);
                    break;
                case "Group":
                	ParseGroup(n);
                	break;
                default:
                    base.ParseElement(n);
                    break;
            }
        }
        private void ParseItemTemplate(System.Xml.XmlNode n)
        {
            Template.ParseAttributes(n);
            Template.ParseXml(n);
        }
        
        private void ParseGroup(System.Xml.XmlNode n)
        {
        	if( groups == null )
        		groups= new List<RepeaterGroup>();
        		
        	RepeaterGroup groupDef = new RepeaterGroup();
        	groupDef.Field= n.Attributes["Field"].Value;
        	
        	XmlNode headerNode = n.SelectSingleNode( "Header" );
        	XmlNode bodyNode = n.SelectSingleNode( "Body" );
        	XmlNode footerNode = n.SelectSingleNode( "Footer" );
        	
        	if( headerNode != null )
        	{
        		 groupDef.Header = RootContext.CreateWidget<Generic>();
        		 groupDef.Header.Parent = this;
        		 groupDef.Header.Parse( headerNode );
        	}      	
        	
        	if( bodyNode != null )
        	{
				groupDef.Body = RootContext.CreateWidget<Generic>( bodyNode);
        		groupDef.Body.Parent = this;
        		groupDef.Body.Parse( bodyNode );
        	}
        	else
        	{
				groupDef.Body = Body;
        	}
        	
        	if( footerNode != null )
        	{
        		groupDef.Footer = RootContext.CreateWidget<Generic>( footerNode );        		 
        		groupDef.Footer.Parent = this;
        		groupDef.Footer.Parse( footerNode );
        	}
        	
        	groups.Add( groupDef );
        	
        	if( groupDef.Header == null )
        	{
        		throw new Exception("supply header please (just for debugging purposes.)");
        	}
        }
        
		
        public override bool Render(Surface surface)
        {
            if (ensureDataBound && !dataBound)
                DataBind();
            return base.Render(surface);
        }

	    public override Dictionary<string,object> Serialize(Dictionary<string,object> h)
	    {
	    	base.Serialize(h);
	    	if( ! h.ContainsKey( "Template" ) && Template != null )
	    		h.Add("Template", template.Serialize() );
	    	return h;
	    }
	    
		public override void Swizzle()
		{
	    	base.Swizzle ();
			header = Find<Generic>("header");
	    	body = Find<Generic>("body");
	    	footer = Find<Generic>("footer");
            this.emptyTemplate = this.Find<Generic>("emptyTemplate");
		}

		public override void Add(Widget c)
		{
            if (IsCloning || deserializing )
            {
                base.Add(c);
            }
            else
            {
	            Template.Add(c);
	        }
		}

		T lastRow;
		Generic currentBody;		
        public void AddItem(T row, int index)
        {
        	//#if DEBUG
        	//	log.Debug( "Adding item ", lastRow, row ); 
        	//#endif
            int count = 0;
            HandleGrouping( lastRow, row );
            lastRow = row;
          //  log.Debug("currentBody: ", currentBody );
            if (currentBody.Widgets != null)
                count = currentBody.Widgets.Count;
			//log.Debug("template: ", template );
			
            //if (template.RootContext == null)
            //   template.RootContext = this.RootContext;
            Template t = PrepareNewItem(row, index);
            //why are we using rowindex?
            int rowIndex = index; // dataSource.IndexOf( row );
			if( Separator != null )
				rowIndex += rowIndex -1;
			if( columnCount > 1 )
			{
				currentBody = columns[index*columnCount/Count] as Generic;
			}
            if (rowIndex < count - 1)
            {
                currentBody.Insert(t, rowIndex);
            }
            else
            {
                currentBody.Add(t);
            }
            if (OnRowAdded != null)
                OnRowAdded(this, new RowEventArgs<T>(t, row));
            //log.Debug( "Finish AddItem" ); 
        }
        
       // Stack<RepeaterGroup> groupStack;
        List<RepeaterGroup> groups;
        int groupDepth = 0;

		public Template GetTemplateForRecord(AbstractRecord r)
		{
			foreach( Template t in FindAll<Template>() )
			{
				if( t.Record == r )
					return t;
			}
			return null;
		}
		
        /*
        private void debug( string s )
        {
    		Label l = RootContext.CreateWidget<Label>();
    		l.Text = s;
    		currentBody.Add( l );
        }
        */
		
        int currentGroupLevel = -1;
        AbstractRecord groupDeterminant;
        
        private void HandleGrouping(T oldRow, T newRow )
        {
        	//log.Debug( "Starting HandleGrouping" ); 
        	
        	if( groups != null )
        	{
        		groupDeterminant = oldRow;
        		bool changed = oldRow == null;
        		
        		currentGroupLevel = changed ? 0 : -1;
        		
        		if( oldRow != null )
        		{
        			//test to see if any groups have changed
        			for( int i = 0; i < groups.Count; i++ )
        			{
        				RepeaterGroup g = groups[i];
        				IComparable a = (IComparable)oldRow[ g.Field ];
	        			IComparable b = (IComparable)newRow[ g.Field ];	
	        			
	        			if( a.CompareTo( b ) != 0 )
	        			{
	        				//debug( "group has changed " + g.Field );
	        				changed = true;
	        				currentGroupLevel = i;
	        				break;
	        			}
        			}
        			
        			if( changed )
        			{
						
        				//need to print footers, dedent body
        				int tempLevel = currentGroupLevel;
        				for( int i = groups.Count - 1; i >= tempLevel; i-- )
        				{
        					currentGroupLevel = i;
        					if( groupDepth == 0 )
        						throw new Exception("groupDepth is 0!");
        					RepeaterGroup g = groups[i];
        					//log.Debug( "printing footer, dedenting ", i, g.Field, groupDepth ); 
        				
        					if( g.Footer != null )
        						PrepareGroup( currentBody, g.Footer, oldRow );
        					currentBody = (Generic)currentBody.Parent;
        					groupDepth--;
				        }
				        currentGroupLevel = tempLevel;
        			}        		        		        		        		
        		}        		
        		
				//now print out headers for new groups
				
				if( changed )
				{
					groupDeterminant = newRow;
					int tempLevel = currentGroupLevel;
					for (int i = tempLevel; i < groups.Count ; i++ ) 
					{
						currentGroupLevel = i;
						RepeaterGroup g = groups[i];
						//log.Debug( "What is header?", g.Header ); 
						if( g.Header != null )
	        			{
	        			//	log.Debug( "preparing new header", g.Field ); 
	        				PrepareGroup( currentBody, g.Header, newRow );
	        				
	        				Generic newBody =  g.Body.Clone() as Generic;
		        			newBody.RootContext = this.RootContext;	        			
		        				//PrepareGroup( currentGroup.Body, newRow );
		        			//indent
		        		//	log.Debug( "indenting ", i, g.Field, groupDepth ); 
		        			currentBody.Add( newBody );
		        			currentBody = newBody;		        			
	        			}
	        			groupDepth++;
					}					
				}				
        	}
        	currentGroupLevel = -1;
        	//log.Debug( "Finish HandleGrouping" ); 
        }
        
        
        public Generic PrepareGroup(Widget parent, Generic source, T row )
        {
            Generic t = source.Clone() as Generic;
            t.RootContext = this.RootContext;
            parent.Add( t );
            if (t.Initialized == false)
                t.Init();
            t.DataBindWidget(row);
            t.PostDataBind();
            
            return t;
        }

		public void InitializeViewTemplate()
		{
			this.template = initViewTemplate();
		}

		private Template initViewTemplate()
		{
			if( ViewTemplate != null )
			{
				ObjectViewer ov = RootContext.CreateWidget<ObjectViewer>
						(body,null,null,true,false);
			 	ov.BindsTo = typeof(T);
				ov.Source = new T();
	            ov.AutoDataBind = false;
				ov.Template = viewTemplate;
				return ov;
			}
			return null;
		}

        public Template PrepareNewItem(T row, int index)
        {
        	Template t = null;
        	if( this.template != null )
        	{
        		t = template.Clone() as Template;
            	t.RootContext = this.RootContext;
            	t.Row = index;
			}
			else if( ViewTemplate != null )
			{
				t = initViewTemplate();
			}
			
            t.Parent = this.body;
            t.Id = index.ToString();
           
            if (t.Initialized == false)
                t.Init();
            t.UnDataBindWidget();
            t.DataBindWidget(row);
            t.PostDataBind();
            items.Add(t);
            if (OnRowClicked != null)
                t.OnClick += new EventHandler<ClickEventArgs>(rowOnClick);

			t.AppendClass("item");
            t.AppendClass(index % 2 == 1 ? "odd" : "even");
			if( index == 0 )
				t.AppendClass( "first" );
			else if( index == PageSize - 1 || index == Count -1 )
			{
				t.AppendClass( "last" );
			}
            return t;
        }

		public void BaseAdd(Widget c)
		{
			base.Add(c);
		}
		
        public void DataBind()
        {
            this.DataBind(1);
        }

		List<Generic> columns;
		
		public bool SaveChangesToList { get; set; }

		public void DataBind(int pageNumber)
		{
			if( ! Initialized )
				Init();
			//if( template == null )
			//	template = RootContext.CreateWidget<Template>();
			EventHandler<RecordEventArgs> r;
            this.startIndex = (pageNumber - 1) * pageSize;
			if( Body.Widgets != null )
			{
				//TODO:support wipeout'
				Body.ClearChildren();
				//tweakdisplay is for a stupid ff bug in rendering dynamic tables.
				if( templateTagName == "tr" )
					this.InvokeClientMethod("TweakDisplay");
				
			}

			if( dataSource == null )
			{
				if( propertySource != null && this.Record != null && this.Record[propertySource] is IRecordList<T> )
				{
					dataSource = this.Record[propertySource] as IRecordList<T>;
					delegatesSubscribed = false;
					if( SaveChangesToList )
					{
						r = new EventHandler<RecordEventArgs>( delegate( object sender, RecordEventArgs ea )
	            			{
	            				this.Record.SaveRelations(propertySource);
	            			});
	            			
						dataSource.OnRecordAdded += r;
						onAddedHandlers.Add( r );
						
	            		dataSource.OnRecordRemoved += r;
	            		onRemovedHandlers.Add( r );
	            	}
				}
				else
				{
//					log.Error("DataSource is null and could not bind to property source", this, this.Record, propertySource,
//						typeof(T) );
					return;
				}
			}

            if (this.EmptyTemplate != null)
            {
				log.Debug("EmptyTemplate not null.", this.DataSource);
			
                if (this.DataSource == null || this.DataSource.Count == 0)
                {
					log.Debug("datasource has no items");
                    this.EmptyTemplate.Visible = true;
					if( this.header != null )
						this.Header.Visible = false;
                    if( this.footer != null )
                    	this.footer.Visible = false;
                    return;
                }
                else
                {
                    if( this.footer != null )
                    	this.footer.Visible = true;
                    if( this.header != null )
						this.Header.Visible = true;
                    this.EmptyTemplate.Visible = false;
                }
            }

		    items = new List<Template>();
		    currentBody = body;
		    lastRow = null;
			if( columnCount > 1 )
			{
				columns = new List<Generic>();
				for( int i =0; i < columnCount; i++ )
				{
					columns.Add( this.RootContext.CreateWidget<Generic>(currentBody) );
					columns[i].AppendClass("column column-" + i );
				}
			}
            
			for( int i = startIndex; i <= EndIndex && i < startIndex + pageSize; i++ )
			{
				if( i > startIndex && separator != null )
				{
					currentBody.Add( Separator.Clone() as Generic );
				}
				AddItem( dataSource[i], i );
				
			}
			handlePager();
			if( ! delegatesSubscribed )
			{
				r = new EventHandler<RecordEventArgs>(DataSource_OnRecordAdded);
			    DataSource.OnRecordAdded += r;
			    onAddedHandlers.Add( r );
			    r = new EventHandler<RecordEventArgs>(DataSource_OnRecordDeleted);
	            DataSource.OnRecordRemoved += r;
	            onRemovedHandlers.Add( r );
	            delegatesSubscribed = true;
	        }
	        if( header != null )
	        	header.DataBindWidget();
			if( footer != null )
				footer.DataBindWidget();
            dataBound = true;
        }
		
		private void handlePager()
		{
			if( ! usePager )
				return;
			List<Pager> pagers = FindAll<Pager>();
			
			if( pagers == null || pagers.Count == 0 )
			{
				if( footer == null )
				{
					footer = RootContext.CreateWidget<Generic>();
					base.Add(footer);
				}
				pager = RootContext.CreateWidget<Pager>(footer);
				pager.PageSet = this;
				pager.TagName = pagerTagName;
				pager.Init();
				//log.Debug( "what did build?", pager, PageCount, usePager , footer); 
			}
			else
			{
				foreach( Pager pager in pagers )
				{
					pager.PageSet = this;
					pager.Refresh();
				}
			}
		}

        private void DataSource_OnRecordDeleted(object sender, RecordEventArgs ea)
        {
            foreach (Template t in items)
            {
                if (t.Record == ea.Record)
                {
                    t.Remove();
                    items.Remove(t);
                    return;
                }
            }
        }

        private void DataSource_OnRecordAdded(object sender, RecordEventArgs ea)
        {
			AddItem((T)ea.Record, DataSource.IndexOf( ea.Record ) );
        }

        private void rowOnClick(object sender, ClickEventArgs ea )
        {
            OnRowClicked(this, new RowEventArgs<T>(sender as Template, ea.Source.Record as T));
        }
    }

	public class RowEventArgs<T> : EventArgs where T : AbstractRecord 
    {
        private Template template;

        //add a more useful name.
        public Template Template
        {
            get { return template; }
            set { template = value; }
        }

        public RowEventArgs(Template t, T r)
        {
            template = t;
            record = r;
        }
        
        private T record;
        public T Record
        {
        	get { return record; }
        	set { record = value; }
        }
    }

    public class TableRepeater<T> : Repeater<T> where T : AbstractRecord, new()
    {
        public TableRepeater()
        {
            this.TagName = "table";
            this.Template.TagName = "tr";
        }
    }
}
