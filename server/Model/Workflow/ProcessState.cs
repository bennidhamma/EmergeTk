using System;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Model.Workflow;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model.Workflow
{
	public class ProcessState : AbstractRecord
	{
		Process process;
		RecordList<OperationState> operations;
		
		public Process Process {
			get {
				return process;
			}
			set {
				process = value;
			}
		}

		public RecordList<OperationState> Operations {
			get {
				if( operations == null )
					lazyLoadProperty<OperationState>("Operations");
				return operations;
			}
			set {
				operations = value;
			}
		}
	}
}