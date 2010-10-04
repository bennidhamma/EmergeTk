using System;
using System.Collections.Generic;
using System.Reflection;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model
{
    public enum FieldLayout
    {
        Terse,
        Spacious
    }
	
	public class DataTypeFieldBuilder
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(DataTypeFieldBuilder));
		private DataTypeFieldBuilder()
		{
		}

        public static Widget GetViewWidget(ColumnInfo fi)
        {
            Widget l = Context.Current.CreateWidget<Label>();
            l.Id = fi.Name;
            return l;
        }
        
        public static Widget GetEditWidget(ColumnInfo fi, FieldLayout layout)
        {
        	return GetEditWidget( fi, layout, null, DataType.None );
        }

        public static Widget GetEditWidget(ColumnInfo fi, FieldLayout layout, IRecordList records, DataType hint)
		{
			if( fi.Type.Name.Contains("RecordList") )
				return null;
            
            Widget propWidget = null;
            if( hint == DataType.None )
            	hint = fi.DataType;
			
			switch( hint )
			{
                case DataType.SmallText:
                    TextBox smallTexBox = Context.Current.CreateWidget<TextBox>();
					if( ! string.IsNullOrEmpty( fi.HelpText ) )
					{
						smallTexBox.HelpText = fi.HelpText;
					}
                    smallTexBox.Columns = 20;
                    propWidget = smallTexBox;
                    break;
                case DataType.Xml:
                    TextBox xmlTextBox = Context.Current.CreateWidget<TextBox>();
					if( ! string.IsNullOrEmpty( fi.HelpText ) )
					{
						xmlTextBox.HelpText = fi.HelpText;
					}
                    xmlTextBox.Rows = 10;
                    xmlTextBox.Columns = 40;
                    xmlTextBox.ClassName = "xmlTextBox";
                    propWidget = xmlTextBox;
                    break;
                case DataType.LargeText:
                    TextBox largeTextBox = Context.Current.CreateWidget<TextBox>();
					if( ! string.IsNullOrEmpty( fi.HelpText ) )
					{
						largeTextBox.HelpText = fi.HelpText;
					}
                    largeTextBox.Rows = 10;
                    largeTextBox.Columns = 40;
                    //largeTextBox.IsRich = true;
                    if( layout == FieldLayout.Terse )
                        largeTextBox.ClassName = "largeGridEditTextBox";
                    propWidget = largeTextBox;
                    break;
                case DataType.RecordSelect:
					propWidget = (Widget)TypeLoader.InvokeGenericMethod( typeof(DataTypeFieldBuilder), "GetRecordSelect", new Type[]{fi.Type},null,new object[]{fi,records});
                    break;
                case DataType.RecordSelectOrCreate:
					propWidget = (Widget)TypeLoader.InvokeGenericMethod( typeof(DataTypeFieldBuilder), "GetSelectOrCreate", new Type[]{fi.Type},null,new object[]{fi,records});                  
                	break;
                case DataType.ReadOnly:
                	propWidget = Context.Current.CreateWidget<Label>();
                	break;
			}
				
			if( propWidget == null )
			{
				if( fi.Type == typeof(DateTime) )
				{
					propWidget = Context.Current.CreateWidget<DatePicker>();
				}
                else if (fi.Type.IsEnum)
                {
                    DropDown dd = Context.Current.CreateWidget<DropDown>();
                    List<string> ids = new List<string>(Enum.GetNames(fi.Type));
					List<string> options = new List<string>();
                    object[] attributes = fi.Type.GetCustomAttributes(typeof(FriendlyNameAttribute),false );
                    if( attributes != null && attributes.Length > 0 )
                    {
                    	FriendlyNameAttribute fa = attributes[0] as FriendlyNameAttribute;	
                    	for( int i = 0; i < fa.FieldNames.Length; i++ )
                    		options.Add(fa.FieldNames[i] ); 	
                    }
                    else for( int i = 0; i < ids.Count; i++ )
                    	options.Add( Util.PascalToHuman( ids[i] ) );
					dd.Ids = ids;
                    dd.Options = options;
                    dd.DefaultProperty = "SelectedId";
                    propWidget = dd;
                }
                else if( fi.Type == typeof(bool) )
                {
                	SelectItem si = Context.Current.CreateWidget<SelectItem>();
                	si.Mode = SelectionMode.Multiple;
                	propWidget = si;                	
                }
                else if( fi.Type == typeof(int) || fi.Type == typeof(float) )
                {
                	TextBox tb = Context.Current.CreateWidget<TextBox>();
					if( ! string.IsNullOrEmpty( fi.HelpText ) )
					{
						tb.HelpText = fi.HelpText;
					}
                    if (layout == FieldLayout.Spacious) tb.Columns = 5;
                    propWidget = tb;
                }
                else if( fi.Type.IsSubclassOf(typeof(AbstractRecord)) )
                {
					propWidget = (Widget)TypeLoader.InvokeGenericMethod( typeof(DataTypeFieldBuilder), "GetModelForm", new Type[]{fi.Type},null,new object[]{fi,records});
                }
                else
                {
                    TextBox tb = Context.Current.CreateWidget<TextBox>();
					if( ! string.IsNullOrEmpty( fi.HelpText ) )
					{						
						tb.HelpText = fi.HelpText;
					}
                    propWidget = tb;
                }
			}
			propWidget.Id = fi.Name;
			string helpText = null;
            if (layout == FieldLayout.Spacious)
            { 
                LabeledWidget<Widget> lc = Context.Current.CreateWidget<LabeledWidget<Widget>>();
                lc.LabelText = Util.PascalToHuman(fi.Name);
                if( fi.HelpText != null )
                {
                	Label help = Context.Current.CreateWidget<Label>();
                	help.Text = helpText;
                	help.ClassName = "editfield-helptext";
                	lc.Label.Add(help);
                }
                lc.Widget = propWidget;
                propWidget = lc;
            }
			return propWidget;
		}
		
		public static Widget GetModelForm<T>(Model.ColumnInfo fi, IRecordList records ) where T : AbstractRecord, new()
        {
        	ModelForm<T> mf = Context.Current.CreateWidget<ModelForm<T>>();
			mf.BindsTo = typeof(T);
        	mf.DestructivelyEdit = true;
        	mf.ShowCancelButton = false;
        	mf.ShowDeleteButton = false;
        	mf.ShowSaveButton = false;
            return mf;
        }
		
		
		public static Widget GetSelectOrCreate<T>(Model.ColumnInfo fi, IRecordList records ) where T : AbstractRecord, new()
		{
			RecordController rc = Context.Current.CreateWidget<RecordController>();
			rc.BindsTo = typeof(T);
			if( records == null )
            	records = DataProvider.Factory.GetProvider(typeof(T)).Load<T>();
            #if DEBUG
            	log.Debug( "setting records to RecordController ", records, records.Count ); 
            #endif
			rc.AvailableOptions = records;
			return rc;
		}

        public static Widget GetRecordSelect<T>(Model.ColumnInfo fi, IRecordList records ) where T : AbstractRecord, new()
        {
        	IDataSourced dd;
        	
            //Autocomplete dd = Context.Current.CreateWidget<Autocomplete>();
            if( records == null )
            	records =  DataProvider.Factory.GetProvider(typeof(T)).Load<T>();
			
           	dd = RecordSelect<T>.CreateSelector( records.Count );
            dd.DataSource = records as IRecordList<T>;
            dd.DataBind();
            return (Widget)dd;
        }
	}
}
