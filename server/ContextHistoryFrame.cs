using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk
{
    public delegate void ContextHistoryHandler(object stateId);

    public class ContextHistoryFrame
    {
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ContextHistoryFrame));		
		
        public ContextHistoryHandler CallBack;
        public object State;
        public string Id;

        public ContextHistoryFrame(ContextHistoryHandler CallBack, object State, string Id)
        {
            this.CallBack = CallBack;
            this.State = State;
            this.Id = Id;
        }
    }
}
