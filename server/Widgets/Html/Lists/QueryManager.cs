using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;


// TODO: Clean up this file to give classes their own files if we decided that is a good idea.
// TODO: refactor this into the Scaffold namespace (or whatever namespace we end up putting all the scaffold code into, i think it makes more sense in there)


namespace EmergeTk.Widgets.Html
{
	public class QueryManager<T> : ToggleBox where T : AbstractRecord, new()
	{
		public event GetFilterEditWidget<T> GetFilterEditWidgetHandler;
		
        private List<ColumnInfo> columns;
		private HtmlElement sortOL, unsortedUL, filterOL;
        private List<string> readOnlyFields;
        private List<FilterInfo> filters;
		private bool isSetup = false;
		
		public QueryManager()
		{
		}
		
		public override void Initialize ()
		{
            if( Open )
            {
            	Setup();
            }
            else
            {
            	OnToggle += new EventHandler(delegate(object sender, EventArgs ea ) {
            		Setup();
            	});
            }
		}
		
		private void Setup()
		{
			if( isSetup )
				return;
			isSetup = true;
			if( columns == null ) {
            	columns = new List<ColumnInfo>( ColumnInfoManager.RequestColumns<T>() );
			}
			SetupSorts( );
			SetupFilters( );
		}

        private IDataSourced dataWidget;

        public IDataSourced DataWidget
        {
            get { return this.dataWidget; }
            set
            {
            	log.Debug("setting datawidget", value);
                this.dataWidget = value;
                if (this.dataWidget != null)
                {
                    this.OriginalDataSource = this.dataWidget.DataSource;
                    if (this.dataWidget is Scaffold<T>)
                    {
                        Scaffold<T> scaffold = this.dataWidget as Scaffold<T>;
                        scaffold.OnAfterSave += new EventHandler<Scaffold<T>.ItemEventArgs>(delegate(object sender, Scaffold<T>.ItemEventArgs e) {
					        this.RefreshItems();
				        });
                    }
                }
                RaisePropertyChangedNotification("DataWidget");
            }
        }

        private IRecordList originalDataSource;
        public IRecordList OriginalDataSource
        {
            get {
            	if( originalDataSource == null && dataWidget != null )
            		originalDataSource = dataWidget.DataSource;
            	return this.originalDataSource; 
            }
            private set
            {
               	this.originalDataSource = value;
                if (this.originalDataSource != null)
                {
                    this.Filters = value.Filters;
                }
               	RaisePropertyChangedNotification("OriginalDataSource");
            }
        }

		public List<string> ReadOnlyFields {
			get {
				if( readOnlyFields == null )
					readOnlyFields = new List<string>();
				return readOnlyFields;
			}
		}
		
		public List<FilterInfo> Filters {
			get {
				if( this.originalDataSource != null )
				{
               		this.filters = originalDataSource.Filters;
               	}
				if( filters == null )
				{
					filters = new List<FilterInfo>();
				}
				return filters;
			}
			set 
			{
				filters = value;
			}
		}

		public List<ColumnInfo> Columns {
			get {
				return columns;
			}
		}
		
		public void SetupSorts()
		{
			sortOL = Find<HtmlElement>("sortedOL");
			unsortedUL = Find<HtmlElement>("unsortedUL");
			sortOL.OnReceiveDrop += new EventHandler<DragAndDropEventArgs>( SortReceiveSortDrop );
			unsortedUL.OnReceiveDrop += new EventHandler<DragAndDropEventArgs>( UnsortReceiveSortDrop );
			foreach( ColumnInfo ci in columns )
			{
				if( readOnlyFields != null && readOnlyFields.Contains( ci.Name ) )
				{
					continue;
				}
				if( ci.Type.GetInterface("IComparable") == null )
				{
					continue;
				}
				HtmlElement li = RootContext.CreateWidget<HtmlElement>();
				li.TagName = "li";
				li.DragSource = unsortedUL;
				Literal l = RootContext.CreateWidget<Literal>();
				l.Html = ci.Name;
				
				unsortedUL.Add( li );
				li.Add(l);
			}
		}
		
