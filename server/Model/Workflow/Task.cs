using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model.Workflow
{
    public class Task : AbstractRecord
    {
    	private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Task));
        private State currentState;
        [PropertyType(DataType.RecordSelect)]
        public State CurrentState
        {
            get { return currentState; }
            set {
            	if( value != currentState )
            	{
            		currentState = value;
            	}
            }
        }
        
        private RecordList<ProcessState> processStates;
		public RecordList<ProcessState> ProcessStates
        {
            get { 
				if ( processStates == null )
				{
					lazyLoadProperty<ProcessState>("ProcessStates");
				}
				return processStates; 
			}
            set { processStates = value; }
        }
        
        public ProcessState GetProcessState( Process p )
        {
        	foreach( ProcessState ps in ProcessStates )
        	{
        		if( ps.Process == p )
        			return ps;
        	}
        	return null;
        }

        public ProcessState AssociateProcess(Process p)
        {
        	if( GetProcessState(p) != null )
        		throw new Exception("A ProcessState for that process already exists.");
        
        	ProcessState ps = new ProcessState();
        	ps.Process = p;
			ps.Save();
			
			if( ps.Id > 800 )
			{
				string msg = string.Format( "How did we get a process state this high? (Note: depending on when this exception is seen, it may actually be a valid process state. (processstate id: {0}, translation id: {1})", ps.Id, Id);
				throw new Exception( msg );
			}
						
			ProcessStates.Add(ps);
			SaveRelations("ProcessStates");
			
			return ps;
        }
        
		public void InitializeTaskForProcess( ProcessState ps )
		{		
			//with the new opdeps, we only wnat to create operatoinstates for ops initially
			//that have no deps, and then, as ops are resolved, create them on demand, as needed.
			
			if( ps == null )
			{
				throw new Exception("Can't InitializeTaskForProcess without a valid ProcessState");
			}
			
			log.Debug( "initializing task for process ", ps.Process.Name );
			foreach( Operation o in ps.Process.Operations )
			{
				log.Debug("testing operation ", o.Name, o.DefaultState );
				OperationState os = new OperationState();
				os.ProcessState = ps;
				os.Task = this;
				os.Operation = o;
				os.State = o.DefaultState;
				
				if( o.Dependency == null || o.Dependency.IsDependencyResolved( ps, os, null, true ) )
				{
					//call base save to avoid testing for ready.
					os.Ready = true;
					os.Save(false);
					ps.Operations.Add(os);
				}
			}
			ps.SaveRelations("Operations");
		}
		
		public void UpdateReadyStatesForAllProccesses()
		{
			this.processStates.ForEach( a => UpdateReadyStates( a, null ) );
		}
		
		public void UpdateReadyStates(ProcessState ps, OperationState changedOperationState )
		{
			if( loading )
				return;
				
			
			if( ps == null )
			{
				throw new Exception("Can't update ready states without a valid ProcessState");
			}
			
			//reload opstates to ensure we have the latest states.
			ps.LoadChildList<OperationState>("Operations");
        	
        	/*
			Run through the operations, and see if any new operations have satisfied dependencies.
			*/
			
			bool changed = false;
		
			foreach( Operation o in ps.Process.Operations )
        	{
        		log.Debug("UpdateReadyStates, testing ", o.Name );
        		OperationState os = new OperationState();
        		os.ProcessState = ps;
				os.Task = this;
				os.Operation = o;
				os.State = o.DefaultState;
				
				if( o.Dependency != null && o.Dependency.IsDependencyResolved( ps, os, changedOperationState, true ) )
				{
					log.Debug("UpdateReadyStates DEP RESOLVED, ADDING OPSTATE ", o.Name );
					//call base save to avoid testing for ready.
					os.Ready = true;
					os.Save(false);
					ps.Operations.Add(os);
					changed = true;
				}
        	}
        	
        	foreach( OperationState os in ps.Operations )
        	{
        		if( os.Ready  && ( os.State != os.Operation.DefaultState ||
        			( os.Operation.Dependency != null && ! os.Operation.Dependency.IsDependencyResolved( ps, os, null, false ) ) ) )
        		{
        			//unready this task.
        			log.Debug("unreadying ", os.Operation, os.Operation.Dependency, os.State, os.Operation.DefaultState );        			
        			os.Ready = false;
        			os.Save(false);
        		}
        		else if( ! os.Ready && os.State == os.Operation.DefaultState && 
        			( os.Operation.Dependency == null || os.Operation.Dependency.IsDependencyResolved( ps, os, null, false ) ) )
        		{
        			log.Debug("readying");
        			os.Ready = true;
        			os.Save(false);
        		}
        	}
        	if( changed )
        		ps.SaveRelations("Operations");
		}
    }
}
