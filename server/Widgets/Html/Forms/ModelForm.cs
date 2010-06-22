using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public enum ModelFormResult {
		Presubmitting,
		Submitted,
		Cancelled,
		ValidationFailed,
		Deleted
	}
	
    public class ModelForm<T> : Generic, ISubmittable where T : AbstractRecord, new()
    {
		protected static EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ModelForm<T>));
		                                                                
    	bool saveAsYouGo, closeOnSubmit, showDeleteButton, showSaveButton = true, showCancelButton = true;
    	bool destructivelyEdit = false;
    	Dictionary<string, AvailableRecordInfo> availableRecords;
    	ModelFormResult result;
    	string template = "modelform";
    	static Type defaultButtonType;
    	
		public string CancelButtonText { get; set; }
		
    	static ModelForm()
    	{
    		defaultButtonType = TypeLoader.GetType(Setting.GetValueT<string>("DefaultButtonType","EmergeTk.Widgets.Html.Button"));
    	}
    	
    	bool submitChildren = true;
    	
        internal Type buttonType = defaultButtonType;
        private string button;
        public string Button
        {
            get { return button; }
            set { button = value; buttonType = TypeLoader.GetType(button);  }
        }
        private string buttonLabel = "Submit";
        public string ButtonLabel
        {
            get { return buttonLabel; }
            set { buttonLabel = value; }
        }
        
        bool ignoreDefaultTemplate = false;
        
        public event EventHandler<ModelFormEventArgs<T>> OnBeforeSubmit;
        public event EventHandler<ModelFormEventArgs<T>> OnCancel;
		public event EventHandler<ModelFormEventArgs<T>> OnValidationFailed;
        public event EventHandler<ModelFormEventArgs<T>> OnAfterSubmit;
        public event EventHandler<ModelFormDeleteEventArgs<T>> OnBeforeDelete;
        public event EventHandler<ModelFormEventArgs<T>> OnDelete;

        private object onSubmitArg;
        public object OnSubmitArg
        {
            get { return onSubmitArg; }
            set { onSubmitArg = value; }
        }
        
        private List<ColumnInfo> readOnlyFields;
        public List<ColumnInfo> ReadOnlyFields { get {
        	if( readOnlyFields == null )
        		readOnlyFields = new List<ColumnInfo>();
        	return readOnlyFields;
        } 
        	set {
        		readOnlyFields = value;
        	}
        }

        Model.ColumnInfo[] fields;
		
		public T TRecord
		{
			get { return (T)Record; }
		}
		
		private bool renderButtons = true;
		public bool RenderButtons { get { return renderButtons; } set { renderButtons = value; } }

        public virtual bool SaveAsYouGo {
        	get {
        		return saveAsYouGo;
        	}
        	set {
        		saveAsYouGo = value;
        		if( saveAsYouGo )
        		{
        			ShowSaveButton = false;
        			ShowCancelButton = false;
        		}
        	}
        }

        public virtual System.Collections.Generic.Dictionary<string, AvailableRecordInfo> AvailableRecords {
        	get {
        		return availableRecords;
        	}
        	set {
        		availableRecords = value;
        	}
        }

        public bool CloseOnSubmit {
        	get {
        		return closeOnSubmit;
        	}
        	set {
        		closeOnSubmit = value;
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

        public ModelFormResult Result {
        	get {
        		return result;
        	}
        	set {
        		result = value;
        	}
        }

        public bool ShowSaveButton {
        	get {
        		return showSaveButton;
        	}
        	set {
        		showSaveButton = value;
        	}
        }

        public bool ShowCancelButton {
        	get {
        		return showCancelButton;
        	}
        	set {
        		showCancelButton = value;
        	}
        }

        public bool IgnoreDefaultTemplate {
        	get {
        		return ignoreDefaultTemplate;
        	}
        	set {
        		ignoreDefaultTemplate = value;
        	}
        }

        private bool ensureCurrentRecordOnEdit = false;
        public bool EnsureCurrentRecordOnEdit
        {
            get { return ensureCurrentRecordOnEdit; }
            set { ensureCurrentRecordOnEdit = value; }
        }

        public override void Initialize()
        {
			BindsTo = typeof(T);
			
            if (this.ensureCurrentRecordOnEdit && this.Record != null )
            {
                this.Record = AbstractRecord.Load<T>(this.Record.Id);
            }

        	this.BindProperty( "Record", new NotifyPropertyChanged( delegate() {
        		log.Debug("setting record ", Record );
        		UnbindForm();
        		if( Record != null ) BindForm();        		
        	}));
        	
        	string classNames = string.Format( "modelForm {0}", this.Name.Replace(".","-") );
            this.AppendClass(classNames);

			log.Debug("init modelform ", typeof(T), Record, Record is T, saveAsYouGo, destructivelyEdit);
            if( !(Record is T) )
            {
            	Record = new T();
            }
            else if ( ! saveAsYouGo && ! destructivelyEdit && ! ensureCurrentRecordOnEdit)
            {
				log.Debug("making clone copy");
				AbstractRecord oldRecord = Record;
            	SetRecord( AbstractRecord.CreateFromRecord<T>(TRecord) );
				log.Debug("oldRecord == new record? " + ( (object)oldRecord == (object)Record ) );
            }

            if( ! templateParsed && ! ignoreDefaultTemplate )
			{
				//attempt to load xml at ModelForm/FullName.xml
				XmlNode node = ThemeManager.Instance.RequestView( typeof(T).FullName.Replace('.', Path.DirectorySeparatorChar) + "." + template );
				
				if( node != null )
				{
					node = ExpandMacros( node );
					RootContext.CreateWidget<Generic>(this, Record, node);
	                templateParsed = true;
				}
			}
			
            fields = Record.Fields;
            
            foreach(Model.ColumnInfo fi in fields)
			{
				//TODO: if there is a custom template, we should continue early if the field is not in the form, and avoid
				//the cost of edit widget instantiation.
				if( readOnlyFields != null && readOnlyFields.Contains( fi ) )
					continue;
				PlaceHolder input = null, field = null;
				if( templateParsed )
				{
					input = Find<PlaceHolder>(fi.Name + "Input");
					field = Find<PlaceHolder>(fi.Name + "LabeledInput");
					if( input == null && field == null )
					{				
						continue;
					}
				}
				IRecordList fieldRecords = null;
				if( availableRecords != null && availableRecords.ContainsKey(fi.Name) &&
					this.RootContext.RecordLists.ContainsKey(availableRecords[fi.Name].Source) )
				{
					fieldRecords = RootContext.RecordLists[availableRecords[fi.Name].Source];
				}
				else
				{
					fieldRecords = Record.GetAvailableOptions(this, fi);
				}
				Widget propWidget = null;
				propWidget = Record.GetPropertyEditWidget(this, fi, fieldRecords);
				if( propWidget == null && Record[fi.Name] is AbstractRecord )
				{
					propWidget = (Record[fi.Name] as AbstractRecord).GetEditWidget(this, fi, fieldRecords);
				}
				if( ! templateParsed )
				{
					if( propWidget == null )
						propWidget = DataTypeFieldBuilder.GetEditWidget( fi, FieldLayout.Spacious, fieldRecords, DataType.None  );
					if( propWidget != null )
					{
						propWidget.Id = this.Id + "." + fi.Name;
						Add( propWidget );
						propWidget.AppendClass(string.Format(" editFieldInput editField{0} {1} {2}",fi.Name, fi.DataType, fi.PropertyInfo.PropertyType.Name));
					}
				}
				else
				{
					if( input != null )
					{
						if( propWidget == null )
							propWidget = DataTypeFieldBuilder.GetEditWidget( fi, FieldLayout.Terse, fieldRecords, DataType.None  );
						if( propWidget != null )
						{
							propWidget.AppendClass( string.Format("terse editFieldInput editFieldInput{0} {1} {2}",fi.Name,
								propWidget.ClientClass,  fi.PropertyInfo.PropertyType.Name) );
							//propWidget.Id = fi.Name + "InputWidget";
							propWidget.Id = this.Id + "." + fi.Name + "InputWidget";
							input.Replace(propWidget);
						}
						else
							log.Debug("why can't we find a prop widget?", fi.Name);
					}
					PlaceHolder label = Find<PlaceHolder>(fi.Name + "Label");
					if( label != null  )
					{
						HtmlElement l = RootContext.CreateWidget<HtmlElement>();
						Literal lt = RootContext.CreateWidget<Literal>(l);
						lt.Html = Util.PascalToHuman(fi.Name);
						l.TagName = "label";
						if ( propWidget != null )
							l.SetClientElementAttribute("for","'" + propWidget.ClientId + "'" );
						label.AppendClass ( "fieldLabel fieldLabel" + fi.Name );						
						label.Replace(l);
					}					
					if( field != null  )
					{
						if( propWidget == null )
							propWidget = DataTypeFieldBuilder.GetEditWidget( fi, FieldLayout.Spacious  );
						if( propWidget != null )
						{
							propWidget.Id = this.Id + "." + fi.Name;
							propWidget.AppendClass( "editField editField" + fi.Name );
							field.Replace(propWidget);
						}
					}
				}
			}
            BindForm();
            
            if( renderButtons )
            {
            	Pane buttonPane = RootContext.CreateWidget<Pane>(this);
            	buttonPane.ClassName = "buttons clearfix";
            	Widget w;
            	
	            if( showSaveButton )
	            {
	            	Widget button = Context.Current.CreateUnkownWidget(buttonType);
		            ((IButton)button).Label = buttonLabel;
		            button.OnClick += new EventHandler<ClickEventArgs>(button_OnClick);
		            button.AppendClass( "submit" );
		            button.Id = "SubmitButton";
					w = Find("SubmitButton");
					if( w != null )
						w.Replace( button );
					else
						buttonPane.Add( button );
				}
				if( ShowCancelButton )
				{
		            Widget cancel = RootContext.CreateUnkownWidget(buttonType);
					((IButton)cancel).Label = CancelButtonText ?? "Cancel";
		            ((IButton)cancel).Arg = buttonLabel;
		            cancel.AppendClass("cancel");
		            cancel.OnClick += new EventHandler<ClickEventArgs>(cancel_OnClick);
		            cancel.Id = "CancelButton";
					w = Find("CancelButton");
					if( w != null )
						w.Replace( cancel );
					else
						buttonPane.Add( cancel );
				}

				if( showDeleteButton )
				{
					ConfirmButton delete = RootContext.CreateWidget<ConfirmButton>();
					//delete.Label = "<div class=\"DeleteBox\"><img class=\"delete\" src=\"/Images/Icons/user-trash-full.png\"><br/>Delete</div>";
					delete.Label = "Delete";
					delete.Id = "DeleteButton";
					delete.OnConfirm += new EventHandler<ClickEventArgs>( delegate( object sender, ClickEventArgs ea ) {
						if( OnBeforeDelete != null )
						{
							EmergeTk.ModelFormDeleteEventArgs<T> mfdea = new EmergeTk.ModelFormDeleteEventArgs<T>(this);
							OnBeforeDelete(this, mfdea );
							if( mfdea.Abort )
								return;
						}
						Record.Delete();
						Remove();
						result = ModelFormResult.Deleted;
						if( OnDelete != null )
							OnDelete( this, new EmergeTk.ModelFormEventArgs<T>( this ) );
					});
					w = Find("DeleteButton");
					if( w != null )
						w.Replace( delete );
					else
						buttonPane.Add( delete );
				}
			}

            
        }

		public override void PostInitialize ()
		{
			this.DataBindWidget();
		}

        
        public Widget DiscoverEditWidgetForType( Type t, object o )
        {
        	MethodInfo mi = t.GetMethod("GetEditWidget",BindingFlags.Static);
        	return mi.Invoke(null, new object[]{o}) as Widget;
        }
        
        private XmlNode ExpandMacros( XmlNode n )
        {
        	
        	//replace references to {FieldInput} and {FieldLabel} and {Field} to <emg:PlaceHolder Id="FieldInput"/> etc.
        	string xml = n.OuterXml;
			string prefix = ! string.IsNullOrEmpty( n.Prefix ) ? n.Prefix + ":" : ! string.IsNullOrEmpty( RootContext.TagPrefix ) ? RootContext.TagPrefix + ":" : "";
        	Regex r = new Regex(@"\{(\w+(Input|Label|LabeledInput))\}");
        	xml = r.Replace(xml,string.Format("<{0}PlaceHolder Id=\"$1\"/>",prefix));
//        	log.Debug("xml is ", xml );
        	XmlDocument doc = new XmlDocument();
        	doc.LoadXml(xml);
        	return doc.FirstChild;
        }
        
        bool templateParsed = false;
        public void ParseTemplate( XmlNode n )
        {
        	n = ExpandMacros( n );
        	this.ParseAttributes(n);
        	this.ParseXml(n);
        	templateParsed = true;
        }
        
        void cancel_OnClick(object sender, ClickEventArgs ea)
        {
        	clearValidationErrors();
        	if( this.CloseOnSubmit )
            	Remove();
            result = ModelFormResult.Cancelled;
            if( OnCancel != null )
                OnCancel(this, new EmergeTk.ModelFormEventArgs<T>(this) );
        }

        void button_OnClick(object sender, ClickEventArgs ea)
        {
           Submit();
        }
		
        public void Submit()
        {
        	if( submitChildren )
        	{
        		List<ISubmittable> submittables = FindAll<ISubmittable>();
                if (submittables != null)
                {
                    foreach (ISubmittable s in submittables)
                    {
                        s.Submit();
                    }
                }
        	}
        	
        	if( OnBeforeSubmit != null )
			{
				try
				{
					result = ModelFormResult.Presubmitting;
					OnBeforeSubmit(this, new EmergeTk.ModelFormEventArgs<T>(this) );
				}
				catch( OperationCanceledException e )
				{
					log.Debug("operation cancelled.", Util.BuildExceptionOutput( e ) );
					return;
				}
			}
			clearValidationErrors();
            try
			{
				log.Debug("Saving record: " + Record );
           		Record.Save();
           	}
           	catch( ValidationException ve )
           	{
           		if( validationErrors == null )
       				validationErrors = new List<Label>();
				
       			Label errorLabel = RootContext.SendClientNotification("error", ve.Message);
				validationErrors.Add(errorLabel);
				if( ve.Errors != null )
				{
					errorLabel.Text += "<ul>";					
					foreach( ValidationError error in ve.Errors )
					{
						errorLabel.Text += "<li>" + error.Problem + "</li>";
					}
					errorLabel.Text += "</ul>";
				}
				
       			result = ModelFormResult.ValidationFailed;
       			if( OnValidationFailed != null )
          			OnValidationFailed(this, new EmergeTk.ModelFormEventArgs<T>(this) );
       			return;
           	}
		
			result = ModelFormResult.Submitted;
          	if( OnAfterSubmit != null )
          		OnAfterSubmit(this, new EmergeTk.ModelFormEventArgs<T>(this) );
          	if( closeOnSubmit )
          		Remove();
        }
        
        private void clearValidationErrors()
        {
        	if( validationErrors != null )
       		{
       			foreach( Label l in validationErrors )
       			{
       				RootContext.DismissNotification( l );
       			}
       			validationErrors.Clear();			
       		}
        }

        public void BindForm()
        {
        	if( fields == null )
        		return;
            foreach (ColumnInfo fi in fields)
            {
            	if( this.ReadOnlyFields != null && ReadOnlyFields.Contains(fi) )
                	continue;
			//log.Info( "Binding ", this.Id + "." + fi.Name );
                bindField(this.Id + "." + fi.Name,fi.Name);
                bindField(this.Id + "." + fi.Name + "InputWidget", fi.Name);
			//	log.Info( "DONE Binding ", this.Id + "." + fi.Name );
                //bindField(fi.Name,fi.Name);
                //bindField(fi.Name + "InputWidget", fi.Name);
            }
        }

        List<string> savedFields = null;

        public List<string> SavedFields
        {
            get { return savedFields; }
            set { savedFields = value; }
        }

        public bool DestructivelyEdit {
        	get {
        		return destructivelyEdit;
        	}
        	set {
        		destructivelyEdit = value;
        	}
        }
        
        List<Label> validationErrors;
        public void bindField( string widgetId, string fieldName )
        {
        	Widget c = Find(widgetId);
        	if (c != null && c is IDataBindable)
        	{
				if( c is IWidgetDecorator )
				{
					c = ((IWidgetDecorator)c).Widget;
				}
				IDataBindable i = c as IDataBindable;
				//log.Debug( "binding field, ", i, i.DefaultProperty, local, fieldName );
				c.Bind(i.DefaultProperty,Record, fieldName);
				c.BindProperty(i.DefaultProperty, delegate(){
				//log.Debug("field changed", i.DefaultProperty, saveAsYouGo, local == Record );
	               if( saveAsYouGo )
	               {
	               		clearValidationErrors();
		                try
						{
			           		Record.Save();
			           	}
			           	catch( ValidationException ve )
			           	{
			           		if( validationErrors == null )
	               				validationErrors = new List<Label>();
	               			validationErrors.Add( RootContext.SendClientNotification("error", ve.Message) );
			           	}
	               }
	               else
	               {
	               		if( savedFields == null )
	               			savedFields = new List<string>();
	               		if( ! savedFields.Contains( fieldName ) )
	               			savedFields.Add( fieldName );
	               }
               });               
			}
//			else
//			{
//				log.Error("Could not find field!", widgetId, fieldName );
//			}
        }
        
        string defaultProperty = "Record";
        public override string DefaultProperty {
        	get { return defaultProperty; }
        	set { defaultProperty = value; }
        }
        
        public override object Value {
        	get { return Record; }
        	set { Record = (AbstractRecord)value; 
        	}
        }

        public bool SubmitChildren {
        	get {
        		return submitChildren;
        	}
        	set {
        		submitChildren = value;
        	}
        }

        public string Template {
        	get {
        		return template;
        	}
        	set {
        		template = value;
        	}
        }

        public override void ParseElement(System.Xml.XmlNode n)
		{
			//override for Grid, which is essenially a repeater.
			//and Add and Edit forms, which will be some sort of Pane.
            switch (n.LocalName)
            {
                case "ReadOnlyField":
                	ParseFieldBehavior(n);
                	break;
                case "Template":
                case "EditTemplate":
                	ParseTemplate(n);
                	break;
                default:
                	base.ParseElement(n);
                	break;
            }
		}
		
		public void ParseFieldBehavior(System.Xml.XmlNode n)
		{
			T r = new T();			
			ReadOnlyFields.Add(r.GetFieldInfoFromName(n.Attributes["Name"].Value));			
		}

        public void UnbindForm()
        {
        	if( fields == null )
        		return;
            foreach (ColumnInfo fi in fields)
            {
                if (fi.DataType != DataType.RecordList)
                {
                	if( this.ReadOnlyFields != null && ReadOnlyFields.Contains(fi) )
                		continue;
                    Widget c = Find(fi.Name);
                    if( c != null )
                    	c.Unbind();
                    c = Find(fi.Name + "InputWidget");
                    if( c != null )
                   		c.Unbind();
                }
            }
        }
    }
}
