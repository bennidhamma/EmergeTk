// IOperationDependency.cs created with MonoDevelop
// User: ben at 7:02 P 14/05/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model.Workflow
{
	public class OperationDependency : AbstractRecord, ISingular
	{
		bool isNonDependency;
		
		public bool IsNonDependency {
			get {
				return isNonDependency;
			}
			set {
				isNonDependency = value;
				NotifyChanged("IsNonDependency");
				//save b/c we are a sub item in a modelform and there is no easy way to ensure a save event occurs otherwise.
				Save();
			}
		}
		
		public virtual bool IsDependencyResolved( 
			ProcessState ps, 
			OperationState os, 
		    OperationState changedOpState, 
		    bool satisfyDependency )
		{
			throw new NotImplementedException();
		}
		
		
		static List<string> operationDependencyTypes = new List<string>();
		public static List<string> GetSubTypeStrings()
		{
			if( operationDependencyTypes != null && operationDependencyTypes.Count > 0 )
			{
	        	return operationDependencyTypes;
	        }
	        else
	        {
	        	Type[] types = TypeLoader.GetTypesOfBaseType(typeof(OperationDependency));
	            operationDependencyTypes = new List<string>();
	            operationDependencyTypes.Add("--SELECT--");
	            foreach ( Type t in types )
	            	operationDependencyTypes.Add( t.FullName );
	            return operationDependencyTypes;
	        }
        }
        
        public virtual Widget GetEditWidget( Process process ) 
        {
        	LabeledWidget<SelectItem> nonDep = Context.Current.CreateWidget<LabeledWidget<SelectItem>>();
        	nonDep.LabelText = "Negative Dependency? ";        	
        	nonDep.Widget.Bind( this, "IsNonDependency");
        	nonDep.Init();
        	return nonDep;
        }
	}
}