		public void SortReceiveSortDrop( object sender, DragAndDropEventArgs ea )
		{
			Widget w = ea.DroppedWidget;
			if( sortOL.Widgets != null && sortOL.Widgets.Contains( w ) )
				sortOL.Widgets.Remove( w );
			if( sortOL.Widgets == null )
				sortOL.InitializeWidgets();
			sortOL.Widgets.Insert( ea.DropPosition , w );
			
			if( unsortedUL.Widgets.Contains( w ) )
			{
				w.Parent = sortOL;
				unsortedUL.Widgets.Remove( w );
			}

            List<SortInfo> sorts = new List<SortInfo>();

			for( int i = 0; i < sortOL.Widgets.Count; i++ )
			{
				Widget w2 = sortOL.Widgets[i];
				Literal lit = w2.Find<Literal>();
                if (lit == null)
                    Debug.Trace("lit is null");
                else
                {
                    sorts.Add(new SortInfo(lit.Html));
                    Debug.Trace("lit: " + lit.Html);
                }
			}

            if (this.dataWidget != null && this.OriginalDataSource != null)
            {
                this.dataWidget.DataSource.Sort(sorts.ToArray());
                this.dataWidget.DataBind();
            }
		}
		
		public void UnsortReceiveSortDrop( object sender, DragAndDropEventArgs ea )
		{
			Widget w = ea.DroppedWidget;
			if( ! unsortedUL.Widgets.Contains( w ) )
			{
				sortOL.Widgets.Remove( w );
				unsortedUL.Widgets.Add( w );
				w.Parent = unsortedUL;
			}
		}

		public void SetupFilters()
		{			
            this.filterOL = this.Find<HtmlElement>("filterList");
            log.Debug("setting up filters OriginalDataSource ", OriginalDataSource);
            filterOL.ClearChildren();
            
            if( OriginalDataSource != null )
            {
            	this.Filters =  OriginalDataSource.Filters;
            	log.Debug("using filters", this.Filters );
            	
	            foreach( FilterInfo fi in this.Filters  )
	            {
	            	log.Debug("adding filter row for ", fi.ColumnName );
					if( readOnlyFields != null && readOnlyFields.Contains( fi.ColumnName ) )
						continue;
	            	FilterWidget<T> fw = AddFilterWidget();
	            	fw.FilterInfo = fi;
	            	fw.Init();	            	
				}
			}
			AddFilterWidget();
		}
		
        public FilterWidget<T> AddFilterWidget()
        {
            HtmlElement li = this.RootContext.CreateWidget<HtmlElement>();
            li.TagName = "li";

            FilterWidget<T> fw = this.RootContext.CreateWidget<FilterWidget<T>>();
			if( GetFilterEditWidgetHandler != null )
	        	fw.GetFilterEditWidgetHandler += GetFilterEditWidgetHandler;
            this.filterOL.Add(li);
            li.Add(fw);
            return fw;
        }
        
        internal void Changed()
        {
        	InvokeChangedEvent(null,null);
        }
        
        public override void ParseElement(System.Xml.XmlNode n)
		{
	        switch (n.LocalName)
	        {
	            case "Column":
	            	if( n.Attributes["Nonconfigurable"] != null && Convert.ToBoolean( n.Attributes["Nonconfigurable"].Value ) )
	            	{
	            		this.ReadOnlyFields.Add( n.Attributes["Name"].Value );
	            	}
	            	else
	            	{
		            	if( this.columns == null )
		            	{
		            		this.columns = new List<ColumnInfo>();
		            	}
		            	columns.Add( ColumnInfoManager.RequestColumn<T>( n.Attributes["Name"].Value ) );
		            }
	            	break;
	            default:
	            	base.ParseElement(n);
	            	break;
	        }            
		}
		
		/*
        private void OnRefreshClick(object sender, ClickEventArgs ea)
        {
            this.RefreshItems();
        }
		*/

        private void RefreshItems()
        {
            this.DataWidget.DataSource = DataProvider.LoadList<T>(this.Filters.ToArray(), this.DataWidget.DataSource.Sorts.ToArray());
            this.DataWidget.DataBind();
        }
	}
		

	public delegate Widget GetFilterEditWidget<T>( FilterWidget<T> fw ) where T : AbstractRecord, new();
 	
    public class FilterWidget<T> : Generic where T : AbstractRecord, new()
    {
    	public GetFilterEditWidget<T> GetFilterEditWidgetHandler;
    	
        private Dictionary<string, ColumnInfo> columns;

        private DropDown fieldsDD, operationsDD;
        
        private ColumnInfo currentColumn;

        private Widget currentEdit;

        private QueryManager<T> qm;

        private FilterInfo filterInfo;

        public FilterWidget()
        {
        }

