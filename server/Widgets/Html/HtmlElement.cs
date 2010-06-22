using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Widgets.Html
{
    public class HtmlElement : Widget
    {
        private string innerHtml = "";

    	private string defaultTagPrefix;
    	
        private string tagName = "div";
		
        public string TagName
        {
            get { return tagName; }
            set { tagName = value; 
            	SetClientAttribute("tn", Util.ToJavaScriptString(value)); 
            	RaisePropertyChangedNotification("TagName");	
            }
        }
        
        public HtmlElement()
        {
        	 ClientArguments["tn"] = "'div'";
       	}
       	
       	public override Dictionary<string,object> Serialize (Dictionary<string,object> h)
       	{
       		foreach( string k in ElementArguments.Keys )
       			h[k] = ElementArguments[k];
       		return base.Serialize (h);
       	}

        public override string ClientClass { get { return "he"; } }
        
        public override string ClientIdBase { get { return tagName ?? ""; } }

        public virtual string DefaultTagPrefix {
        	get {
        		if( defaultTagPrefix == null )
        			defaultTagPrefix = TagPrefix;
        		return defaultTagPrefix;
        	}
        	set {
        		defaultTagPrefix = value;
        	}
        }

        public override bool SetAttribute(string Name, string Value)
        {
        	if( Name.StartsWith("xmlns") )
        		return false;
        	
        	if( ( TagPrefix != null && Name.StartsWith( TagPrefix ) ) || DefaultTagPrefix == TagPrefix )
        	{
        		return base.SetAttribute( Name.Replace(TagPrefix + ":",""), Value );        		
        	}
        	InitDataBoundAttribute(Name, Value);
            if (Name == "Id")
                Id = Value.Trim('\'');
            else 
            	Value = Util.ToJavaScriptString(Value);
           
            SetClientElementAttribute(Name, Value);

			return true;
        }
        
        public override string ToDebugString()
        {
        	return JSON.Default.HashToJSON(ClientArguments);
        }
        
        public static HtmlElement Create( string tag )
        {
        	if( Context.Current == null ) 
        		return null;
        	HtmlElement he = Context.Current.CreateWidget<HtmlElement>();
        	he.TagName = tag;
        	return he;
        }

        public string InnerHtml
        {
            get {
				return innerHtml;
			}
            set
            {
			    string s = Util.ToJavaScriptString(value);
                if (s != this.innerHtml)
                {
                    this.SetClientElementProperty("innerHTML", s);
                }
            }
        }
    }
}
