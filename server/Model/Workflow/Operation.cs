using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk;
using EmergeTk.Administration;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model.Workflow
{
    public class Operation : AbstractRecord, ISingular
    {
    	//private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Operation));
		private State defaultState;
		
    	private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        
        Process process;
    	[PropertyType(DataType.RecordSelect)]
        public Process Process {
        	get {
        		return process;
        	}
        	set {
        		process = value;
        	}
        }
    	
        //is ITaskEditor
        private string editWidget;
        public string EditWidget
        {
            get { return editWidget; }
            set { editWidget = value; }
        }
        
        [PropertyType(DataType.RecordSelect)]
        public State DefaultState {
        	get {
        		return defaultState;
        	}
        	set {
        		if( value == null )
        			throw new Exception("State cannot be null");
        		defaultState = value;
        	}
        }
        
        private string argument;
        public string Argument
        {
            get { return argument; }
            set { argument = value; }
        }
        
        private OperationDependency dependency;
        public OperationDependency Dependency
        {
            get { return dependency; }
            set {
            	dependency = value; 
            }
        }
		
        private RecordList<Permission> permissions;
        public EmergeTk.Model.RecordList<Permission> Permissions {
        	get {
        		if( permissions	== null )
            		lazyLoadProperty<Permission>("Permissions");
        		return permissions;
        	}
        	set {
				permissions = value;
        	}
        }
		
        public Widget NewEditWidget()
        {
        	Type type = TypeLoader.GetType( editWidget );
        	if( type != null )
        		return (Widget)Activator.CreateInstance(type);
        	else
        		return null;
        }
        
        public override void Save ()
        {
        	base.Save();
        	if( this.process != null )
        	{
        		if( this.OriginalValues != null && (int) this.OriginalValues["Process"] != this.process.Id )
        		{
        			Process oldProc = AbstractRecord.Load<Process>( this.OriginalValues["Process"] );
        			if( oldProc != null )
        			{
        				if( oldProc.Operations.Contains( this ) )
        				{
        					oldProc.Operations.Remove(this);
        					oldProc.SaveRelations("Operations");
        				}
        			}
        		}
        		if( ! this.process.Operations.Contains( this ) )
        		{
        			this.process.Operations.Add(this);
        			this.process.SaveRelations("Operations");
        		}
        	}
        }
       
//		public override void Validate ()
//		{
//			if( this.process == null )
//			{
//				throw new ValidationException("Process cannot be null.");
//			}			
//		}
		
		public override string ToString()
		{
			return string.Format("{0} ({1})", name, process != null ? process.Name : "NA");
		}

        public override Widget GetPropertyEditWidget(Widget parent, ColumnInfo column, IRecordList records)
        {
            switch (column.Name)
            {
                case "Permissions":
                    return  setupProp<Permission>("Permissions", this.Permissions );
				case "Dependency":
					Label head = Context.Current.CreateWidget<Label>(this);
					head.TagName = "h3";
					head.Text = "Dependency Information";
						
	                DependencyEditor de = Context.Current.CreateWidget<DependencyEditor>();
	                de.Bind("Process", this, "Process");
	                de.Bind("Dependency", this, "Dependency");
	                de.Init();	                
	                
	                Pane p = Context.Current.CreateWidget<Pane>();
	                p.Add( head, de );
	                return p;
                case "EditWidget":
                	LabeledWidget<DropDown> dd = Context.Current.CreateWidget<LabeledWidget<DropDown>>();
                	dd.LabelText = "Edit Widget";
                	dd.Widget.Options = getTaskEditors();
                	dd.Widget.DefaultProperty = "SelectedOption";
                	dd.Widget.OnChanged += new EventHandler<ChangedEventArgs>(delegate( object sender, ChangedEventArgs ea ) {
                		string opt = dd.Widget.SelectedOption;
                		this.editWidget = opt != "--SELECT--" ? opt : null; 
                	});
                	dd.Widget.SelectedOption = this.editWidget;
                	return dd;
                	
                default:
                    return base.GetPropertyEditWidget(parent, column, records);
            }
        }
        
        static List<string> taskEditors = null;
        static private List<string> getTaskEditors()
        {
        	if( taskEditors != null && taskEditors.Count > 0 )
        		return taskEditors;
        	Type[] types = TypeLoader.GetTypesOfInterface(typeof(ITaskEditor));
            taskEditors = new List<string>();
            taskEditors.Add("--SELECT--");
            foreach ( Type t in types )
            	taskEditors.Add( t.FullName );
            return taskEditors;
        }
            
        private Widget setupProp<T>(string prop, RecordList<T> list ) where T : AbstractRecord, new()
        {
        	
        	SelectList<T> sl = Context.Current.CreateWidget<SelectList<T>>();
            sl.ClassName = "clearfix";	
            sl.Mode = SelectionMode.Multiple;
            sl.LabelFormat = "{Name}";
            sl.SelectedItems = list;
            Label l = Context.Current.CreateWidget<Label>();
	        l.Text = prop;
	        l.Init();
            sl.Header = l;
            sl.Init();
            l.TagName = "h3";
	        sl.DataSource = DataProvider.LoadList<T>();
            sl.DataBind();
            sl.OnChanged += new EventHandler<ChangedEventArgs>(delegate (object sender, ChangedEventArgs ea )
	        {
	        	//we need to save the object if it's id is 0, b/c otherwise the associations will go to the wrong place.
	        	EnsureId();
				SaveRelations(prop);
	        });
	       	//sl.Footer.TagName = "hr";
            return sl;
        }      
    }
}
