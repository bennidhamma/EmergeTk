using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model.Workflow
{
	public class TaskStateDependency : OperationDependency
	{
		State taskState;
		
		public State TaskState {
			get {
				return taskState;
			}
			set {
				taskState = value;
				NotifyChanged("TaskState");
				Save();
			}
		}
		
		public TaskStateDependency()
		{
		}
		
		public override bool IsDependencyResolved (ProcessState ps, OperationState os, OperationState changedOpState, bool satisfyDependency)
		{
			return ( os.Task.CurrentState == taskState ) != IsNonDependency;
		}

		public override Widget GetEditWidget (Process process)
		{
			Pane p = Context.Current.CreateWidget<Pane>();
			Label l = Context.Current.CreateWidget<Label>(p);
			l.Text="Task State: ";
			l.TagName = "span";
			RecordSelect<State> stateSelect = Context.Current.CreateWidget<RecordSelect<State>>(p);
			stateSelect.DataSource = DataProvider.LoadList<State>();
			p.Init();
			stateSelect.DataBind();
			stateSelect.Bind( this, "TaskState");
			p.Add( base.GetEditWidget( process ) );
			return p;
		}
	}
}
