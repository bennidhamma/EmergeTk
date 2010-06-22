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
	public class OperationStateDependency : OperationDependency, ISingular
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(OperationStateDependency));
		Operation operation;
		State operationState;

		public Operation Operation {
			get {
				return operation;
			}
			set {
				operation = value;
				NotifyChanged("Operation");
				Save();
			}
		}

		public State OperationState {
			get {
				return operationState;
			}
			set {
				operationState = value;
				NotifyChanged("OperationState");
				Save();
			}
		}
		
		public OperationStateDependency()
		{
		}
		
		public override bool IsDependencyResolved (
			ProcessState ps, 
			OperationState dependentState, 
			OperationState changedOpState, 
			bool satisfyDependency)
		{
			bool found = false;
			
			//loop through all other operationstates for the task and look to see if any are a match for operation & state
			if( changedOpState != null )
			{
				found = resolveDep( dependentState, changedOpState, satisfyDependency );
			}
			
			//otherwise loop through all the op states and see if the dep is satisfied.
			if( ! found )
			{
				
				//loop through all other operationstates for the process state and look to see if any are a match for operation & state
			foreach( OperationState potentialDependency in ps.Operations )
				{
					found = resolveDep(dependentState, potentialDependency, satisfyDependency);
					if( found )
						break;
				}
			}
			//log.Debug( "returning ", found != IsNonDependency ); 
			return found != IsNonDependency;
		}
		
		private bool resolveDep( OperationState dependentState, OperationState potentialDependency, bool satisfyDependency )
			{
				if( potentialDependency.Operation == operation && potentialDependency.State == operationState )
				{
					//we are a match.  Next we want to ensure that any given dependency is only associated to one other operation state
					//of a given operation, that is to say an operation can be a dependent to more than one KIND of operation, but only 
					//fulfill a dependency ONCE for a specific operation.
					
					//first assume we have a valid slot.
					bool hasValidSlot = true;
					
					//loop through all the prexisting dependents of this potential dependency to make sure there is a valid slot.
					foreach( OperationState stateSlot in potentialDependency.DependentStates )
					{
						//if this dependent's operation equals our target op, and the state slot is NOT the target opstate, this dependency
						//has already been consumed by a different dependent of the same operation.
					if( stateSlot.Operation == dependentState.Operation && stateSlot.Id != dependentState.Id )
						{
							log.Debug("DEPEDENDANT SLOT CONSUMED ", stateSlot.Operation.Name );
							hasValidSlot = false;
						return false;
						}
					}
					
					if( hasValidSlot )
					{
						log.Debug("DEPENDENCY ", operation.Name, " RESOLVED FOR ", dependentState.Id, " BY ", potentialDependency.Id );
						//we have found a valid operationstate to satisfy this dependency.
						if( satisfyDependency )
						{
							potentialDependency.DependentStates.Add( dependentState );
							potentialDependency.SaveRelations("DependentStates");
						}
							
					return true;
					}
				}
			return false;	
		}
		
		public override Widget GetEditWidget (Process process)
		{
			Pane p = Context.Current.CreateWidget<Pane>();
			Label l = Context.Current.CreateWidget<Label>(p);
			l.Text="Operation: ";
			l.TagName = "span";
			RecordSelect<Operation> operationSelect = Context.Current.CreateWidget<RecordSelect<Operation>>(p);
			operationSelect.DataSource = process.Operations;
			operationSelect.Init();
			operationSelect.DataBind();
			operationSelect.Bind( this, "Operation");
			
			l = Context.Current.CreateWidget<Label>(p);
			l.Text=" &nbsp;State: ";
			l.TagName = "span";
			RecordSelect<State> stateSelect = Context.Current.CreateWidget<RecordSelect<State>>(p);
			stateSelect.DataSource = DataProvider.LoadList<State>();
			stateSelect.Init();
			stateSelect.DataBind();
			stateSelect.Bind( this, "OperationState");
			
			p.Add( base.GetEditWidget(process) );
			return p;
		}
	}
}
