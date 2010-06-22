using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model.Workflow
{
    public class Process : AbstractRecord, ISingular
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private IRecordList<Operation> operations;
        public IRecordList<Operation> Operations
        {
            get {
            	if( operations == null )
            		lazyLoadProperty<Operation>("Operations");
            	return operations; }
            set { operations = value; }
        }

        private IRecordList<State> permittedStates;
        public IRecordList<State> PermittedStates
        {
            get {
            	if( permittedStates == null )
            		lazyLoadProperty<State>("PermittedStates");
            	return permittedStates; }
            set { permittedStates = value; }
        }
        
        private string payloadTypeFriendlyName;
        public string PayloadTypeFriendlyName {
        	get { 
        		return payloadTypeFriendlyName;
        	}
        	set {
        		payloadTypeFriendlyName = value;
        	}
        }
        
		private string payloadType;
        public string PayloadType {
        	get {
        		return payloadType;
        	}
        	set {
        		payloadType = value;
        	}
        }
        
        public override Widget GetPropertyEditWidget(Widget parent, ColumnInfo column, IRecordList records)
        {
            switch (column.Name)
            {
                case "PayloadType":
                    LabeledWidget<DropDown> dd = Context.Current.CreateWidget<LabeledWidget<DropDown>>();
                    dd.LabelText = "Task Type";
                    dd.Widget.Options = getTaskTypes();
                    dd.Widget.OnChanged += new EventHandler<EmergeTk.ChangedEventArgs>(delegate(object sender, ChangedEventArgs ea )
                    {
                        string opt = dd.Widget.SelectedOption;
                        this.payloadType = opt != "--SELECT--" ? opt : null;
                    });
                    dd.Widget.SelectedOption = this.payloadType;
                    return dd;
                default:
                    return base.GetPropertyEditWidget(parent, column, records);
            }
        }
        
        List<string> taskTypes = null;
        private List<string> getTaskTypes()
        {
        	if( taskTypes != null )
        		return taskTypes;
        	Type[] types = TypeLoader.GetTypesOfBaseType(typeof(Task));
            taskTypes = new List<string>();
            taskTypes.Add("--SELECT--");
            foreach ( Type t in types )
            	taskTypes.Add( t.FullName );
            return taskTypes;
        }

		public override string ToString()
		{
			return name;	
		}
    }
}
