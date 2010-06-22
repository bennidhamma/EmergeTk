// OperationDependencySet.cs created with MonoDevelop
// User: ben at 7:02 P 14/05/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using EmergeTk;
using EmergeTk.Administration;
using EmergeTk.Model;
using EmergeTk.Model.Workflow;
using EmergeTk.Widgets;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model.Workflow
{
	public enum JoinOperator {
		And,
		Or
	}
	
	public class OperationDependencySet : OperationDependency
	{
		JoinOperator joinOperator;		
		public JoinOperator JoinOperator {
			get {
				return joinOperator;
			}
			set {
				joinOperator = value;
				NotifyChanged("JoinOperator");
				Save();
			}
		}

		RecordList<OperationDependency> dependencies;
		public EmergeTk.Model.RecordList<OperationDependency> Dependencies {
			get {
				if( dependencies == null )
				{
					lazyLoadProperty<OperationDependency>("Dependencies");
				}
				return dependencies;
			}
			set {
				dependencies = value;
			}
		}	
		
		public OperationDependencySet()
		{
		}
		
		public override bool IsDependencyResolved (
			ProcessState ps, 
			OperationState os, 
			OperationState changedOpState,
			bool satisfyDependency)
		{
			bool resolved = false;
			bool anyAreFalse = false;
			foreach( OperationDependency opdep in Dependencies )
			{
				bool result = opdep.IsDependencyResolved(ps, os, changedOpState, satisfyDependency);
				if( result && joinOperator == JoinOperator.Or )
				{
					resolved = true;
					break;
				}
				else if( ! result && joinOperator == JoinOperator.And )
				{
					anyAreFalse = true;
					break;
				}
			}
			
			if( ( joinOperator == JoinOperator.And && ! anyAreFalse ) || 
				( joinOperator == JoinOperator.Or && resolved ) )
			{
				return ! IsNonDependency;
			}
			
			return IsNonDependency;
		}

		private Generic editWidget;
		private Process process;
		public override Widget GetEditWidget (Process process)
		{
			this.process = process;
			editWidget = Context.Current.CreateWidget<Generic>();
			editWidget.AppendClass("OperationDependencySet");
			
			Label head = Context.Current.CreateWidget<Label>(editWidget);
			head.TagName = "h4";
			head.Text = "Dependency Set";

			Generic existingDepsContainer = Context.Current.CreateWidget<Generic>(editWidget);
			existingDepsContainer.TagName = "ul";
			foreach( OperationDependency opdep in Dependencies )
			{
				existingDepsContainer.Add( SetupOpDepItem( opdep ) );
			}
			
			SetupNewDepEd();
			
			dependencies.OnRecordAdded += new EventHandler<RecordEventArgs>( delegate( object sender, RecordEventArgs ea ) {
				existingDepsContainer.Add( SetupOpDepItem( (OperationDependency)ea.Record) );
				SetupNewDepEd();
				SaveRelations("Dependencies");
			});			
			
			LabeledWidget<EnumDropDown<JoinOperator>> joinDD = Context.Current.CreateWidget<LabeledWidget<EnumDropDown<JoinOperator>>>(editWidget);
			joinDD.LabelText = "Join Operator";
			joinDD.Widget.Bind(this, "JoinOperator");
			Widget baseWidget = base.GetEditWidget(process);
			log.Debug( "what is base widget?", baseWidget );
			editWidget.Add( baseWidget );
			
			return editWidget;
		}
		
		DependencyEditor newDepEd;
		private void SetupNewDepEd()
		{
			//Widget replace = null;
			if( newDepEd != null )
				newDepEd.Remove();
			newDepEd = Context.Current.CreateWidget<DependencyEditor>();
			//if( replace != null )
			//	replace.Replace( newDepEd );
			//else
			editWidget.Add( newDepEd );
			newDepEd.Process = process;
			newDepEd.Init();
			newDepEd.OnAdded += new EventHandler( delegate( object o, EventArgs ea ) {
				dependencies.Add( newDepEd.Dependency );
			});
		}

		private DependencyEditor SetupOpDepItem( OperationDependency opdep )
		{
			DependencyEditor depEd = Context.Current.CreateWidget<DependencyEditor>();
			depEd.Process = process;
			depEd.TagName = "li";
			depEd.Dependency = opdep;
			depEd.OnRemoved += new EventHandler( delegate( object sender, EventArgs ea ) {
				DependencyEditor d = (DependencyEditor)sender;
				dependencies.Remove( d.Dependency );
				d.Remove();
				SaveRelations("Dependencies", dependencies, false);
			});
			depEd.Init();
			return depEd;
		}
	}
}
