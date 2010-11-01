using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using EmergeTk.Model;
using EmergeTk.Model.Security;

namespace EmergeTk.Widgets.Html
{
	public enum NewButtonPosition {
		Top,
		Bottom,
		Both
	}
	
    public class Scaffold<T> : Widget, IDataSourced
        where T : AbstractRecord, new()
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Scaffold<T>));
        private Widget rootScaffold;
		private Repeater<T> grid;
		private Template customGridTemplate;
		private Hashtable customGridTemplates;
		private ModelForm<T> addForm;
		private IButton showAddButton;
		private Widget showAddButtonWidget;
		private IRecordList<T> dataSource;
		private Model.ColumnInfo[] fieldInfos;
		private List<IQueryInfo> query;
		private HtmlElement addPane;
		private List<FilterInfo> defaultValues;
		bool ignoreDefaultEditTemplate = false;
		bool saveAsYouGo = false;
		bool addExpanded = false;
		bool useEditForView = false;
		bool showDeleteButton = false;
		bool useStack = false;

        bool applyFiltersToNew = false;

		string propertySource;
		string tagName;
		string templateTagName;
		bool usePager;
		int pageSize;
		bool destructivelyEdit = false;

		Permission addPermission, editPermission, deletePermission,
			addVisibleTo, editVisibleTo, deleteVisibleTo;
		
		public event EventHandler<ItemEventArgs> OnBeforeSave;
		public event EventHandler<ItemEventArgs> OnAfterSave;
