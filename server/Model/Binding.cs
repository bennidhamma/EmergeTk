using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model
{
    public delegate void NotifyPropertyChanged();
    public class Binding
    {
    	private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Binding));			
        static Regex widgetRegEx = new Regex(@"\$(?<widget>\w+)\.?(?<property>#?\w+)?", RegexOptions.Compiled);
        private IDataBindable destination;

        public IDataBindable Destination
        {
            get { return destination; }
            set { destination = value; }
        }

        private IDataBindable source;

        public IDataBindable Source
        {
            get { return source; }
            set { source = value; }
        }

        private string destinationProperty;

        public string DestinationProperty
        {
            get { return destinationProperty; }
            set { destinationProperty = value; }
        }

        private string sourceProperty;

        public string SourceProperty
        {
            get { return sourceProperty; }
            set { sourceProperty = value; }
        }

        private GenericPropertyInfo sourceGpi, destGpi;
        private Type fieldType;
        private string sourcePropertySetValue;
        
        private bool destinationChanging = false, sourceChanging = false;

        private void DestinationChangedHandler()
        {       
        	if( sourceChanging || destinationChanging )
        		return;
        	object newDest = PropertyConverter.Convert(destGpi.Getter(destination), fieldType);
            object sourceVal = source[sourcePropertySetValue];
            if ((newDest == null && sourceVal == null) || (newDest != null && newDest.Equals(sourceVal)))
            {
            	return;
            }
        	destinationChanging = true;
            source[sourcePropertySetValue] = newDest;
            destinationChanging = false;
        }

        private void SourceChangedHandler()
        {        	
        	if( sourceChanging || destinationChanging )
        	{
        		return;	
        	}
        	//log.Debug("SourceChangedHandler -- BEGIN");
        	sourceChanging = true;
            UpdateDestination();
            sourceChanging = false;
            //log.Debug("SourceChangedHandler -- END");
        }
        
        // TODO: binding should support multiple depths of properties when binding

        public void UpdateDestination()
        {
        	if( destination == null )
        		return;
        	try
        	{
	            if (fieldRequiresFormatting)
	            {				
	                List<object> values = new List<object>();
	                foreach (string f in fields)
	                {
	                	string field = f;
						if( field.ToLower() == "$record" )
						{
							//#if DEBUG
							//	log.Debug( "binidng dest prop to ", source, source.GetType() ); 
							//#endif
		        	        destination[destinationProperty] = source;
		            	    return;
						}
	                    string valString = string.Empty;
	                    object oVal = null;
	                    bool invert = false;
	                    if( field.StartsWith( "!" ) )
	                    {
	                    	invert = true;
	                    	field = field.Substring(1);
	                    }
	                    	
	                    if (field.StartsWith("$"))
	                    {
	                        //syntax: {$textbox1.Text}
	                        Match m = widgetRegEx.Match(field);
	                        //log.Debug("Looking for ", field );
	                        if (m.Success)
	                        {
	                        	string widgetId = m.Groups["widget"].Value;
	                        	Widget w = null;
	                        	if( destination is Widget )
	                        	{
	                        		Widget dw = (Widget)destination;
	                        		w = dw.FindAncestor<Widget>(widgetId);
	                        	}
	                        	if( w == null )
	                            	w = Context.Current.Find(widgetId);
	                            if (w != null && m.Groups["property"].Success)
	                            {
	                                oVal = w[m.Groups["property"].Value];	                                
	                            }
	                            else
	                            {
	                                if (w is IDataBindable && w[(w as IDataBindable).DefaultProperty] != null)
	                                {
	                                    oVal = w[(w as IDataBindable).DefaultProperty];
	                                }
	                                else if( w != null )
	                                {
	                                    oVal = w.Id;
	                                }
	                                else
	                                {
	                                	log.Warn("Could not find successfully bind to widget", field, w );
	                                }
	                            }
	                        }
	                        else
	                        	log.Warn("Could not find successfully bind ", field );
	                    }
	                    else if( field.Contains( "(" ) )
	                    {
	                    	//this is a agg. function
	                    	Widget refWidget = null;
	                    	if( destination is EmergeTk.Widget )
	                    	{
	                    		refWidget = (Widget)destination;
	                    	}
	                    	else if( source is EmergeTk.Widget )
	                    	{
	                    		refWidget = (Widget)source;
	                    	}
	                    	
	                    	if( refWidget != null )
	                    	{
		                    	string[] parts = field.Split('(',')');
		                    	
		                    	string function = parts[0];
		                    	string column = parts[1];
		                    	oVal = Calc.Aggregate( refWidget, function, column );
		                    }
	                    }
	                    else if (field == "ObjectId" && source != null)
	                        oVal = source.ObjectId; 
	                    else if (source != null)
	                    {
	                    	string fieldKey = field;
							
	                    	if( field.Contains(".") )
	    	                {
	    	                	bool isCalc = false;
	    	                	IDataBindable tmpSource = source;
	        	    			string[] parts = field.Split('.');
	        	    			for( int i = 0; i < parts.Length - 1; i++ )
	        	    			{
	        	    				if( tmpSource[parts[i]] is IDataBindable )
	        	    					tmpSource = tmpSource[parts[i]] as IDataBindable;
	        	    				else if( tmpSource[parts[i]] is IRecordList )
	        	    				{
	        	    					oVal = Calc.CalculateU( tmpSource[parts[i]] as IRecordList , parts[i+2], parts[i+1]);
	        	    					isCalc = true;
	        	    					break;
	        	    				}
	        	    				else
	        	    				{
	        	    					//log.Debug(string.Format("Binding:UpdateDestination: {0} -- {1} -- {2} does not chain down IDataBindable sources.", 
	        	    					//	tmpSource, tmpSource.GetType(), field ) );
	        	    					return;
	        	    				}
	        	    			}
	        	    			if( ! isCalc )
	        	    			{
	        	    				fieldKey = parts[parts.Length - 1];
	        	    				oVal = tmpSource[ fieldKey ];
	        	    			}
	            	        }
	            	        else
	            	        {
	            	        	oVal = source[fieldKey];
	            	        }

	            	        if( destGpi.PropertyInfo != null &&
							   /*sVal != null &&*/
                               destGpi.PropertyInfo.PropertyType.IsInstanceOfType( oVal ) &&
							   destGpi.PropertyInfo.PropertyType != typeof(string) &&
							   ! sourceProperty.StartsWith("=")  &&
							   ! invert )
							{
								destination[destinationProperty] = oVal;
								return;
							}
	                    }						

						if( formatStrings != null && formatStrings.ContainsKey(field) )
	                    {
							string fmt = formatStrings[field];							
							switch( fmt )
							{
							case "ws2dash":
							case "css":
							    valString = oVal.ToString().ToLower().Replace(" ", "-");
							    break;
							    
							case "p2h":
								valString = Util.PascalToHuman( oVal.ToString() );
								break;							
							case "bool":
								bool b = false;
								
								if(oVal is String || oVal is int || oVal is float || oVal is decimal || oVal is double )
								{
									b = Convert.ToBoolean(oVal);
								}
								else
									b = oVal != null;
								destination[destinationProperty] = b;
								return;
							default:
	                    		valString = string.Format( "{0:" + formatStrings[field] + "}",  oVal );
	                    		break;
	                    	}
	                    	//log.Debug("formatting oval with ", formatStrings[field], oVal, oVal.GetType(), valString );
	                    }
	                    else if( oVal is DateTime )
						{
							DateTime d = (DateTime)oVal;
							if ( d.Ticks > 0 )
                        		valString = ((DateTime)oVal).ToShortDateString();
							else
								valString = "";														
                        }
                        
                        else if( invert )
                        {
							valString = (! Convert.ToBoolean( oVal )).ToString();
						}	
	                    else
	                    {
	                    	valString = Convert.ToString( oVal );
	                    }
						
	                    values.Add(valString);
	                }
	                bool doEval = sourceProperty.StartsWith("=");
	                string value = string.Format(sourceProperty, values.ToArray());
	                if (doEval)
	                {
	                    object result = Util.Eval(value.TrimStart('='));
	                    if (result != null)
	                        value = result.ToString();
	                    else
	                        value = string.Empty;
	                }
	                
	                //Debug.Trace("Bind setting {0} to {1}, destination: {2}({3})", destinationProperty, value, destination, destination.GetType());
	                if (destination is Widget)
	                {
	                	//log.Error("setting destination widget 1 ", value, destination );
                		destination[destinationProperty] = value;
	                	
	                    //(destination as Widget).SetAttribute(destinationProperty, value,true);
	                }
	                else
	                {
	                    string oldString = destGpi.Getter(destination) as string;
	                    if (oldString != value)
	                    {
							destGpi.Setter(destination,value);
	                    }
	                }
	            }
	            else
	            {	            	
	                object newValue = null;
	                object oldValue = null;
	                string sourceField = sourceProperty;
	                bool invert = false;
	                if( source != null )
	                {
		                if( sourceField.StartsWith( "!" ) )
	                    {
	                    	invert = true;
	                    	sourceField = sourceField.Substring(1);
	                    }
	                	if( destGpi.PropertyInfo != null )
	                	{
	                		newValue = PropertyConverter.Convert(source[sourceField], destGpi.PropertyInfo.PropertyType);
	                	}
	                	else
	                		newValue = source[sourceField];
	                		
	                	//log.DebugFormat("updating destination {0} with source field {1} value {2}", destinationProperty, sourceField, newValue );
	                	
	                	if( invert )
	                		newValue = ! Convert.ToBoolean(newValue );
	                }
	                
	                if( destGpi.PropertyInfo != null )
                		oldValue = destGpi.Getter(destination);
                    
                    if ( newValue == null || ! newValue.Equals(oldValue))
                    {
                    	if( destination is Widget && newValue is string )
                    	{
                    		//#435 -- fix curly braces in target urls.
                    		Widget destWidget = (Widget)destination;
                    		destWidget.SetAttribute(destinationProperty, newValue as string, false, true );
                    	}
                    	else
                    		destination[ destinationProperty ] = newValue;
                    }
	            }
	        }
	        catch( Exception e )
	        {
	        	log.Error("Eror during databind of binding", this, Util.BuildExceptionOutput(e));
	        	throw new Exception("Error dudring databind", e );
	        }
        }

        public NotifyPropertyChanged OnDestinationChanged, OnSourceChanged;
        private List<string> fields = null;
        public List<string> Fields { get { return fields; } set { fields = value; source.Unbind(this); source.Bind(this); } }
        bool fieldRequiresFormatting = false;
        
        
        public GenericPropertyInfo SourcePropertyInfo
        {
        	get {
        		if( sourceGpi.PropertyInfo == null )
        		{
        			string sourcePropertySetValue = this.sourceProperty.Trim(new char[] { '}', '{' });
            		sourceGpi =  TypeLoader.GetGenericPropertyInfo(source,sourcePropertySetValue);
            	}
            	return sourceGpi;
        	}
        }
        
        System.Collections.Generic.Dictionary<string,string> formatStrings;

        public Binding(IDataBindable destParam, string destPropParam, IDataBindable sourceParam, string sourcePropParam):
        	this(destParam,destPropParam,sourceParam,sourcePropParam, false )
        {
        	
        }

		static Regex regBindingFormat = new Regex(@"(?<!{)\{(?<Name>[^{}]+)\}(?!})", RegexOptions.Compiled);
        public Binding(IDataBindable destParam, string destPropParam, IDataBindable sourceParam, string sourcePropParam, bool oneWay )
        {
        	if( destParam == null )
        	{
        		throw new ArgumentNullException("destParam");
        	}
        	this.destination = destParam;
            this.destinationProperty = destPropParam;
            this.source = sourceParam;
            this.sourceProperty = sourcePropParam;
            OnSourceChanged = new NotifyPropertyChanged(SourceChangedHandler);

            if (sourcePropParam.Contains("{"))
            {
                fieldRequiresFormatting = true;
                fields = new List<string>();
                //parse for formatted string.

                this.sourceProperty = regBindingFormat.Replace(sourcePropParam, new MatchEvaluator(delegate(Match m) 
                { 
                	string match = m.Groups[1].Value;
                	if( match.Contains(":") )
                	{
                		string[] parts = match.Split(new char[]{':'},2);
                		match = parts[0];
                		if( formatStrings == null )
                			formatStrings = new Dictionary<string,string>();
                		formatStrings[ match ] = parts[1];
                	}
                	fields.Add(match); 
                	return "{" + (fields.Count - 1).ToString() + "}"; 
                }));
                if (fields.Count > 0)
                {                	
                    foreach (string f in fields)
                    {
                    	//log.Debug("binding field ", source.GetType(), f);
                        if (f.StartsWith("$") && f.ToLower() != "$record" )
                        {
                            Match m = widgetRegEx.Match(f);
                            if (m.Success)
                            {
                                Widget s = Context.Current.Find(m.Groups["widget"].Value);
                                string propertyName = null;
                                if( m.Groups["property"].Success )
                                {
                                    propertyName = m.Groups["property"].Value;
                                }
                                else if ( s is IDataBindable )
                                {
                                    propertyName = (s as IDataBindable).DefaultProperty;
                                }
                                //log.Debug("binding to ", s, m.Groups["widget"].Value, propertyName );
                                if( propertyName != null && s != null )
                                    s.BindProperty(propertyName, OnSourceChanged);
                            }
                        }
                    }
                }
                
            }

            sourcePropertySetValue = sourcePropParam.Trim(new char[] { '}', '{' });
            if( source != null )
            	sourceGpi = TypeLoader.GetGenericPropertyInfo(source, sourcePropertySetValue);

            if ((fields == null || (fields.Count == 1 && sourcePropParam.StartsWith("{")
                        && sourcePropParam.EndsWith("}"))) &&
                        destination is IDataBindable && !oneWay && source != null)
            {
                //do not allow two way binding on formatted bindings.
                if (sourceGpi.PropertyInfo != null && sourceGpi.PropertyInfo.CanWrite )
                {
                    fieldType = source.GetFieldTypeFromName(sourcePropertySetValue);
                    if( fieldType != null )
                    	OnDestinationChanged = new NotifyPropertyChanged(DestinationChangedHandler);
                }
            }

			if( source is AbstractRecord )
            {
            	source.Bind( this );
            }
			destGpi = TypeLoader.GetGenericPropertyInfo(destination,destPropParam);
        }
        
        public override string ToString ()
        {
        	string s = "binding { ";
        	try
        	{
        	
        		if( fields != null )
        		{
        			s += "fields: ";
	        		s += Util.Join( fields, ",\n" );
	        	}
		        s += string.Format( "\n destination: " + (destination != null ? destination.ToString() + " (" + destination.GetType() + ")" : "NULL" ) );
	        	s += string.Format( "\n destinationProperty: " + destinationProperty );
	        	s += "\n source: " + source;
	        	s += "\n sourcePro: " + ( this.sourceProperty != null ? this.sourceProperty : "" ); 
        	}
        	catch(Exception e)
        	{
        		s += " ERR " + e.Message;
        	}
        	return  s + "}\n" ; 
        }

    }
}
