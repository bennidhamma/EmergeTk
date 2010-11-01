using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model.Security;

namespace EmergeTk.Model.Workflow
{
    public class OperationState : AbstractRecord
    {
        private Operation operation;
        public Operation Operation
        {
            get { return operation; }
            set { operation = value; }
        }

		private Process process;
    	public Process Process
        {
            get { 
            	if( process == null && operation != null )
            		process = operation.Process;
            	return process; }
            set { process = value; }
        }
        
        private ProcessState processState;
        public ProcessState ProcessState
        {
        	get 
        	{
        		return processState;	
        	}
        	set
        	{
        		processState = value;
        	}				
        }
		
        private State state;
        public State State
        {
            get { return state; }
            set { state = value; 
            	if( ! loading && OnStateChanged != null )
            		OnStateChanged( this, EventArgs.Empty );
            	if( state == null )
            		throw new Exception("how is state null?");
            }
        }

		bool ready;
		public bool Ready {
        	get {
        		return ready;
        	}
        	set {
        		ready = value;
        	}
        }

		Task task;
        public Task Task {
        	get {
        		return task;
        	}
        	set {
				//log.Debug("OperationState: setting task - " + value);
        		task = value;
        	}
        }
        
        RecordList<OperationState> dependents;
        public RecordList<OperationState> DependentStates
        {
        	get {
        		if( dependents == null )
        		{
        			lazyLoadProperty<OperationState>("DependentStates");
        		}
        		return dependents;
        	}
			set {
				dependents = value;
			}
        }

		[PropertyType(DataType.RecordSelect)]
        public User CheckedOutBy {
        	get {
        		return checkedOutBy;
        	}
        	set {
				//log.Debug("OperationState: setting CheckedOutBy - " + value);
        		checkedOutBy = value;
        		if( checkedOutBy != null )
        		{
        			CheckedOutOn = DateTime.Now;
        		}
        		if( ! loading )
        		{
	        		if( OnOperationCheckedOut != null )
	        			OnOperationCheckedOut( this, EventArgs.Empty );
	        	}
        	}
        }
        
        private DateTime checkedOutOn;
		public DateTime CheckedOutOn {
        	get {
        		return checkedOutOn;
        	}
        	set {
        		checkedOutOn = value;
        	}
        }
        
		public bool IsCheckedOut
		{
			get
			{
				return !(CheckedOutBy == null);
			}
		}
		
        public string Status
		{
			get
			{
				if ( CheckedOutBy != null )
					return "Checked out by " + CheckedOutBy;
				else if ( Ready )
					return "Ready";
				else 
					return "Not Ready";
			}
		}

		
		
        User checkedOutBy;
        
        public override string ToString()
        {
        	if( task == null || operation == null )
        		return base.ToString();
        	return string.Format( "[{0}] {1}-{2} ({2})", Id, operation.Name, state, task.GetType().Name );
        }
       
        //Notify interested parties when states change.
        public static event EventHandler OnStateChanged;
        public static event EventHandler OnOperationCheckedOut;
        
//        public override void Validate ()
//        {
//        	if( state == null )
//				throw new EmergeTk.Model.ValidationException("State is null");
//			if( operation == null )
//				throw new EmergeTk.Model.ValidationException("Operation is null.");
//			if( ProcessState == null )
//        		throw new EmergeTk.Model.ValidationException("Why is process state null?  This is really bad.");
//        	if( task == null )
//        		throw new EmergeTk.Model.ValidationException("Task is null.");
//        	base.Validate ();
//        }

        public override void Save ()
        {        	
        	base.Save();
        	//only update ready states if this is a new record, or state has changed
        	if( state != null && task != null && ( OriginalValues == null || (int)OriginalValues["State"] != state.Id ) )
        	{
        		
        		if( ProcessState != null )
				{
        			task.UpdateReadyStates(ProcessState, this);
				}
			}
				
        }
        
    }
}