        public override void Initialize()
        {
            this.currentEdit = this.Find<PlaceHolder>("filterValuePlaceHolder");
            this.currentEdit.SetClientElementStyle("display", "inline", true);

            this.columns = new Dictionary<string, ColumnInfo>();
            this.qm = this.FindAncestor<QueryManager<T>>();
            List<ColumnInfo> columnInfos = this.qm.Columns; 			
            string fields = "--Select Field--";
            foreach (ColumnInfo ci in columnInfos)
            {
                if (ci.Type.GetInterface("IRecordList") == null && ci.Type.GetInterface("IComparable") != null ||
                	(qm.ReadOnlyFields == null || ! qm.ReadOnlyFields.Contains( ci.Name)  ) )
                {
                    if (fields != String.Empty) fields += ",";
                    fields += ci.Name;

                    this.columns.Add(ci.Name, ci);
                }
            }

            this.fieldsDD = this.Find<DropDown>("fieldDropDown");
            this.fieldsDD.OptionsAsString = fields;

            this.operationsDD = this.Find<DropDown>("filterOpDropDown");
            List<string> ops = new List<string>(Enum.GetNames(typeof(FilterOperation)));
            this.operationsDD.Options = new List<string>(ops);
            this.operationsDD.SelectedOption = "Equals";
            if( filterInfo != null ) syncFilterInfo();
        }

        public FilterInfo FilterInfo {
        	get {
        		return filterInfo;
        	}
        	set {
        		filterInfo = value;
        		if( this.Initialized ) syncFilterInfo();
        	}
        }

        public Widget CurrentEdit {
        	get {
        		return currentEdit;
        	}
        }

        public ColumnInfo CurrentColumn {
        	get {
        		return currentColumn;
        	}
        }
        
        private void syncFilterInfo()
        {
        	this.fieldsDD.SelectedOption = filterInfo.ColumnName;
    		this.operationsDD.SelectedOption = filterInfo.Operation.ToString();
    		if( filterInfo.Value != null )
    			this.Find<LinkButton>("removeFilterLink").Visible = true;
    		SelectField(filterInfo.ColumnName, filterInfo.Value);
    		
        }

		public void FieldSelectedHandler(Widget source, object newValue)
		{
			string field = ((DropDown)source).SelectedOption;
			currentColumn = ColumnInfoManager.RequestColumn<T>(field);
            SelectField( field, null );
		}
		
        public void SelectField(string field, object Value)
        {
        	if( field == "--Select Field--" )
        	{
        		qm.Filters.Remove( this.filterInfo );
        		qm.DataWidget.DataSource = DataProvider.LoadList<T>( qm.Filters.ToArray(),
					qm.DataWidget.DataSource.Sorts.ToArray() );
        		qm.DataWidget.DataBind();
      			qm.Changed();
        		return;
        	}
        	if( ! this.columns.ContainsKey( field ) ) return;
            ColumnInfo ci = currentColumn = this.columns[field];
            Widget w = null;
            if( GetFilterEditWidgetHandler != null )
            	w = GetFilterEditWidgetHandler( this );
            if( w == null )
            	w = DataTypeFieldBuilder.GetEditWidget(ci, FieldLayout.Terse);
            if( Value != null )
            	w.Value = Value;
            w.BindProperty( w.DefaultProperty, FilterValueSetHandler );

            this.currentEdit.Replace(w);
            this.currentEdit = w;
        }

        public void FilterOpSelectedHandler(Widget source, object newValue)
        {
            if (this.filterInfo != null)
            {
                this.filterInfo.Operation = (FilterOperation)newValue;
                this.qm.DataWidget.DataSource = DataProvider.LoadList<T>( qm.Filters.ToArray(),
					qm.DataWidget.DataSource.Sorts.ToArray() );
                this.qm.DataWidget.DataBind();
                qm.Changed();
            }
        }

        public void FilterValueSetHandler()
        {
            if (this.filterInfo != null)
            {
                qm.Filters.Remove(this.filterInfo);
            }
            else
            {
                this.Find<LinkButton>("removeFilterLink").Visible = true;
                this.qm.AddFilterWidget();
            }

			this.filterInfo = new FilterInfo(this.fieldsDD.SelectedId, currentEdit[currentEdit.DefaultProperty], (FilterOperation)this.operationsDD.SelectedIndex);

			qm.Filters.Add( filterInfo );
            this.qm.DataWidget.DataSource = DataProvider.LoadList<T>( qm.Filters.ToArray(),
				qm.DataWidget.DataSource.Sorts.ToArray() );
            this.qm.DataWidget.DataBind();
            qm.Changed();
        }

        public void RemoveFilterHandler(Widget source, string msg)
        {
            qm.Filters.Remove(this.filterInfo);
			this.qm.DataWidget.DataSource = DataProvider.LoadList<T>( qm.Filters.ToArray(),
				qm.DataWidget.DataSource.Sorts.ToArray() );
            //this.qm.DataWidget.DataSource = this.qm.OriginalDataSource.Filter();
            this.qm.DataWidget.DataBind();
			qm.Changed();
            HtmlElement li = this.FindAncestor<HtmlElement>();
            li.RemoveChild(this);
            li.Remove();
        }
    }
}
