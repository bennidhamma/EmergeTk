// Autocomplete.cs created with MonoDevelop
// User: ben at 1:21 PM 7/3/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Widgets.Html
{
	public class Autocomplete : Widget, IDataSourced
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Autocomplete));
		public Autocomplete()
		{
			DefaultProperty = "SelectedItem";
		}
		
		public EventHandler OnData;
		
		AbstractRecord selectedItem;
		string propertyKey;
		
		public AbstractRecord SelectedItem {
			get {
				return selectedItem;
			}
			set {
				#if DEBUG
					log.Debug( "SETTING SelectedItem", selectedItem, value ); 
				#endif
				selectedItem = value;
				
				if( value != null )
				{
					if( rendered )
					{
						InvokeClientMethod( "SetValue", value.Id.ToString() );
					}
					else
					{
						SetClientProperty( "value", value.Id.ToString()  );
					}
				}
				RaisePropertyChangedNotification("SelectedItem"); 
			}
		}

		IRecordList dataSource;
		public EmergeTk.Model.IRecordList DataSource {
			get {
				return dataSource;
			}
			set {
				dataSource = value;
			}
		}
		
		bool isDataBound = false;
		public bool IsDataBound {
			get {
				return isDataBound;
			}
			set {
				isDataBound = value;
			}
		}

		string propertySource;
		public string PropertySource {
			get {
				return propertySource;
			}
			set {
				propertySource = value;
			}
		}

		public string PropertyKey {
			get {
				return propertyKey;
			}
			set {
				propertyKey = value;
			}
		}
		
		public override void HandleEvents (string evt, string args)
		{
			#if DEBUG
				log.Debug( evt, args, dataSource.Count, propertyKey ); 
			#endif
			if( evt == "OnData" )
			{
				if( OnData != null )
				{
					OnData( this, EventArgs.Empty );
				}
				
				if( dataSource != null )
				{
					IRecordList filtered = dataSource;
					if( args != "*" )
						filtered = dataSource.Filter( new FilterInfo( propertyKey ?? "Value", args.Trim('*'), FilterOperation.Contains ));
					filtered.Sort( new SortInfo( propertyKey ?? "Value" ) );
					
					Dictionary<string,object> result = new Dictionary<string,object>();
					result["numRows"] = filtered.Count;
					result["identity"] = "id";
					
					List< Dictionary<string,object> > items = new List<Dictionary<string,object>>();
					
					foreach( AbstractRecord r in filtered )
					{
						Dictionary<string,object> item = new Dictionary<string,object>();
						item["id"] = r.Id;
						item["name"] = r[propertyKey ?? "Value" ].ToString();
						items.Add( item );
					}
					result["items"] = items;
					
					RootContext.HttpContext.Response.Write(
						JSON.Default.Encode( result ) );
					
				}
			}
			if( evt == "OnDataId" )
			{
				if( OnData != null )
				{
					OnData( this, EventArgs.Empty );
				}
				
				int testId;
				if( dataSource != null && int.TryParse(args, out testId) )
				{
					IRecordList filtered = dataSource;
					AbstractRecord r =  null;
					foreach( AbstractRecord r2 in dataSource )
					{
						if( r2.Id == testId )
						{
							r = r2;
							break;
						}
					}
					
					if( r == null )
					{
						log.Error( "Could not find ID in list", args );
						return;
					}
					
					Dictionary<string,object> result = new Dictionary<string,object>();
					result["numRows"] = filtered.Count;
					result["identity"] = "id";
					
					List< Dictionary<string,object> > items = new List<Dictionary<string,object>>();
					
					Dictionary<string,object> item = new Dictionary<string,object>();
					item["id"] = r.Id;
					item["name"] = r[propertyKey ?? "Value" ].ToString();
					items.Add( item );
				
					result["items"] = items;
					
					RootContext.HttpContext.Response.Write(
						JSON.Default.Encode( result ) );
					
				}
			}
			else if( evt == "OnChange" )
			{
				int id = int.Parse(args);
				foreach( AbstractRecord r in dataSource )
				{
					if( r.Id == id )
					{
						this.InvokeChangedEvent( SelectedItem, r );
						SelectedItem = r;
					}
				}
			}
			else
				base.HandleEvents (evt, args);
		}
		
		public override object Value {
			get { return SelectedItem; }
			set { 
				SelectedItem = ( AbstractRecord ) value;
				RaisePropertyChangedNotification("Value");
			  }
		}

		public AbstractRecord Selected {
			get {
				return SelectedItem;
			}
			set {
				SelectedItem = value;
			}
		}


		public void DataBind ()
		{
			//don't think we need to do anything.
		}

		
	}
}