//		public event EventHandler OnDataSourceChanged;
        
        static Type defaultButtonType;
    	
    	static Scaffold()
    	{
    		defaultButtonType = TypeLoader.GetType(Setting.GetValueT<string>("DefaultButtonType","EmergeTk.Widgets.Html.Button"));
    	}

		private string model;
		public string Model
		{
			get
			{
				return this.model;
			}
			set
			{
				model = value;
			}
		}

		private string friendlyModelName;
		public string FriendlyModelName
		{
			get
			{
				if( friendlyModelName == null )
				{
                    friendlyModelName = model ?? typeof(T).Name;
					if( friendlyModelName.LastIndexOf(".") > -1 )
					{
						friendlyModelName = friendlyModelName.Substring(friendlyModelName.LastIndexOf(".")+1);
					}
				}
				return this.friendlyModelName;
			}
			set
			{
				this.friendlyModelName = value;
			}
		}

		public IRecordList<T> DataSource
		{
			get { return dataSource; }
			set { 
				dataSource = value;
				if( grid != null )
					grid.DataSource = value;
//				if( OnDataSourceChanged != null )
//					OnDataSourceChanged( this, new EventArgs() );
			}
		}

		private Type buttonType = defaultButtonType;
		private string button = defaultButtonType.FullName;
		
		public string Button
		{
			get
			{
				return this.button;
			}
			set
			{
				this.button = value;
				this.buttonType = DiscoverType(button);
			}
		}

		private bool hideListOnAdd = false;
		public bool HideListOnAdd {
			get { return hideListOnAdd; }
			set { hideListOnAdd = value; }
		}

        private bool showNewButton = true;
        public bool ShowNewButton
        {
            get { return showNewButton; }
            set { showNewButton = value; }
        }
		
		public string EditTemplate { get; set; }
        
        private NewButtonPosition newButtonPosition = NewButtonPosition.Bottom;
        public NewButtonPosition NewButtonLocation
        {
            get { return newButtonPosition; }
            set { newButtonPosition = value; }
        }
        
        private string newButtonLabel;
        public string NewButtonLabel
        {
            get { return newButtonLabel; }
            set { newButtonLabel = value; }
        }

		private string order = "ROWID";
		public string Order
		{
			get
			{
				return this.order;
			}
			set
			{
				this.order = value;
			}
		}

        private bool ensureCurrentRecordOnEdit = true;
        public bool EnsureCurrentRecordOnEdit
        {
            get { return ensureCurrentRecordOnEdit; }
            set { ensureCurrentRecordOnEdit = value; }
        }

		public Scaffold()
		{
			this.ClientClass = "PlaceHolder";
			rootScaffold = this;
		}
		
		Pane header,body,footer;
		Widget host;
		Stack stack = null; // TODO: this is never set
		
		public override void Initialize()
		{
			
			host = this;
			
			header = RootContext.CreateWidget<Pane>(host);
			body = RootContext.CreateWidget<Pane>(host);
			footer = RootContext.CreateWidget<Pane>(host);
			
			fieldInfos = ColumnInfoManager.RequestColumns<T>();
			
			SetupGrid();
			SetupAddButtons();
		}
		
		private void SetupGrid()
		{
			Template gridPane = null;
			if( ! useEditForView )
                gridPane = this.CustomGridTemplate != null ? this.CustomGridTemplate : buildDefaultGridPane();
			else
			{
				gridPane = RootContext.CreateWidget<Template>(host);				
			}
			RemoveChild(gridPane);
			gridPane.Id = "gridPane";
			insertEditAndDeleteButtons( gridPane );
            grid = Grid;
            grid.PropertySource = propertySource;
            grid.Template = gridPane;
            grid.Footer = RootContext.CreateWidget<Generic>();
            grid.TagName = tagName ?? "div";
            grid.Template.AppendClass("item");
            grid.TagName = tagName ?? "div";
            grid.TemplateTagName = templateTagName ?? "div";
            
			grid.Id = "grid";
			grid.UsePager = usePager;
			if( useEditForView )
			{
				grid.OnRowAdded += new EventHandler<RowEventArgs<T>>( delegate( object sender, RowEventArgs<T> ea ) {
					associateEditForm( ea.Template );
				});
			}
			if( pageSize != 0 ) grid.PageSize = pageSize;
			setDataSource();
			if( dataSource != null )
			{
				grid.DataSource = dataSource;
			}
			host.AppendClass( "scaffold " + Name.Replace(".","-") );
		}

		private void SetupAddButtons()
		{
			if( ShowNewButton || addExpanded)
			{
				
				addPane = RootContext.CreateWidget<HtmlElement>();
				addPane.VisibleToPermission = AddVisibleTo;
				addPane.Id = "addPane";
				addPane.AppendClass( "addPane" );
				
				if( addExpanded )
					ShowAddForm();
				else
				{				
					showAddButton = RootContext.CreateUnkownWidget(buttonType) as IButton;
		            showAddButtonWidget = (Widget)showAddButton;
		            showAddButtonWidget.AppendClass( "addButton" );
		            showAddButtonWidget.Id = "AddButton";
					showAddButton.Label = newButtonLabel ?? "Add New " + FriendlyModelName;
					showAddButtonWidget.OnClick += new EventHandler<ClickEventArgs>(add_OnClick);
					addPane.Add(showAddButtonWidget);
				}
				
				if( newButtonPosition == NewButtonPosition.Top ||
					newButtonPosition == NewButtonPosition.Both ) 
				{
					header.Add( addPane );
				}
				body.Add( grid );
				if( newButtonPosition == NewButtonPosition.Both )
				{
					footer.Add( addPane.Clone() as Widget );								
				}
				else if( newButtonPosition == NewButtonPosition.Bottom )
				{
					footer.Add( addPane );
				}
			}
			else
				body.Add( grid );
		}

		private Template buildDefaultGridPane()
		{
			Template gridPane = RootContext.CreateWidget<Template>(host, this.Record);
			gridPane.ClassName = "simplePane";
			foreach( Model.ColumnInfo fi in fieldInfos )
			{
				Widget c = null;
                if (fi.DataType == DataType.RecordList)
				{
					c = new PlaceHolder();
				}
				else
				{
					c = RootContext.CreateWidget<Label>();
                    c.SetAttribute("Text", "<strong>" + fi.Name + ":</strong>&nbsp;&nbsp;{" + fi.Name + "}");
				}
				c.Id = fi.Name;
				gridPane.Add( c );
			}
			PlaceHolder editPh = RootContext.CreateWidget<PlaceHolder>();
			editPh.Id = "EditPlaceHolder";
            PlaceHolder deletePh = RootContext.CreateWidget<PlaceHolder>();
			deletePh.Id = "DeletePlaceHolder";
			gridPane.Add( editPh );
			gridPane.Add( deletePh );
			return gridPane;
		}

        private bool useImageButtons = false;
		public bool UseImageButtons {
			get {
				return useImageButtons;
			}
			set {
				useImageButtons = value; 
			}
		}
		
		private void insertEditAndDeleteButtons( Template pane )
		{
			PlaceHolder editPh = pane.Find( "EditPlaceHolder" ) as PlaceHolder;
			PlaceHolder deletePh = pane.Find( "DeletePlaceHolder" ) as PlaceHolder;

			if( editPh != null && editPh.Widgets == null )
			{
                Widget editButton = RootContext.CreateUnkownWidget(buttonType);
				editButton.Id = "EditButton";
				string label = "Edit";
				editButton.AppendClass("edit");
				if( useImageButtons )
				{
					string path = ThemeManager.Instance.RequestClientPath( "/Images/Icons/Edit.png" );
					if( path != null )
					{
						label = string.Format("<img src='{0}' title='Edit'>", path );
					}
				}
				((IButton)editButton).Label = label;
				editPh.ClassName = "Edit Button";
				editButton.VisibleToPermission = EditVisibleTo;
				editButton.OnClick += new EventHandler<ClickEventArgs>(editButton_OnClick);
				editPh.Replace( editButton );
			}

			if( deletePh != null && deletePh.Widgets == null )
			{
                ConfirmButton deleteButton = RootContext.CreateWidget<ConfirmButton>();
				deleteButton.Id = "DeleteButton";
				string label = "Delete";
				deleteButton.ConfirmTitle = "Confirm Deletion";
				deleteButton.ConfirmText = "Please confirm that you want to delete this " + FriendlyModelName + ".";
				deleteButton.AppendClass("delete");
				if( useImageButtons )
				{
					string path = ThemeManager.Instance.RequestClientPath( "/Images/Icons/Delete.png" );
					if( path != null )
					{
						label = string.Format("<img src='{0}' title='Delete'>", path );
					}
				}
				
				deleteButton.Label = label;
				deletePh.ClassName = "Delete Button";
				deleteButton.VisibleToPermission = deleteVisibleTo;
				deleteButton.OnConfirm += new EventHandler<ClickEventArgs>(deleteButton_OnClick);
				deleteButton.OnClick += delegate {
					log.Debug("deletebutton clicked");
				};
				deletePh.Replace( deleteButton );
			}
		}

        bool dataBound = false;
        
        private bool setDataSource()
        {
        	if( dataSource == null )
			{
				if( propertySource != null && this.Record != null && this.Record[propertySource] is IRecordList<T> )
				{	
					dataSource = this.Record[propertySource] as IRecordList<T>;
					dataSource.OnRecordAdded += new EventHandler<RecordEventArgs>( delegate( object sender, RecordEventArgs ea )
	       			{
	       				log.Debug("RECORD ADDDED " + ea.Record);
	       				ea.Record.Parent = this.Record;
	       				this.Record.SaveRelations(propertySource);
	       			});
	       			dataSource.OnRecordRemoved += new EventHandler<RecordEventArgs>( delegate( object sender, RecordEventArgs ea )
	       			{
	       				log.Debug("RECORD REMOVED " + ea.Record );
	       				this.Record.SaveRelations(propertySource);
	       			});
	       			if( grid != null )
	       				grid.DataSource = dataSource;
	       			return true;
				}
				else
					log.Warn("Did not set property source", this, this.Record, propertySource, dataSource );
			}			
			return false;
        }
			
		public void Refresh()
			{
				DataSource = DataProvider.LoadList<T>(query.ToArray());
				DataBind();
			}
        
        public void DataBind()
        {
            if (!this.Initialized)
            {
            	//TODO: why can't we call init first?
                throw new ApplicationException("Scaffolds must be initialized before databinding.");
            }
            setDataSource();
        	if( dataSource != null && grid != null )
        	{
        		grid.IsDataBound = false;
        		grid.DataBind();
        	}        	
        	dataBound = true;
        }
        
        public bool IsDataBound { get { return dataBound; } set { dataBound = value; } }
        public override bool Render (Surface surface)
        {
        	if( ! dataBound )
                DataBind();
        	return base.Render(surface);
        }

		private void add_OnClick(object sender, ClickEventArgs ea)
		{
			//add the form.
            ShowAddForm();
		}

		public Stack Stack
		{
			get
			{
				if( stack == null )
				{
					stack = FindAncestor<Stack>();
				}
				return stack;
			}
			set
			{
				stack = value;
			}
		}
		
		public void ShowAddForm()
		{
			if( showAddButtonWidget != null )
				showAddButtonWidget.Visible = false;
			
			addForm = RootContext.CreateWidget<ModelForm<T>>();
			if( !string.IsNullOrEmpty(EditTemplate) )
				addForm.Template = EditTemplate;
			if( ! useStack )
				addPane.Add(addForm);
			else
				Stack.Push(addForm);
			if( AddPermission != null )
				addForm.Permission = addPermission;
            addForm.EnsureCurrentRecordOnEdit = false;
            if (DataSource != null)
            {
                addForm.Record = DataSource.NewRowT();
                addForm.Record.EnsureId();
            }
            else
                throw new NullReferenceException("DataSource should not be null.");
            
            if( applyFiltersToNew && DataSource != null && DataSource.Filters != null )
			{
				foreach( FilterInfo fi in DataSource.Filters )
				{
					addForm.Record[ fi.ColumnName ]  = fi.Value;
				}
			}
			if( defaultValues != null )
			{
				foreach( FilterInfo fi in defaultValues )
				{
					addForm.Record[ fi.ColumnName ]  = fi.Value;
				}
			}
            addForm.ReadOnlyFields = readOnlyFields;
            addForm.buttonType = this.buttonType;
            addForm.IgnoreDefaultTemplate = ignoreDefaultEditTemplate;
            //addForm.SaveAsYouGo = saveAsYouGo;
            addForm.DestructivelyEdit = true;
            addForm.ButtonLabel = "Add";
            if( AddExpanded )
            	addForm.ShowCancelButton = false;
            
            if( editTemplateNode != null )
            {
            	addForm.TagName = this.templateTagName ?? "div";
       			addForm.ParseElement(editTemplateNode);
       		}
       		addForm.AvailableRecords = availableRecordLists;
            addForm.AppendClass("item");
            addForm.OnBeforeSubmit += new EventHandler<EmergeTk.ModelFormEventArgs<T>>(Add_OnSubmit);
			addForm.OnValidationFailed += delegate
			{
				 addForm.StateBag.Remove("added");
            };
            addForm.OnAfterSubmit += delegate( object sender, EmergeTk.ModelFormEventArgs<T> ea )
            {
				if( ! dataSource.Contains( addForm.Record ) )
					dataSource.Add(addForm.Record);
        		addForm = null;
        		showAddButtonWidget.Visible = true;
        		if (AddExpanded)
	            {
	                ShowAddForm();
	            }
        		OnAfterSubmit(this, ea );
        		//if( UseStack )
				//	stack.Pop();
            };
            addForm.OnCancel += delegate
            {
            	addForm.Remove();
            	addForm = null;
            	showAddButtonWidget.Visible = true;
            	if( UseStack )
					Stack.Pop();
            };
            addForm.Id = "AddPane";
            addForm.Init();
       		//addPane.Add(addForm);
		}

        private Object addLocker = new Object();

        void Add_OnSubmit(object sender, EmergeTk.ModelFormEventArgs<T> ea )
        {        	
            lock (this.addLocker)
            {
                if (ea.Form.StateBag.ContainsKey("added"))
                {
                    throw new OperationCanceledException("add model form has already been submitted.");
                }
                else
                {
                    ea.Form.StateBag["added"] = true;
                }
            }
            
			if( OnBeforeSave != null )
			{
				ItemEventArgs iea = new ItemEventArgs(ea.Form.TRecord, null);
				OnBeforeSave(this, iea);
				if( ! iea.DoSave )
					throw new OperationCanceledException();
			}
			if( dataSource == null )
			{
				dataSource = new RecordList<T>();
				DataBind();
			}
            // addForm.Remove();
            
        }

		public override void ParseElement(System.Xml.XmlNode n)
		{
			//override for Grid, which is essenially a repeater.
			//and Add and Edit forms, which will be some sort of Pane.
            switch (n.LocalName)
            {
                case "GridTemplate":
                    ParseGridTemplate(n);
                    break;
                case "EditTemplate":
                	editTemplateNode = n;
                	break;
                case "AvailableRecords":
                    ParseAvailableRecords(n);
                    break;
                case "ReadOnlyField":
                	ParseFieldBehavior(n);
                	break;
            }
		}
		
		private System.Xml.XmlNode editTemplateNode;
		
		List<ColumnInfo> readOnlyFields;
		private void ParseFieldBehavior(System.Xml.XmlNode n)
		{
			T r = new T();			
			ReadOnlyFields.Add(r.GetFieldInfoFromName(n.Attributes["Name"].Value));			
		}

        Dictionary<string, AvailableRecordInfo> availableRecordLists;
        private void ParseAvailableRecords(System.Xml.XmlNode n)
        {
            //Field="InStockAt" Source="availableStores"
            if (availableRecordLists == null)
                availableRecordLists = new Dictionary<string, AvailableRecordInfo>();
            AvailableRecordInfo info = new AvailableRecordInfo();
            info.Source = n.Attributes["Source"].Value;
            if( n.Attributes["Format"] != null )
                info.Format = n.Attributes["Format"].Value;
            availableRecordLists[n.Attributes["Field"].Value] = info;
        }

        private void ParseGridTemplate(System.Xml.XmlNode n)
        {
            string model = null;
            if (n.Attributes["Model"] != null)
            {
                model = n.Attributes["Model"].Value;
                n.Attributes.Remove(n.Attributes["Model"]);
            }
            if (n.Attributes["Order"] != null)
            {
                order = n.Attributes["Order"].Value;
                n.Attributes.Remove(n.Attributes["Order"]);
            }

            Template p = RootContext.CreateWidget<Template>();
            //careful here - this is a bit of hack to get event handlers to wire up correctly
            p.Parent = this;
            p.ParseAttributes(n);
            p.ParseXml(n);

            if (model == null)
            {
                customGridTemplate = p;
            }
            else
            {
                if (customGridTemplates == null)
                    customGridTemplates = new Hashtable();
                customGridTemplates[model] = p;
            }
        }

		private ModelForm<T> SetupEditForm()
		{
			ModelForm<T> mf = RootContext.CreateWidget<ModelForm<T>>();
			if( !string.IsNullOrEmpty(EditTemplate) )
				mf.Template = EditTemplate;
          mf.EnsureCurrentRecordOnEdit = this.EnsureCurrentRecordOnEdit;
          if( editPermission != null )
		  	mf.Permission = editPermission;
	      mf.buttonType = this.buttonType;
		  mf.ButtonLabel = "Save";
	      mf.ReadOnlyFields = readOnlyFields;
	      mf.IgnoreDefaultTemplate = ignoreDefaultEditTemplate;
	      mf.SaveAsYouGo = SaveAsYouGo;
		  mf.ShowDeleteButton = showDeleteButton;
	      mf.DestructivelyEdit = destructivelyEdit;
	     
          if( editTemplateNode != null )
          {
          	mf.TagName = this.templateTagName ?? "div";
           	mf.ParseElement(editTemplateNode);          	
          }
          mf.AvailableRecords = availableRecordLists;
	      
	      mf.AppendClass("item");
	      mf.OnBeforeSubmit += Edit_OnSubmit;
	      mf.OnCancel += mf_OnCancel;
	      mf.OnAfterSubmit += OnAfterSubmit;
	      return mf;
		}
        
        private ModelForm<T> GetEditForm(Template t)
        {
	    	ModelForm<T> mf = SetupEditForm();
	    	if( editPermission != null )
				mf.Permission = editPermission;
	    	log.Debug("editing record", t.Record);
       		mf.Record = t.Record;
			mf.Id = "editform-" + t.Record.Definition;
 			mf.StateBag["viewWidget"] = t;
 			mf.Init();
			return mf;
        }

		bool showTemplateWhileEditing = false;
		bool modalEdit = false;
		private void editButton_OnClick(object sender, ClickEventArgs ea)
		{
			log.Debug("editButton_OnClick");
			Template t = ea.Source.FindAncestor<Template>();
			associateEditForm( t );
		}
		
		private void associateEditForm( Template t )
		{
			log.Debug("associateEditForm");
			Widget editForm = GetEditForm(t);
			if( useStack )
			{
				Stack.Push( editForm );
			}
			else if( modalEdit )
			{
				grid.InsertBefore( editForm );
				grid.Visible = false;
			}
			else
			{
				if( ! showTemplateWhileEditing )
					t.Visible = false;
				t.InsertAfter( editForm );
			}

			if( this.showAddButton != null )
	            (this.showAddButton as Widget).Visible = false;
		}

        void mf_OnCancel(object sender, EmergeTk.ModelFormEventArgs<T> ea )
        {
        	Template t =(ea.Form.StateBag["viewWidget"] as Template);
			grid.Visible = true;
			if( UseStack )
				Stack.Pop();
			if( this.showAddButton != null )
				(this.showAddButton as Widget).Visible = true;
			t.Visible = true;
			ea.Form.Remove();
        }

		public void OnAfterSubmit( object sender, EmergeTk.ModelFormEventArgs<T> ea)
		{
			Template t = null;
			if( ea.Form.StateBag.ContainsKey( "viewWidget" ) )
			{				
				t = (ea.Form.StateBag["viewWidget"] as Template);
			}
			if( OnAfterSave != null )
			{
				ItemEventArgs iea = new ItemEventArgs(ea.Form.TRecord,t);
				OnAfterSave( this, iea );
			}
			if( modalEdit )
			{
				grid.Visible = true;
			}
			if( UseStack )
				Stack.Pop();
			else
				ea.Form.Remove();
			
			
			//since we are making fresh copies of records on modelform edits, wehn we're done with the submission,
			//we want to rebind the template to the new version.
			if( t != null )
			{
				int index = this.dataSource.IndexOf(t.Record);
				this.dataSource[index] = ea.Form.TRecord;
				t.Unbind();
				t.DataBindWidget(ea.Form.TRecord,true);
			}

			if( this.showAddButton != null )
				(this.showAddButton as Widget).Visible = true;
		}

        void Edit_OnSubmit(object sender, EmergeTk.ModelFormEventArgs<T> ea )
        {
			Template t =(ea.Form.StateBag["viewWidget"] as Template);
			if( OnBeforeSave != null )
			{
				ItemEventArgs iea = new ItemEventArgs(ea.Form.TRecord,t);
				OnBeforeSave(this, iea);
				if( ! iea.DoSave )
					throw new OperationCanceledException();
			}
        }

		private void deleteButton_OnClick(object sender, ClickEventArgs ea )
		{
			if( deletePermission != null )
			{
				RootContext.EnsureAccess( deletePermission, delegate {
					delete( ea.Source );
				});
			}
			else
				delete( ea.Source );
		}

		private void delete(Widget w)
	    {
			Template t = w.FindAncestor<Template>();
            w.Record.Delete();

			while( t.Widgets.Count > 0 )
                t.RemoveChild( t.Widgets[0] );
		}

        #region IDataSourced Members

        IRecordList IDataSourced.DataSource
        {
            get
            {
                return DataSource as IRecordList;
            }
            set
            {
                DataSource = value as IRecordList<T>;
            }
        }

        public virtual EmergeTk.Widgets.Html.Repeater<T> Grid {
        	get {
        		if( grid == null )
        			grid = RootContext.CreateWidget<Repeater<T>>();
        		return grid;
        	}
        }

        public virtual bool UsePager {
        	get {
        		return usePager;
        	}
        	set {
        		usePager = value;
        	}
        }

        public virtual int PageSize {
        	get {
        		return pageSize;
        	}
        	set {
        		pageSize = value;
        	}
        }

        public virtual System.Collections.Generic.List<EmergeTk.Model.ColumnInfo> ReadOnlyFields {
        	get {
        		if( readOnlyFields == null )
					readOnlyFields = new List<ColumnInfo>();
        		return readOnlyFields;
        	}
        	set {
        		readOnlyFields = value;
        	}
        }

        public virtual string TagName {
        	get {
        		return tagName;
        	}
        	set {
        		tagName = value;
        		if( grid != null )
					grid.TagName = value;
        	}
        }

        public virtual string TemplateTagName {
        	get {
        		return templateTagName;
        	}
        	set {
        		templateTagName = value;
        		if( grid != null )
					grid.TemplateTagName = value;
        	}
        }

        public virtual string PropertySource {
        	get {
        		return propertySource;
        	}
        	set {
        		if( grid != null )
        			grid.PropertySource = value;
        		propertySource = value;
        			setDataSource();
        			
        	}
        }

        public System.Collections.Generic.List<IQueryInfo> Query {
        	get {
        		return query;
        	}
			set {
					query = value;
				}
        }

        public bool ApplyFiltersToNew {
        	get {
        		return applyFiltersToNew;
        	}
        	set {
        		applyFiltersToNew = value;
        	}
        }

        public bool ShowTemplateWhileEditing {
        	get {
        		return showTemplateWhileEditing;
        	}
        	set {
        		showTemplateWhileEditing = value;
        	}
        }

        public bool ModalEdit {
        	get {
        		return modalEdit;
        	}
        	set {
        		modalEdit = value;
        	}
        }

        public List<FilterInfo> DefaultValues {
        	get {
        		if( defaultValues == null )
        			defaultValues = new List<FilterInfo>();
        		return defaultValues;
        	}
        	set {
        		defaultValues = value;
        	}
        }

        public bool IgnoreDefaultEditTemplate {
        	get {
        		return ignoreDefaultEditTemplate;
        	}
        	set {
        		ignoreDefaultEditTemplate = value;
        	}
        }

        public bool SaveAsYouGo {
        	get {
        		return saveAsYouGo;
        	}
        	set {
        		saveAsYouGo = value;
        	}
        }

        public bool DestructivelyEdit {
        	get {
        		return destructivelyEdit;
        	}
        	set {
        		destructivelyEdit = value;
        	}
        }

        public bool AddExpanded {
        	get {
        		return addExpanded;
        	}
        	set {
        		if( value )
        			showNewButton = false;
        		addExpanded = value;
        	}
        }

        public bool UseEditForView {
        	get {
        		return useEditForView;
        	}
        	set {
        		useEditForView = value;
        	}
        }

        public bool ShowDeleteButton {
        	get {
        		return showDeleteButton;
        	}
        	set {
        		showDeleteButton = value;
        	}
        }

		public Template CustomGridTemplate
        {
        	get
            {
                if (this.customGridTemplate == null)
					{
					XmlNode node = ThemeManager.Instance.RequestView( 
						typeof(T).FullName.Replace('.', Path.DirectorySeparatorChar) + ".scaffoldtemplate" );
                    if ( node != null )
                    {
                        this.customGridTemplate = RootContext.CreateWidget<Template>(this, null, node);
                    }
                }
        		return customGridTemplate;
        	}
        	set { this.customGridTemplate = value; }
        }

		public AbstractRecord Selected {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
        }

		public Permission EditVisibleTo {
			get {
				return editVisibleTo;
			}
			set {
				editVisibleTo = value;
			}
		}

		public XmlNode EditTemplateNode {
			get {
				return editTemplateNode;
			}
			set {
				editTemplateNode = value;
			}
		}

		public Permission DeleteVisibleTo {
			get {
				return deleteVisibleTo;
			}
			set {
				deleteVisibleTo = value;
			}
		}

		public Permission DeletePermission {
			get {
				return deletePermission;
			}
			set {
				deletePermission = value;
			}
		}

		public Permission AddVisibleTo {
			get {
				return addVisibleTo;
			}
			set {
				addVisibleTo = value;
			}
		}

		public Permission AddPermission {
			get {
				return addPermission;
			}
			set {
				addPermission = value;
			}
		}

		public Permission EditPermission {
			get {
				return editPermission;
			}
			set {
				editPermission = value;
			}
		}

		public bool UseStack {
			get {
				return useStack;
			}
			set {
				useStack = value;
			}
		}
		
		

        #endregion
        
        public class ItemEventArgs : EventArgs
	    {
	    	public T UncommittedRecord;
	    	public T Record;
	    	public Template View;
	    	public bool DoSave = true;
	    	
	    	public ItemEventArgs(T record, Template view)
	    	{
	    		Record = record;
	    		View = view;
	    	}
	    	
	    	public ItemEventArgs(T uncommittedRecord, T record, Template view)
	    	{
	    		Record = record;
	    		View = view;
	    		UncommittedRecord = uncommittedRecord;
	    	}
	    }
    }
    

}
