// RecordController.cs created with MonoDevelop
// User: ben at 9:36 P 23/05/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Reflection;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class RecordController : Generic
	{		
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(RecordController));
		IRecordList availableOptions;
		
		public override void Initialize ()
		{
			Type type = this.GetType();
			MethodInfo mi = type.GetMethod("Setup",BindingFlags.Instance|BindingFlags.NonPublic);
			
			if( this.Record != null )
			{
				mi.MakeGenericMethod(Record.GetType()).Invoke(this, new object[]{});
			}
			
			BindProperty("Record", new NotifyPropertyChanged( delegate() {
				if( ! setupComplete )
					mi.MakeGenericMethod(Record.GetType()).Invoke(this, new object[]{});				
			}) );
			
			//TODO: support Record being assigned after control is initialized.
		}
		
		bool setupComplete = false;
		
		IDataSourced ids;
		#pragma warning disable 169
		private void Setup<T>() where T : AbstractRecord, new()
		{
			if( setupComplete )
				return;
				
			log.Debug("calling Setup");
			ids = RecordSelect<T>.CreateSelector( availableOptions.Count );
			ids.DataSource = availableOptions;
			ids.DataBind();
			if( Record != null )
				ids.Selected = Record;
			Add( ids as Widget );
			Widget w = (Widget)ids;
			Bind( w );
			
			LinkButton lb = RootContext.CreateWidget<LinkButton>(this);
			lb.Label = "create new";
			lb.OnClick += delegate 
			{
				T newT = new T();
				Lightbox lightbox = RootContext.CreateWidget<Lightbox>(this);
				ModelForm<T> mf = RootContext.CreateWidget<ModelForm<T>>(lightbox, newT);
				mf.OnAfterSubmit += delegate {
					this.Record = mf.Record;
					lightbox.Remove();
					ids.DataSource.Add( mf.Record );
					ids.Selected = mf.Record;
				};
				mf.OnCancel += delegate { lightbox.Remove(); };
			};
			
			setupComplete = true;
		}
		#pragma warning restore 169
		
		public override object Value {
			get { return Record; }
			set { 
				Record = (AbstractRecord)value;
			}
		}
		
		public override string DefaultProperty {
			get { return "Record"; }
		}

		public IRecordList AvailableOptions {
			get {
				return availableOptions;
			}
			set {
				availableOptions = value;
			}
		}


	}
}
