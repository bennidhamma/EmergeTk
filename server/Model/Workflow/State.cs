using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model.Workflow
{
    public class State : AbstractRecord, ISingular
    {
        private string name;
        public string Name
        {
            get {
				return name;
			}
            set { name = value; }
        }
		
		public override string ToString()
		{
			return Name;
		}
        
        public static State GetState( string name )
        {
        	return EmergeTk.Model.Workflow.State.Load<State>(new FilterInfo("Name",name));
        }
    }
}
