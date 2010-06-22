using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;

namespace EmergeTk
{
    public class Widget : ICloneable, IDataBindable, IJSONSerializable
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Widget));		
		
		static Widget()
		{
			//note: do not use upper-case functionnames, or lower case a-f as these can be used as
			//widget ids.
			clientClassNameMap["Literal"] = "l";
			clientClassNameMap["Comet"] = "co";
			clientClassNameMap["Pane"] = "p";
			clientClassNameMap["Poller"] = "po";
			clientClassNameMap["PlaceHolder"] = "ph";
			clientClassNameMap["DropDown"] = "dr";
			clientClassNameMap["EnumDropDown"] = "dr";
			clientClassNameMap["TabPane"] = "etk_t";
			clientClassNameMap["DatePicker"] = "dp";
			clientClassNameMap["HoverBox"] = "h";
			clientClassNameMap["Label"] = "la";
			clientClassNameMap["SelectItem"] = "si";
			clientClassNameMap["TextBox"] = "tb";
			clientClassNameMap["NumberSpinner"] = "tb";
			clientClassNameMap["Link"] = "li";
			clientClassNameMap["LinkButton"] = "lb";
			clientClassNameMap["TreeNode"] = "tnode";
			clientClassNameMap["Button"] = "bu";
			clientClassNameMap["ImageButton"] = "ib";
			clientClassNameMap["emergetk.Image"] = "etk_i";
		}
		
		static Dictionary<string,string> clientClassNameMap = new Dictionary<string,string>();
		
        public Widget() { if( this.GetType() == typeof( Widget ) ) throw new System.NotSupportedException("Cannot directly instantiate widgets.  Widget would be abstract, but for the lack of variance in C#."); }
		
		public virtual void Unregister()
		{
			if( this.widgets != null )
				foreach( Widget w in this.widgets )
					w.Unregister();
		}
		
		private string name;
		public string Name
		{
			get { 
				if( name == null )
				{
					name = this.GetType().FullName;
					//name = name.Replace(".",string.Empty);
					if( name.Contains("`") )
					{
		                name = name.Substring(0, name.IndexOf('`'));
		                foreach( Type t in GetType().GetGenericArguments() )
		                {
		                    name += t.Name;
		                }
					}					
				}
				return name;  }
			set { name = value; }
		}
       	string id;

		private string className = null;
		private bool visible = true;
		public bool rendered = false;
        protected string OverrideBaseElement;
		protected Widget parent;
		private Context root;
		public int position = -1;
		string tagPrefix = "emg";
		bool enabled = true;
		bool debugging = false;
		Permission permission;
        private Permission visibleTo, notVisibleTo;
		
		private bool initialized = false;
		
		public bool Initialized { get { return initialized; } }

        private Dictionary<string,object> stateBag;
        public Dictionary<string,object> StateBag
        {
            get { if (stateBag == null) stateBag = new Dictionary<string, object>();  return stateBag; }
        }

        private string clientClass;
        public virtual string ClientClass
        {
            set
            {
                clientClass = value;
            }
            get
            {
                if (clientClass == null)
                {
                    string typeName = this.GetType().FullName;
                    typeName = typeName.Replace("EmergeTk.Widgets.Html", "");
                    typeName = typeName.Replace("EmergeTk.Widgets", "");
                    typeName = typeName.Replace("EmergeTk", "");
                    if( RootContext.DefaultNamespace != string.Empty )
                    	typeName = typeName.Replace(RootContext.DefaultNamespace, "");
                    typeName = typeName.Replace(".", "");
                    
                    clientClass = typeName;
                    int tildeIndex = clientClass.IndexOf('`');
                    if (tildeIndex > 0)
                    {
                        clientClass = clientClass.Substring(0, tildeIndex);
                    }
                }
                return clientClass;
            }
        }

        private string appearEffect;

        public string AppearEffect
        {
            get { return appearEffect; }
            set { appearEffect = value; ClientArguments["appearEffect"] = "'" + Util.FormatForClient( value ) + "'"; }
        }

        private Dictionary<string, Model.NotifyPropertyChanged> notifyPropertyChangedHandlers;
        public Dictionary<string, Model.NotifyPropertyChanged> NotifyPropertyChangedHandlers
        {
            get
            {
                return notifyPropertyChangedHandlers;
            }
        }
        
        protected void alert(string msg)
        {
        	RootContext.Alert(msg);
        }
        
        protected void alert(string format, params string[] args)
        {
        	RootContext.Alert(string.Format(format,args));
        }
        
        virtual protected void RaisePropertyChangedNotification(string property)
        {
        	if( ! setProperties.ContainsKey( property ) )
			{
        	  setProperties[ property ] = "";
			}
            if (notifyPropertyChangedHandlers != null && notifyPropertyChangedHandlers.ContainsKey(property))
            {
            	notifyPropertyChangedHandlers[property]();
            }
        }
        
        private List<Binding> bindings;
        public List<Binding> Bindings { get { return bindings; } }
        public Binding Bind(string property, IDataBindable source, string field)
        {
            Binding b = new Binding(this, property, source, field);
            b.UpdateDestination();
            Bind(b);
            return b;
        }

        public void BindProperty( string name, NotifyPropertyChanged del )
        {
            if (notifyPropertyChangedHandlers == null)
                    notifyPropertyChangedHandlers = new Dictionary<string, Model.NotifyPropertyChanged>();
            if (NotifyPropertyChangedHandlers.ContainsKey(name))
                NotifyPropertyChangedHandlers[name] += del;
            else
                NotifyPropertyChangedHandlers[name] = del;
        }

        public void Bind(Binding b)
        {
            if (this is IDataBindable)
            {
                if( b.OnDestinationChanged != null )
                    BindProperty(b.DestinationProperty,b.OnDestinationChanged);
                if (b.Source is Widget)
                {
                    Widget source = b.Source as Widget;
                    source.BindProperty(b.SourceProperty,b.OnSourceChanged);
                }
            }
            if (bindings == null) bindings = new List<Binding>();
            bindings.Add(b);
        }

        /// <summary>
        /// Binds to default property, if it's available.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="field"></param>
        public Binding Bind(IDataBindable source, string field)
        {
            if (this is IDataBindable)
            {
                IDataBindable db = this as IDataBindable;
                return Bind(db.DefaultProperty, source, field);
            }
            return null;
        }

        public Binding Bind(IDataBindable source)
        {
            if (this is IDataBindable)
            {
                IDataBindable db = this as IDataBindable;
                return Bind(db.DefaultProperty, source, source.DefaultProperty);
            }
            return null;
        }

        public void Unbind()
        {
            if (bindings != null)
            {
                while (bindings.Count > 0)
                    Unbind(bindings[0]);
                bindings.Clear();
            }
        }

        public void Unbind(Binding binding)
        {
        	if( NotifyPropertyChangedHandlers != null )
        	{
            	NotifyPropertyChangedHandlers[binding.DestinationProperty] -= binding.OnDestinationChanged;
            	if (this.NotifyPropertyChangedHandlers[binding.DestinationProperty] == null)
                	NotifyPropertyChangedHandlers.Remove(binding.DestinationProperty);
            }
            if( binding != null && binding.Source != null )
            	binding.Source.Unbind(binding);
            bindings.Remove(binding);
        }

        private Dictionary<string, string> styleArguments;

        public Dictionary<string, string> StyleArguments
        {
            get { if (styleArguments == null) styleArguments = new Dictionary<string, string>(); return styleArguments; }
        }

        private Dictionary<string, string> clientArguments;

        public Dictionary<string,string> ClientArguments
        {
            get { if( clientArguments == null ) clientArguments = new Dictionary<string, string>(); return clientArguments; }
        }

        private Dictionary<string, string> elementArguments;

        public Dictionary<string, string> ElementArguments
        {
            get { if(elementArguments == null) elementArguments = new Dictionary<string, string>(); return elementArguments; }
        }
        
        private Dictionary<string, string> elementProperties;

        public Dictionary<string, string> ElementProperties
        {
            get { if(elementProperties == null) elementProperties = new Dictionary<string, string>(); return elementProperties; }
        }

        public void AddQuotedElementArg(string key, string value)
        {
            ElementArguments[key] = Util.Quotize(value);
        }

        public void AddQuotedArg(string key, string value)
        {
            ClientArguments[key] = Util.Quotize(value);
        }

		private WidgetCollection widgets;
		public WidgetCollection Widgets { get { return widgets; } }
		protected System.Collections.Queue ClientEvents;

		Dictionary<string,string> DataBoundAttributes;
        private Model.AbstractRecord record;
		
		/// <summary>
		/// Property Record (Model.Record)
		/// </summary>
		public virtual Model.AbstractRecord Record
		{
			get
			{
				return this.record;
			}
			set
			{
				if( record != value )
				{
					this.record = value;
					RaisePropertyChangedNotification("Record");
				}
			}
		}
		
		public void SetRecord(AbstractRecord record)
		{
			this.record = record;	
		}

        private bool isSafe = true;

        virtual public bool IsSafe
        {
            get { return isSafe; }
            set { isSafe = value; }
        }
		
		public virtual string BaseElement 
        { 
            get 
            {
                if (OverrideBaseElement != null)
                    return OverrideBaseElement;
                else
                    return ClientId + ".elem"; 
            }
            set 
            { 
                OverrideBaseElement = value; 
            }
        }
        
        public virtual string ToDebugString()
        {
        	object ret = this[DefaultProperty] ?? Value ?? UID;
        	return ret.ToString();
        }
		
		private string cssClassNameOnClient;

        virtual public string ClassName
        {
            get
            {
                return className;
            }
            set
            {
				className = "";
				clientClasses = null;
           		AppendClass( value );
            }
        }
        
      	private void updateClientClassName()
        {
        	if (rendered)
            {
            	//log.Debug("setting classname: ", this, this.rendered, this.removed, this.VisibleToRoot );
                SendCommand("SetAttribute({0},'{1}','{2}')\n", this.ClientId, "class", className);
            }
        }
        
        public void ClearClassName()
        {
        	className = string.Empty;
        	clientClasses.Clear();
        	updateClientClassName();
        }
		
		List<string> clientClasses;
		public void AppendClass( string name )
		{
			if (string.IsNullOrEmpty(name))
				return;
				
			if (null==clientClasses)
				clientClasses = new List<string>();
				
			string[] names= {name};
			bool updated= false;
			
			if (name.Contains(" "))
				names = name.Split(' ');
				
			foreach (string n in names)
			    if (!clientClasses.Contains(n))
			    {
			        updated= true;
			        clientClasses.Add(n);
			    }
			    
			if (!updated)
			    return;
			
			className= Util.Join(clientClasses, " ");
		}
		
		public void RemoveClass( string name )
		{
			if (string.IsNullOrEmpty(name) || null==clientClasses)
				return;
			string[] names= {name};
			bool updated= false;
			
			if (name.Contains(" "))
				names = name.Split(' ');
				
			foreach (string n in names)
			    if (clientClasses.Contains(n))
			    {
			        updated= true;
			        clientClasses.Remove(n);
			    }
			    
			if (!updated)
			    return;
			    
			className= Util.Join(clientClasses, " ");
		}

		public bool HasClass( string name )
		{
			return clientClasses == null || clientClasses.Contains( name );
		}

		public Context RootContext
		{
            get
            {
                if (root != null)
                {
                    Widget currentRoot = root;
                    while (currentRoot.parent != null)
                    {
                        currentRoot = currentRoot.parent;
                    }
                    return currentRoot as Context;
                }
                else if (HttpContext.Current != null)
                {
                    return HttpContext.Current.Items["context"] as Context;
                }
                throw new Exception("wtf? where is the context? ");
            }
			set { 
					if( value == null )
						throw new Exception("cannot set root to null.");
					root = value; 
				}
		}
		
		public virtual string BaseXml { get {return null;} }
                
        public object ObjectId
        {
            get { return id; }
        }

		public string Id
		{
			get { return id; }
			set {
				if( RootContext != null )
					RootContext.UpdateServerId(this,id,value);
				id = value;				
			}
		}

		public Widget Parent
		{
			get { return parent; }
			set 
            {
                this.parent = value;
            }
		}

        public void MoveOnClient(Widget parent)
        {
            this.parent = parent;
            this.InvokeClientMethod("Move", this.parent.ClientId, this.ClientId);
        }

        public string UID
		{
			get
			{
        		if( Parent != RootContext && Parent != null )
				{
					return Parent.UID + "_" + Id;
				}
				else 
                    return Id;
			}
		}

		private string clientId;
        public string ClientId
        {
            get {
            	if( this == RootContext )
            		return null; 
            	if ( clientId == null )
            	{
            		RootContext.GetNewClientId(this);
            	} 
            	return clientId; }
            set { clientId = value; 
            
            	if( value != null ) 
            	{
            		clientId = clientId.Replace(' ','_');
            		RootContext.SetClientId(this, clientId);
            	}
            }
        }

        public virtual string ClientIdBase
        {
            get { return this.GetType().Name; }
        }

		public bool VisibleToRoot
		{
			get
			{
				Widget c = this;
				while( c != null )
				{
					if( ! c.Visible )
					{
						return false;
					}
					c = c.Parent;
				}
				return true;
			}
		}

		public bool IsAncestorOf( Widget w )
		{
			Widget p = w.parent;
			while( p != null )
			{
				if( p == this )
					return true;
				p = p.Parent;
			}
			return false;
		}

		public T FindAncestor<T>() where T : Widget
		{
            Widget p = Parent;
			while( p != null )
			{
				if( p is T )
					return p as T;
				p = p.Parent;
			}
			return null;
		}

        public T FindAncestor<T>(string ID) where T : Widget
		{
			Widget p = this;
			while(p != null && p.Id != ID )
			{
                p = p.Parent;
            }
			return p as T;
		}
		
		public object FindAncestor(Type type)
		{
			Widget p = Parent;
			while( p != null )
			{
				if( type.IsInstanceOfType(p) )
					return p;
				p = p.Parent;
			}
			return null;
		}

		virtual public bool Visible
		{
			get { return visible; }
            set 
            { 
            	if (visible == value) 
            		return;
            	visible = value;
            	 
            	if (value)
            		RootContext.ShowWidget(this); 
            	else 
            		RootContext.HideWidget(this); 
            	RaisePropertyChangedNotification("Visible");
            }
		}

		public void Init()
		{
			Initialize();
			RecurseInit();			
			initialized = true;
			PostInitialize();
		}
		
		public virtual void PostInitialize(){}
		
		public virtual void Initialize(){}
		
		private void RecurseInit()
		{
            if (Widgets != null)
                for(int i = 0; i < Widgets.Count;i++)
                {
                	if( Widgets.Count <= i )
                		break;
                	Widget w = Widgets[i];
                	if( w != null && ! w.initialized )
                    {
                    	w.Init();
                    }
                }
		}

        public virtual bool Render(Surface surface) 
        {
            //IDataSourced ds = this as IDataSourced;
            //if (ds != null && ! ds.IsDataBound)
            //{
            //    ds.DataBind();
           // }
           if( ! initialized )
           	Init();
           surface.Write(GetClientCommand()); 
           return true; 
        }

        internal void RecurseRender( Widget widget, Surface surface )
		{
			widget.TestAndUpdateCssOnClient();
			if( widget.ClientEvents != null && widget.VisibleToRoot )
			{
				Widget c = null;
				while( widget.ClientEvents.Count > 0 )
				{
					object o = widget.ClientEvents.Dequeue();
					//log.Info("processing event " + o );
					if( o is Widget )
					{
						if( widget.debugging )
							log.Debug("rendering widget ", widget.ClientId, o );
						c = o as Widget;
						if( c.VisibleToRoot && ! c.rendered && ! c.removed ) 
						{
							c.Render( surface );
							RecurseRender( c, surface );
							c.rendered = true;
							c.PostRender();
						}
					}
					else if( o != null )
					{
                        string cmd = o.ToString().Replace(clientIdPlaceHolder, widget.ClientId);
                        if( widget.debugging )
                        {
							log.Debug("rendering cmd ", widget.ClientId, cmd );
						}
                        surface.Write(cmd);
					}
				}
			}
			
			if( widget.Widgets != null )
				foreach( Widget child in widget.Widgets )
					RecurseRender( child, surface );
		}

        public void RenderSelfAndChildren(Surface surface)
        {
            Render(surface);
            RecurseRender(this, surface);
            this.rendered = true;
        }
        
        protected virtual void PostRender()
        {
        }
		
		private void TestAndUpdateCssOnClient()
		{
			if( className != cssClassNameOnClient )
			{
				if( ! string.IsNullOrEmpty( cssClassNameOnClient ) )
					InvokeClientMethod("rc",Util.ToJavaScriptString( cssClassNameOnClient ));
				if( ! string.IsNullOrEmpty( className ) )
        			InvokeClientMethod("ac",Util.ToJavaScriptString( className ));
				cssClassNameOnClient = className;
			}	
		}

		public virtual void Update()
		{
			if( Widgets != null )
				foreach( Widget c in Widgets )
					c.Update();
		}

		public void SendEvents(Surface surface)
		{
			if( widgets != null )
			{
				foreach( Widget c in widgets )
				{
					c.SendEvents(surface);
				}
			}

			if( ClientEvents != null )
			{
				while( ClientEvents.Count > 0 )
				{
					surface.Write( ClientEvents.Dequeue().ToString() );
				}
			}
		}

        private const string constructorFormat = " w({0},{1});";

        public string GetClientCommand()
        {
            return GetClientCommand(false);
        }

		public string GetClientCommand( bool onlyGetConstructor )
		{
            ClientArguments["id"] = Util.Quotize(ClientId);
            if( OverrideBaseElement != null )
            {
            	ClientArguments["baseElem"] = OverrideBaseElement;
            }
            if (!this.clientArguments.ContainsKey("p") )
            {
            	if(  this.Parent != null && this.Parent.ClientId != null)
            	{
                	clientArguments["p"] = Parent.ClientId;                
               	}
               	else if( this.Parent == this.RootContext && this.RootContext.BaseElement != null )
               	{
               		ClientArguments["baseElem"] = this.RootContext.BaseElement;
               	}
                else
                {
                	log.Warn(UID + " does not have a parent to render under");
                }
            }
            if( this.className != null )
                clientArguments["cn"] = Util.Quotize(this.className);
            if (elementArguments != null && elementArguments.Count > 0)
            {
                clientArguments["ea"] = JSON.Default.HashToJSON(elementArguments,true);
            }
            
            if (elementProperties != null && elementProperties.Count > 0)
            {
                clientArguments["ep"] = JSON.Default.HashToJSON(elementProperties,true);
            }

            if (styleArguments != null && styleArguments.Count > 0)
            {
                clientArguments["s"] = JSON.Default.HashToJSON(styleArguments,true);
            }
            if( ! onlyGetConstructor )
            	clientArguments["r"] = "1";
            if( position != -1 )
            	clientArguments["idx"] = position.ToString();
            
            // removing String.Format call for optimization
            //string ret = string.Format(constructorFormat, clientClassNameMap.ContainsKey(ClientClass)?clientClassNameMap[ClientClass]:ClientClass,
            //	JSON.Default.HashToJSON(clientArguments,true));
            //return ret;

            return " w(" + (clientClassNameMap.ContainsKey(ClientClass) ? clientClassNameMap[ClientClass] : ClientClass) + "," + JSON.Default.HashToJSON(clientArguments, true) + ");";
		}
		
		public void SendCommand( string format, params object[] args )
		{
			SendCommand( string.Format( format, args ) );
		}

		public void SendCommand( string cmd )
		{
            if (cmd == null || cmd.Length == 0) return;
            if (rendered) cmd = cmd.Replace(clientIdPlaceHolder, ClientId);
            //if (cmd[cmd.Length - 1] != ';') cmd += ';';
            if (ClientEvents == null)
                ClientEvents = new System.Collections.Queue();
               ClientEvents.Enqueue(cmd);
            if (RootContext != null && RootContext != Context.Current && RootContext.CometClient != null)
				RootContext.RequiresTransformation = true;
		}

        public void InvokeClientMethod(string methodName)
        {
            InvokeClientMethod(methodName, string.Empty);
        }

        public void InvokeClientMethod(string methodName, string args)
        {
            SendCommand("{0}.{1}({2});", clientIdPlaceHolder, methodName, args);
        }

        public void InvokeClientMethod(string methodName, params string[] args)
        {
            SendCommand("{0}.{1}({2});", clientIdPlaceHolder, methodName, Util.Join(args));
        }

        public void SetClientElementStyle(string name, string value, bool quotize)
        {
            if (quotize) value = Util.ToJavaScriptString(value);
            SetClientElementStyle(name, value);
        }
        
        public void SetClientElementStyle(string name, string value)
        {
            if (rendered)
            {
                InvokeClientMethod("SetStyle", string.Format("{{{0}:{1}}}", name, value));
            }
            else
            {
                StyleArguments[name] = value;
            }
        }

        public void SetClientElementProperty(string name, string value)
        {
        	if (rendered)
            {
                InvokeClientMethod("SetElementProperty", string.Format("'{0}',{1}", name, value));
            }
            else
            {
                ElementProperties[name] = value;
            }        		
        }

        public void SetClientElementAttribute(string name, string value, bool quotize)
        {
            if (quotize) value = Util.Quotize(value);
            SetClientElementAttribute(name, value);
        }

        public void SetClientElementAttribute(string name, string value)
        {
            if (name == "class") name = "className";
            if (rendered)
            {
                InvokeClientMethod("SetElem", string.Format("{{{0}:{1}}}", name, value));
                ElementArguments[name] = value;
            }
            else
            {
                ElementArguments[name] = value;
            }
        }
        
        public virtual void HandleEvents( string subKey, string evt, string args )
        {
        	HandleEvents( evt, args );
        }

		public virtual void HandleEvents( string evt, string args )
		{
			if ( !Enabled ) return;
            switch(evt)
            {
                case "OnDelayedMouseOver":
                    if( onDelayedMouseOver != null )
                    {
                        string[] coords = args.Split(',');
                        int x = int.Parse(coords[0]);
                        int y = int.Parse(coords[1]);
                        onDelayedMouseOver(this, new DelayedMouseEventArgs( this, x, y ) );
                    }
                    break;
                case "OnDelayedMouseOut":
                    if (onDelayedMouseOut != null)
                    {
                        onDelayedMouseOut(this, new DelayedMouseEventArgs( this, -1, -1 ) );
                    }
                    break;
                case "OnReceiveDrop":
                    if (onReceiveDrop != null)
                    {
                    	string[] parts = args.Split(',');
						int index = int.Parse( parts[1] );
						if( index == -1 )
						{
							log.Error("on received drop returned a -1 ordinal.", this);
							return;
						}						
						Widget droppedWidget = RootContext.GetWidgetByClientId(parts[0]);
                        onReceiveDrop(this, new DragAndDropEventArgs( droppedWidget, this, index ) );
                    }
                    break;
                case "OnClick":
                    if (onClick != null)
                    {
                        onClick(this, new ClickEventArgs( this ) );
                    }
                    break;
				case "OnKeyPress":
					if( onKeyPress != null )
					{
						onKeyPress( this, new KeyPressEventArgs( this, args ) );
					}
					break;
                case "OnBlur":
                	if( onBlur != null )
                		onBlur( this, new WidgetEventArgs( this, null, null ) );
                	break;
                case "Debug":
                	this.debugging = true;
                	log.Debug(this);                	
                	break;
				case "Find":
					this.FindForClient(args);
					break;
            }
        }
		
		private void FindForClient(string args)
		{
			log.Debug("FindForClient: ", this.GetType(), args);	
			Dictionary<string, object> hashArgs = (Dictionary<string, object>) JSON.Default.Decode(args);
			
			string widgetType = (string)hashArgs["widgetType"];
			string widgetServerId = (string)hashArgs["widgetServerId"];
			string recordType = (string)hashArgs["recordType"];
			string callback = (string)hashArgs["callback"];
			int recordId = int.Parse( (string)hashArgs["recordId"] );
			int searchBelow = int.Parse( (string)hashArgs["searchBelow"]);
			
			if( searchBelow == 1 && widgets != null )
			{
				List<string> ids = new List<string>();
				
				List<Widget> ws = null;
				
				if( ! string.IsNullOrEmpty( widgetType ) )
				{
					log.Debug("searching widgets of type ", widgetType );
					Type t = TypeLoader.GetType( widgetType );
					ws = this.widgets.FindAll(t);

                    if (ws == null)
                    {
                        log.Warn("no widgets found");
                    }
                    else
                    {
                        foreach (Widget w in ws)
                        {
                            log.Debug("Found widget of type ", t, w);
                        }
                    }
                    
                    Type rType = null;
                    if( ! string.IsNullOrEmpty( recordType ) )
					{							
						if( ! string.IsNullOrEmpty( recordType ) )
							rType = TypeLoader.GetType(recordType);
					}

                    foreach (Widget w in ws)
                    {
                        if (!string.IsNullOrEmpty(widgetServerId) && w.id != widgetServerId)
                            continue;
                        if( null != rType && ( w.record == null || w.record.GetType() != rType ) )
                            continue;
                        if (null != rType && recordId > 0 && w.record.Id != recordId) 
                            continue;
                        log.Debug("found widget ", w.id, w.ClientId);
                        ids.Add(w.ClientId + "::" + w.id);
                    }
				}
				else
				{
					log.Debug("no widget type specified");
					if( ! string.IsNullOrEmpty( widgetServerId ) )
					{
						log.Debug("looking for widget id ", widgetServerId );
						Widget w = this.widgets.Find<Widget>(widgetServerId);
						log.Debug("found widget ", w );
						if( w != null )
							ids.Add( w.ClientId + "::" + w.id );
					}
				}
				
				string output = string.Format( "{0}({1});", callback, JSON.Default.Encode( ids ) );
				
				log.Info("Found ", output );
				HttpContext.Current.Response.Write( output );
			}
			else
			{
				log.Debug( "Not searching below", searchBelow, widgets );
			}
		}

        public const string clientIdPlaceHolder = "$ClientId";
		public void SetClientAttribute( string Name, object Value )
		{
            if (rendered)
            	InvokeClientMethod("SetAttribute",Util.ToJavaScriptString(Name), Value.ToString());
            else
                ClientArguments[Name] = Value.ToString();
		}
		
		public void SetClientProperty( string Name, string Value )
		{
			if (rendered)
				InvokeClientMethod("SetProperty",Util.ToJavaScriptString(Name), Value);
            else
                ClientArguments[Name] = Value.ToString();
		}

        public void Center()
        {
            InvokeClientMethod("Center");
        }

        public void Effect(string name)
        {
            SendCommand("Effect.{0}($('{1}'));", name, clientIdPlaceHolder);
        }
        
        public void FadeShow()
        {
        	this.Visible = true;
        	this.Opacity = 0.0f;
        	this.InvokeClientMethod("FadeShow","500");
        }
        
        public void FadeHide()
        {        
        	this.InvokeClientMethod("FadeHide","500");
        }
        

        private string foreColor;

        public string ForeColor
        {
            get { return foreColor; }
            set
            {
                foreColor = value; foreColor = value; SetClientElementStyle("color", value, true);
                RaisePropertyChangedNotification("ForeColor");
            }
        }
	

        string backgroundColor;
        virtual public string BackgroundColor
        {
            get
            {
                return backgroundColor;
            }
            set
            {
                backgroundColor = value; SetClientElementStyle("backgroundColor", value, true);
                RaisePropertyChangedNotification("BackgroundColor");
            }
        }

        float opacity;
        virtual public float Opacity
        {
            get
            {
                return opacity;
            }
            set
            {
                opacity = value;
                SetClientAttribute("opac",value);                
            }
        }

        virtual public object this[string key]
        {
            get
            {
               	return GetAttribute(key);
            }
            set
            {
                if (value is string)
                    SetAttribute(key, value as string);
                else
                {
                	try
                	{
						if( this is IWidgetDecorator )
						{
							((IWidgetDecorator)this).Widget[key] = value;
							return;
						}
						GenericPropertyInfo gpi = TypeLoader.GetGenericPropertyInfo(this,key);
						gpi.Setter(this,PropertyConverter.Convert(value,gpi.PropertyInfo.PropertyType));
						RaisePropertyChangedNotification(key);
	                }
	                catch( Exception e )
	                {
	                	log.Error("an error occurred setting attribute " + key + "\n" + Util.BuildExceptionOutput(e),
	                		"setting value: ", value != null ? value.GetType().FullName : "(null)", "widget", this.id);
	                	throw e;
	                }
                }
            }
        }

        public Type GetFieldTypeFromName(string name)
        {
            try
            {
                return this.GetType().GetProperty(name).PropertyType;
            }
            catch (System.Reflection.AmbiguousMatchException)
            {
                return this.GetType().GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).PropertyType;
            }
        }

		public virtual bool SetAttribute( string Name, string Value )
		{
			//log.Debug("SetAttribute",Name,Value);
			return SetAttribute(Name,Value,false,false);
		}
		
		public virtual MethodInfo FindMethod( string methodName, out object target )
		{
			MethodInfo handlerMI = null;
			Widget currentRoot = this;
			while( currentRoot != null )
            {
            	Type t = currentRoot.GetType();
            	handlerMI = t.GetMethod(
            			methodName,
            			BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic |
                        BindingFlags.Instance );
                        
            	if (handlerMI == null)
                {
                	currentRoot = currentRoot.parent; 
                	continue;
                }
               
                break;
            }
            
            if( handlerMI == null )
            {
            	currentRoot = RootContext;
            	BindingFlags flags = 
            		BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic |
            			BindingFlags.Instance | BindingFlags.DeclaredOnly;
					
            	MemberInfo[] mis = RootContext.GetType().FindMembers( MemberTypes.Method,
                        flags,
                        Type.FilterName, methodName);
               if( mis != null && mis.Length > 0 )
               		handlerMI = mis[0] as MethodInfo;
            }
			
			target = currentRoot;
			
			return handlerMI;
		}
		
		private Dictionary<string,string> lostEvents;
		
		public virtual bool SetAttribute( string Name, string Value, bool suppressChangeNotification, bool suppressBindings )
		{
			setProperties[ Name ] = Value;
			//# is the databind symbol for now. Two #s (##) escapes a #.
			//what about:
			//"Title: #Title#" can it somehow => "Title: {0}", 
            if( ! suppressBindings && InitDataBoundAttribute(Name, Value) )
            	return false;
            //test for an event first
            Type thisType = this.GetType();
            EventInfo ei = thisType.GetEvent(Name);
			//System.Console.WriteLine("setting {0} to {1}", Name, Value);
            if (ei != null)
            {			
            	MethodInfo handlerMI = null;
                if( Value.Contains(".") )
                {
                    List<string> handlerParts = new List<string>(Value.Split('.'));
                    string methodName = handlerParts[ handlerParts.Count - 1 ];
                    handlerParts.RemoveAt( handlerParts.Count - 1 );
                    string handlerTypeName = Util.Join( handlerParts.ToArray(), "." );
                    Type handlerType = TypeLoader.GetType(handlerTypeName);
                    handlerMI = handlerType.GetMethod( methodName );
                    //if this is a generic method, let's try to assign it the first generic param of this.
                    if (handlerMI.IsGenericMethod)
                    {
                        handlerMI = handlerMI.MakeGenericMethod(this.GetType().GetGenericArguments()[0]);
                    }
                    ei.AddEventHandler(this, Delegate.CreateDelegate(ei.EventHandlerType, handlerMI));
                }
                else
                {
					Widget currentRoot = this;
					Type[] typeParams = null;
					ParameterInfo[] parms = ei.EventHandlerType.GetMethod("Invoke").GetParameters();
					if( parms != null && parms.Length > 0  )
					{
						List<Type> ltypes = new List<Type>();
						foreach( ParameterInfo pi in parms )
						{
							ltypes.Add( pi.ParameterType );
						}
						typeParams = ltypes.ToArray();
					}
                    while( currentRoot != null )
                    {
                    	Type t = currentRoot.GetType();
                    	handlerMI = t.GetMethod(
                    			Value,
                    			BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic |
                                BindingFlags.Instance, 
                                null,
                                typeParams,
                                null);
                                
                    	/*log.Debug("looking for event", 
                    		Name, 
                    		Value, 
                    		currentRoot.GetType(),
                    		typeParams,
                    		handlerMI);
                    	*/
                    	
                    	if (handlerMI == null)
                        {
                        	currentRoot = currentRoot.parent;
                        	continue;
                        }
                       
                        break;
                    }
                    
                    if( handlerMI == null )
                    {
                    	currentRoot = RootContext;
                    	BindingFlags flags = 
                    		BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic |
                    			BindingFlags.Instance | BindingFlags.DeclaredOnly;
							
                    	MemberInfo[] mis = RootContext.GetType().FindMembers( MemberTypes.Method,
                                flags,
                                Type.FilterName, Value);
                       if( mis != null && mis.Length > 0 )
                       		handlerMI = mis[0] as MethodInfo;
                    }
                    if( handlerMI != null )
                    {
                    	//log.Debug("FOUND HANDLER",Name,Value, ei.EventHandlerType);
                        ei.AddEventHandler(this, Delegate.CreateDelegate(ei.EventHandlerType, currentRoot, Value, true));
                        if( lostEvents != null && lostEvents.ContainsKey( Name ) )
                        {
                        	lostEvents.Remove(Name);
                        }
                    }             
                    else
                    {
                    	//log.Warn("NEVER FOUND HANDLER", Name, Value);
                    	if( lostEvents == null )
                    		lostEvents = new Dictionary<string,string>();
                    	if( ! lostEvents.ContainsKey( Name ) )
                    		lostEvents[ Name ] = Value;
                    	return false;
                    }
                }
            }
            else
            {
                PropertyInfo pi = TypeLoader.GetGenericPropertyInfo(this,Name).PropertyInfo;                
                if (pi == null)
                {
                	this.StateBag[Name] = Value;
                    return false;
                    //ClientArguments[Name] = Value.ToString();
                }
                if (pi.PropertyType == typeof(String) || pi.PropertyType == typeof(object) || pi.PropertyType.IsInstanceOfType( Value ) )
                {
					TypeLoader.SetProperty(this,Name,Value);
                   if( ! suppressChangeNotification )  RaisePropertyChangedNotification(Name);
                }
                else if( pi.PropertyType == typeof(Type))
                {
					TypeLoader.SetProperty(this,Name,TypeLoader.GetType(Value));
				}
                else if( pi.PropertyType.IsSubclassOf( typeof(AbstractRecord) ) )
                {
					TypeLoader.SetProperty(this,Name,PropertyConverter.Convert( Value, pi.PropertyType ));
                	if( ! suppressChangeNotification )  RaisePropertyChangedNotification(Name);
                }
				else if( pi.PropertyType.IsAssignableFrom(typeof(EmergeTk.IDataSourced))
						|| pi.PropertyType.IsSubclassOf( typeof( Widget ) ) 
						|| pi.PropertyType == typeof( Widget ) )
				{
					//worth taking a hit to check direct ancestors first, then a second pass to check for ancestors descendents.
					
					Widget target = this;
					while( target != null )
					{
						if( target.id == Value )
							break;
						target = target.parent;
					}
					
					if( target == null )
					{
						target = this;
						while( target != null )
						{
							Widget w = target.Find( Value );
							if( w != null )
							{
								target = w;
								break;
							}
							target = target.parent;
						}
					}
					
					if( target != null )
					{						
						TypeLoader.SetProperty(this,Name,target);
					}
					else
					{
						RootContext.ListenForWidgetId(Value, new EventHandler( delegate( object sender, EventArgs ea ) {
							TypeLoader.SetProperty(this,Name,sender);
						}) );
					}
				}
                else
                {
					//System.Console.WriteLine("pi property type interfaces pi:" + pi.PropertyType);
					//foreach( object o in pi.PropertyType.GetInterfaces() )
					//	System.Console.WriteLine(o);
					object v = PropertyConverter.Convert( Value, pi.PropertyType);
                    try
                   	{
                   		TypeLoader.SetProperty(this,Name,v);
                   	}
                   	catch( FormatException fe )
                   	{
                   		//something like setting null to boolean value - log and move on.
                   		log.Error(Util.BuildExceptionOutput(fe));
                   		throw( new Exception("error setting value", fe ) );
                   	}
                   	
                    if( ! suppressChangeNotification ) RaisePropertyChangedNotification(Name);
                }
            }
            return true;
		}

        protected bool InitDataBoundAttribute(string Name, string Value)
        {
			if (Value != null && Value.IndexOf('{') > -1 && 
                (DataBoundAttributes == null || ! DataBoundAttributes.ContainsKey(Name) ) )
            {
                Regex r = new Regex(@"\{\S+?\}");
                if (r.IsMatch(Value))
                {
                    if (DataBoundAttributes == null)
                        DataBoundAttributes = new Dictionary<string, string>();
                    //Debug.Trace("databinding attribute {0}:{1}", Name, Value);
                    DataBoundAttributes[Name] = Value;
                    return true;
                }
            }
            /*else if( Value == null && DataBoundAttributes != null && DataBoundAttributes.ContainsKey(Name) )
			{
				DataBoundAttributes[Name] = Value;
				return true;
			}*/
				
            return false;
        }

		protected void ClearDataBoundAttributes()
		{
			DataBoundAttributes = null;
		}
        
        public void InitDataBoundAttribute(string Name)
        {
        	InitDataBoundAttribute(Name, Convert.ToString(this[Name]));
        }
        
        public PropertyInfo GetProperty( string Name )
        {
        	return TypeLoader.GetGenericPropertyInfo(this,Name).PropertyInfo;
        }

		public object GetAttribute( string Name )
		{
			return TypeLoader.GetGenericPropertyInfo(this,Name).Getter(this);
		}

		public Type DiscoverType( string SimpleName )
		{
			if( SimpleName.IndexOf(".") == -1 )
			{
				SimpleName = RootContext.DefaultNamespace + "." + SimpleName;
			}
            return TypeLoader.GetType(SimpleName);
		}

		public virtual void Add( Widget c )
		{
			setupChild(c,-1,this);
		}

		public virtual void Add( params Widget[] widgets )
		{
            foreach (Widget c in widgets)
            {
                if (c != null)
                    Add(c);
            }
		}
		
		public void ReplaceChild( Widget oldChild, Widget newChild )
		{
			if( this.widgets == null )
				return;
			int index = widgets.IndexOf(oldChild);
			RemoveChild(oldChild);
			setupChild(newChild, index, this);
		}
		
		public Widget Replace(Widget newWidget )
		{
			if( this.parent != null )
				this.parent.ReplaceChild(this, newWidget );
			return newWidget;
		}
		
		public virtual void Insert( Widget c, int index )
		{
			setupChild( c, index, this );
		}

		public virtual void InsertBefore( Widget c )
		{
            int thisIndex = Parent.widgets.IndexOf(this);
			setupChild(c, thisIndex, this.parent);
		}

		public virtual void InsertAfter( Widget c )
		{
            int thisIndex = Parent.widgets.IndexOf(this);
			setupChild(c, thisIndex+1, this.parent);
		}
		
		public void InitializeWidgets()
		{
			widgets = new WidgetCollection();
		}
		
		private void setupChild(Widget c, int index, Widget parentWidget)
		{
			if( parentWidget.widgets == null )
			{
				parentWidget.widgets = new WidgetCollection();
			}

			if( parentWidget.ClientEvents == null )
			{
				parentWidget.ClientEvents = new System.Collections.Queue();
			}
			
			c.removed = false;
			c.rendered = false;
			c.root = root;
			if( c.record == null && record != null )
				c.record = record;
			c.parent = parentWidget;
			if( index != -1 )
			{
				c.position = index;
				parentWidget.widgets.Insert(index,c);
			}
			else
				parentWidget.widgets.Add( c );
            parentWidget.ClientEvents.Enqueue(c);
            if (RootContext != null && RootContext != Context.Current && RootContext.CometClient != null)
				RootContext.RequiresTransformation = true;
			c.WireUpLostEvents();
		}
		
		public void WireUpLostEvents()
		{
			if( lostEvents != null && lostEvents.Count > 0 )
			{
				bool done = false;
				while( ! done )
				{
					if( done ) break;
					done = true;
					foreach( var pair in lostEvents )
					{
						if( SetAttribute( pair.Key, pair.Value ) )
						{
							done = false;
							break;
						}
					}
				}
			}
			
			if( widgets != null )
			{
				foreach( Widget w in widgets )
				{
					w.WireUpLostEvents();
				}
			}
		}

        /** Hook for Widgets that need to clean up before they are removed from
            their parents.
         **/
        public virtual void WillBeRemovedFromParent()
        {}
        
		bool removed = false;
        public void Remove()
        {
        	if( removed ) return;
        	WillBeRemovedFromParent();
            RootContext.RemoveWidget(this);
            removed = true;
            
            if( parent != null )
            	parent.Widgets.Remove( this );
        }
        
        
		public void ClearChildren()
		{
			if( Widgets != null )
				while( Widgets.Count > 0 )
					RemoveChild( Widgets[0] );
            if( Widgets != null ) Widgets.Clear();
		}

		public void RemoveChild( Widget row )
		{
			if( row == null )
				return;
			row.Remove();
			if( this.Widgets != null )
				this.Widgets.Remove( row );
		}        

		public bool IsParent
		{
			get { if( widgets == null ) return false; if( widgets.Count == 0 ) return false; return true; }
		}

        public Context NearestContext
        {
            get 
            {
            	Context r = this as Context;
				if (r != null )
					return this as Context; 
				r = FindAncestor<Context>();
				if (r != null ) 
					return r as Context; 
				return this.RootContext;
			}
        }
        
        public void Parse()
        {
            Parse((string)null);
        }
        
        public void Parse(string xml)
        {
			//log.Debug("Parsing widget", this.GetType() );
        	XmlDocument doc = null; 
			string rootNodeName = this is Context ? "Context" : "Widget";
			XmlNode rootNode = null;
                       
            if (xml != null)
            {
				doc = new XmlDocument();
                doc.LoadXml(xml);
                rootNode = doc.SelectSingleNode(rootNodeName);
            }
            else
            {				
            	rootNode = ThemeManager.Instance.RequestView( this.Name.Replace(".", "/")  + ".xml", rootNodeName ); 
            	if( rootNode == null )
            	{
            		//if i am generic, i should also check for `1 format.
            		Type myType = this.GetType();
            		if( myType.IsGenericType )
            		{
            			string name = myType.Name;
            			if( ! string.IsNullOrEmpty( myType.Namespace ) )
            				name = myType.Namespace + "." + name;
            			rootNode = ThemeManager.Instance.RequestView( name.Replace(".", "/")  + ".xml", rootNodeName );                 		
	                }
					
					
            	}
            }
            
            if( rootNode == null && BaseXml != null )
            {
				doc = new XmlDocument();
            	doc.LoadXml( BaseXml );
            	rootNode = doc.SelectSingleNode( rootNodeName );
            }
            
            if( rootNode == null ) 
			{
				//log.Error("No rootNode");
            	return;
			}
            
            Parse( rootNode );
        }

		public void Parse(XmlNode rootNode)
		{
			if( rootNode == null )
				return;
            
            if( this is Context )
			{
				Context thisc = this as Context;
				if( rootNode.Attributes["DefaultNamespace"] != null )
				{
					thisc.DefaultNamespace = rootNode.Attributes["DefaultNamespace"].Value;
					rootNode.Attributes.RemoveNamedItem("DefaultNamespace");
				}
				
	            if (rootNode.Attributes["DataBind"] != null && Convert.ToBoolean(rootNode.Attributes["DataBind"].Value))
	            {
	                thisc.DataBindPostInit = true;
	                rootNode.Attributes.RemoveNamedItem("DataBind");
	            }
	            
	        }
	        
			this.TagPrefix = rootNode.GetPrefixOfNamespace("http://www.emergetk.com/");

            foreach( XmlAttribute na in rootNode.Attributes )
            {
            	if( na.Prefix != "xmlns" )
            	{
            		this.SetAttribute(na.Name, na.Value);
            	}
            }
            
			string xpathRecordList = null;
	        if( this.TagPrefix != null )
	        	xpathRecordList = "//RecordList";
	       	else
	       		xpathRecordList = string.Format("//{0}:RecordList",this.TagPrefix);
            XmlNodeList recordLists = rootNode.SelectNodes(xpathRecordList);
            if( recordLists.Count > 0 )
            {
	            List<XmlNode> recordListsToRemove = new List<XmlNode>();
	            foreach (XmlNode n in recordLists)
	            {
	            	ParseRecordList(n);
	                recordListsToRemove.Add(n);                
	            }
	            foreach( XmlNode n in recordListsToRemove )
	            	n.ParentNode.RemoveChild(n);
			}
			ParseXml( rootNode );
		}

		public void ParseXml( XmlNode node )
		{
			for( int i = 0; i < node.ChildNodes.Count; i++ )
			{
                XmlNode n = node.ChildNodes[i];
                if (n.Name == "#text" || n.Name == "#cdata-section")
                {
					
					string text = n.InnerText;
					bool appendSpace = false, prependSpace = false;
					if( text.EndsWith(" " ) )
						appendSpace = true;
					if( text.StartsWith( " " ) )
						prependSpace = true;
					text = text.Trim();
					if( prependSpace )
						text = " " + text;
					if( appendSpace )
						text = text + " ";
					PropertyDescriptor defaultProperty = TypeDescriptor.GetDefaultProperty(this);
                    if (defaultProperty != null)
                    {
                    	this[defaultProperty.Name] = text;
                	}
                	else
                	{
                		Literal literal = RootContext.CreateWidget<Literal>();
                		literal["Html"] = text;
                		Add(literal);
                	}	              
                }
                else if( n.LocalName == "ClientAttribute" )
                {
                	this.ElementArguments[ n.Attributes["Key"].Value ] = n.Attributes["Value"].Value;
                }
                else if (GetNearestTagPrefix() == null || n.Prefix == GetNearestTagPrefix() )
                    ParseElement(n);
                else if(  n.Name != "#comment" ) 
                    ParseHtmlElement(n);
			}
		}
		
		private string GetNearestTagPrefix()
		{
			Widget w = this;
			while( w != null )
			{
				if( w.tagPrefix != null )
					return w.tagPrefix;
				w = w.parent;
			}
			return null;
		}


        private void ParseHtmlElement(XmlNode n)
        {
			/*Widget parent,
			 AbstractRecord record,
			 XmlNode xml,
			 bool parseXml,
			 bool addToParent*/
				
            HtmlElement he = NearestContext.CreateWidget<HtmlElement>
			 (this, this.record,null,false,true);
			he.currentGenericType = currentGenericType;
            he.TagName = n.Name;
            he.id = he.TagName + he.id;
            he.DefaultTagPrefix = "html";
            //Add(he);
            foreach (XmlAttribute a in n.Attributes)
            {
            	if( a.Prefix != "xmlns" )
            		he.SetAttribute(a.Name, a.Value);
            }
            
            he.ParseXml(n);
        }

		internal Type currentGenericType;
		
        private Type GetGenericAncestorType()
        {
        	if( currentGenericType != null )
				return currentGenericType;
			else if( parent != null )
				return parent.GetGenericAncestorType();
			else
				return null;
		}
		
        public virtual void ParseElement( XmlNode n )
		{
			string typeName = n.LocalName;
			
			Type t = null;
			Type genericTypeParam = null;
			try {				
			    t = TypeLoader.GetType(typeName);
			    genericTypeParam = null;
			   //log.Debug("parsing element: ", t, n.LocalName);
				
	            if( t == null && typeName.IndexOf(".") == -1 )
				{
					string[] namespaces = new string[] { RootContext.DefaultNamespace, "EmergeTk", "EmergeTk.Widgets.Html" };
					foreach( string ns in namespaces )
					{
						if( t != null )
							break;
						typeName = ns + "." + n.LocalName;
			        	t = TypeLoader.GetType(typeName);
	        		}
				}
				PropertyInfo pi = GetProperty( n.LocalName );
				if( t == null && pi != null && typeof(Widget).IsAssignableFrom(pi.PropertyType) )
				{
					//if there is a property of the correct name, and the type is a widget,
					//create a widget and associate it.
					Widget propertyWidget = this.RootContext.CreateUnkownWidget( pi.PropertyType,null,null,this.Record );
					//log.Debug( "setting up property widget", n.LocalName );
					propertyWidget.currentGenericType = currentGenericType;
					setupWidget( propertyWidget, n );			
					this[n.LocalName] = propertyWidget;
					//log.Debug("done setting up property widget");
					return;
				}
				else if( t == null && pi != null )
				{
					//try to convert the innerxml of the element to a type that is compatible 
					//with the property.
					log.Warn("assigning property ", n.LocalName, n.InnerXml );
					this[n.LocalName] = PropertyConverter.Convert( n.InnerXml, pi.PropertyType );
					return;
				}
			    if (t == null)
	            {
	                throw new System.TypeLoadException( "Could not create widget for " + typeName );
	                //log.Error( "Could not find type for ", typeName, this.GetType() );
	                //return;
	            }
	            if( t.IsGenericType )
	            {            	
	        		string modelName = null;
	            	if( n.Attributes["Model"] != null )
	                	modelName = n.Attributes["Model"].Value;
	                if( modelName == null &&   n.Attributes["Type"] != null )
	                	modelName = n.Attributes["Type"].Value;
	              	if( modelName == null )
	              	{
	              		genericTypeParam = currentGenericType ?? GetGenericAncestorType();
	              	}
	              	if( genericTypeParam == null )
	              	{
						if ( modelName == null )
							throw new XmlException("Missing required attribute for widget: 'Type'"); 
						genericTypeParam = TypeLoader.GetType(modelName);
					}
					
	                if (genericTypeParam == null)
	                {
	                    if (t.GetInterface("IDataSourced") != null )
	                    {
	                        genericTypeParam = XmlTypeBuilder.CreateType(modelName);
	                    }
	                    else if (modelName.IndexOf(".") == -1)
	                    {
	                        modelName = RootContext.DefaultNamespace + "." + modelName;
	                        genericTypeParam = TypeLoader.GetType(modelName);
	                    }
	                }
	                if (genericTypeParam.IsSubclassOf(typeof(XmlRecord)))
	                {
	                    ColumnInfoManager.RegisterColumns(genericTypeParam, XmlTypeBuilder.ReadColumnInfos(modelName, genericTypeParam));
	                }
	                
					Type genericType = null;
					try
					{	
	                	genericType = t.MakeGenericType(genericTypeParam);
	                }
	                catch(Exception e)
	                {
	                	log.Error( "Invalid generic widget", n.Name, typeName, t, genericType, genericTypeParam, n.InnerXml, n.OwnerDocument.OuterXml );
	                	throw new Exception("Invalid generic widget.", e);
	                }
	                t = genericType;
	            }
			
				if( genericTypeParam != null )
					currentGenericType = genericTypeParam;
				//log.Debug("currentGenericType : " , Id, GetType().Name, currentGenericType );
	            Widget ctrl = RootContext.CreateUnkownWidget(t,null,null,this.Record,false,true);
				if( ctrl == null )
					throw new Exception("ctrl is null");
				ctrl.currentGenericType = currentGenericType;
				ctrl.Parse();
	            setupWidget( ctrl, n );
	            Add( ctrl );
				//currentGenericType = null;
           }
           catch(Exception e )
           {
           		log.Error(
				          Id, 
				          typeName, 
				          t, 
				          currentGenericType, 
				          parent, 
				          parent != null && parent.currentGenericType != null ? parent.currentGenericType.ToString() : "", 
				          genericTypeParam, 
				          n != null ? n.Name : "");
           		throw ( e );
           }
		}
		
		private void setupWidget( Widget ctrl, XmlNode n )
		{
			if( ctrl == null )
			{
				log.Debug("why is ctrl null?" );
				log.Debug(n.OuterXml);
				return;
			}
			ctrl.parent = this;
			
		 	if (ctrl is IDataSourced && n.Attributes["DataSource"] != null )
            { 	
				string key = n.Attributes["DataSource"].Value;
           	 	IDataSourced ids = ctrl as IDataSourced;
           	 	if( this.Record != null && this.Record[key] != null && this.Record[key] is IRecordList )
           	 	{
            		//Debug.Trace("setting datasource on {0} to this.Record['{1}'] ({2})", ctrl, key, this.record[key] );
            		ids.DataSource = this.Record[key] as IRecordList;
            		ids.DataSource.OnRecordAdded += new EventHandler<RecordEventArgs>( delegate( object sender,  RecordEventArgs r )
        			{
        				this.Record.SaveRelations(key);
        			});
            		ids.DataSource.OnRecordRemoved += new EventHandler<RecordEventArgs>( delegate( object sender, RecordEventArgs r )
        			{
        				this.Record.SaveRelations(key);
        			});
            	}
            	else if( RootContext.RecordLists.ContainsKey(key)  )
                	ids.DataSource = RootContext.RecordLists[key];
                else
                {
                	//Debug.Trace("setting property source on {0} to {1}", ctrl, key);
                	ids.PropertySource = key;
				}
                n.Attributes.Remove(n.Attributes["DataSource"]);
            }
			ctrl.ParseAttributes( n );

			//System.Console.WriteLine("parsing widget " + ctrl.UID);
			ctrl.ParseXml( n );

		}

        protected void ParseRecordList(XmlNode n)
        {
            string modelName = n.Attributes["Type"].Value;
            Type recordType = TypeLoader.GetType(modelName);
            if (recordType == null)
                recordType = XmlTypeBuilder.CreateType(modelName);
            string Id = n.Attributes["Id"].Value;
            List<FilterInfo> filters = new List<FilterInfo>();
            List<SortInfo> sorts = new List<SortInfo>();

            for (int i = 0; i < n.ChildNodes.Count; i++ )
            {
                XmlNode child = n.ChildNodes[i];
                switch (child.Name)
                {
                    case "FilterInfo":
                    	object value = null;
                    	string val = child.Attributes["Value"].Value;
						if( val == "$record" )
							value = Record;
						else if( val.StartsWith("$record.") && Record != null )
						{
							value = Record[val.Replace("$record.","")];
						}
						
                        filters.Add(new FilterInfo(
                            child.Attributes["Name"].Value,
                            value,
                            child.Attributes["Operation"] != null ? 
                              (FilterOperation)Enum.Parse(typeof(FilterOperation), child.Attributes["Operation"].Value) :
                              FilterOperation.Equals )
                            );
                        break;
                    case "SortInfo":
                        SortDirection direction = SortDirection.Ascending;
                        if (child.Attributes["Direction"] != null)
                            direction = (SortDirection)Enum.Parse(typeof(SortDirection), child.Attributes["Direction"].Value);
                        sorts.Add(new SortInfo(
                            child.Attributes["Name"].Value,
                            direction));
                        break;
                }
            }

            MethodInfo mi = typeof(DataProvider).GetMethod("LoadList", BindingFlags.Public | BindingFlags.Static, null,
                new Type[] { typeof(FilterInfo[]), typeof(SortInfo[]) }, null);
            IRecordList newList = mi.MakeGenericMethod(recordType).Invoke(null, new object[] { filters.ToArray(), sorts.ToArray() }) as IRecordList ;
            if (n.Attributes["Live"] != null && Convert.ToBoolean(n.Attributes["Live"].Value))
            {
                newList.Live = true;
            }
            
            RootContext.RecordLists[Id] = newList;

        }

		public void ParseAttributes( XmlNode n )
		{
			if( n.Attributes == null  )
				return;
			foreach( XmlAttribute att in n.Attributes )
			{
				SetAttribute( att.Name, att.Value );
			}
		}
		
		public Type BindsTo { get; set; }
		
		public virtual void PostDataBind()
		{
			if( Widgets != null )
			{
				foreach( Widget w in Widgets )
					w.PostDataBind();
			}
		}

        public virtual void DataBindWidget()
        {
            DataBindWidget(this.record);
        }

		public virtual void DataBindWidget( Model.AbstractRecord data )
		{
			DataBindWidget( data, false);
		}
		
		bool dataBound = false;
		public virtual void DataBindWidget( Model.AbstractRecord data, bool forceRecord )
		{
			if( BindsTo != null && 
				( data == null || ( data != null && ! BindsTo.IsAssignableFrom( data.GetType() ) ) ) )
			{
				//log.Debug("did not data bind b/c of bindsTo.");
				return;
			}

			if( forceRecord )
				UnDataBindWidget();

			if( data != null )
			{
				Record = data;
			}
			
			if( DataBoundAttributes != null )
                foreach (string DataBoundAttribute in DataBoundAttributes.Keys)
                {
                   	Bind(DataBoundAttribute, record, DataBoundAttributes[DataBoundAttribute]);
                }
            if( this is EmergeTk.IDataSourced )
            {
            	IDataSourced ids = this as IDataSourced;
            	ids.DataBind();
            }

			if( Widgets != null )
                for (int i = 0; i < widgets.Count; i++)
                {
                    Widget c = widgets[i];
                    if( c.Visible )
                    	c.DataBindWidget(data, false);
                    else
                    {
						c.BindProperty( "Visible", delegate {
							if( ! c.dataBound )
								c.DataBindWidget(data,false);
						});
					}
                }
           dataBound = true;
		}

		public void UnDataBindWidget()
		{
			this.record = null;
			if( Widgets != null )
			{
				foreach (Widget w in Widgets ) {
					w.UnDataBindWidget();
				}
			}		
		}

		public T Find<T>(string ID) where T : Widget
        {
			if ( this is T && this.Id == ID ) return this as T;
        	if( Widgets != null )
            	return Widgets.Find<T>(ID);
            else
            	return null;
        }
        
       	public T Find<T>() where T : Widget
        {			
			if (this is T) return this as T;
        	return Find<T>(null);
        }

		public Widget Find( string ID )
		{
			if ( this.Id == ID ) return this;
			if( Widgets != null )
			{
				return Widgets.Find( ID );
			}
			return null;
		}
		
		public List<T> FindAll<T>() where T : class
		{
			if( Widgets != null )
				return Widgets.FindAll<T>();
			else
				return null;
		}

		#region ICloneable Members
        private bool isCloning;

        public bool IsCloning
        {
            get { return isCloning; }
        }

		public virtual object ShallowClone()
		{
			Widget newWidget = this.MemberwiseClone() as Widget;
			//newWidget.widgetsToRender = new Queue();
			if( clientClasses != null )
				newWidget.clientClasses = new List<string>(clientClasses);
            if( lostEvents != null )
            {
            	newWidget.lostEvents = new Dictionary<string,string>();
            	foreach( var p in lostEvents )
            		newWidget.lostEvents[p.Key] = p.Value;
            }
			newWidget.isCloning = true;
            newWidget.PreClone();
			newWidget.ClientEvents = null;
            newWidget.Parent = null;
            newWidget.widgets = null;
			newWidget.ClientId = null;
			//newWidget.RootContext = this.RootContext;
			//newWidget.bindings = new List<Binding>();
            if(clientArguments != null )
            {
                newWidget.clientArguments = new Dictionary<string,string>(clientArguments);
                newWidget.clientArguments.Remove("p");
            }
            if(elementArguments != null)
                newWidget.elementArguments = new Dictionary<string, string>(elementArguments);
                
            if( DragSource != null )
				newWidget.DragSource = DragSource;
			return newWidget;
		}

		public virtual object Clone()
		{
			Widget newWidget = ShallowClone() as Widget;
			if( Widgets != null )
				for( int i = 0; i < Widgets.Count; i++ )
				{
					newWidget.Add( Widgets[i].Clone() as Widget );
				}
			
			newWidget.Swizzle();
			newWidget.PostClone();
			if( newWidget.OnClone != null )
				newWidget.OnClone( this, new CloneEventArgs( this, newWidget ) );
            newWidget.isCloning = false;
			return newWidget;
		}
		
		protected virtual void PostClone()
		{
		}
		
        protected virtual void PreClone()
        {
        }

		public event EventHandler<CloneEventArgs> OnClone;

		#endregion
		
		private System.Collections.Generic.Dictionary<string,string> setProperties = new Dictionary<string,string>();
		public Dictionary<string,object> Serialize()
		{
			return Serialize(new Dictionary<string,object>());
		}
		
		public virtual Dictionary<string,object> Serialize(Dictionary<string,object> h)
		{
			SerializeSelf( h );
			if( this.widgets != null && this.widgets.Count > 0 )
			{
				ArrayList children = new ArrayList();
				for( int i = 0; i < widgets.Count; i++ )
				{
					children.Add( widgets[i].Serialize() );
				}
				h.Add("_children", children);
			}
			
			return h;
		}
		
		public void SerializeSelf( Dictionary<string,object> h)
		{
			Type t = this.GetType();
			h.Add("Id",id);
			h.Add("_type", t.FullName);
			h.Add("ClientId", this.ClientId);
			h.Add("ClassName",this.ClassName);
			h.Add("rendered", this.rendered);
			h.Add("debugging", this.debugging);
			if( this.record != null )
			{
				h.Add("_recordType", this.record.GetType().FullName);
				h.Add("_recordId", this.record.Id );
			}
			foreach( string k in setProperties.Keys )
			{
				h[k] = this[k] ?? setProperties[k];
			}
			if( onClick != null && ! h.ContainsKey( "OnClick" ) )
				h.Add("OnClick", GetMethodNameForEvent("OnClick") );
		}
		
		public virtual void Swizzle(){}
		
		protected bool deserializing = false;
		
		public bool IsDeserializing
		{
			get 
			{
				return deserializing;
			}
			set
			{
				deserializing = value;
			}
		}
		
		public virtual void Deserialize(Dictionary<string,object> data )
		{
			Deserialize( data, true );
		}
		
		public virtual void Deserialize(Dictionary<string,object> data, bool setInitializedToTrue )
		{
			id = data["Id"].ToString();
			
			foreach( string k in data.Keys )
			{
				switch( k )
				{
				case "_recordType":
					Record = AbstractRecord.Load( TypeLoader.GetType( data["_recordType"].ToString() ), data["_recordId"].ToString() ) ;
					break;
				case "_recordId":
					break;
				case "_children":
					IList list = data["_children"] as IList;
					foreach( object o in list )
					{
						if( o is System.Collections.Generic.Dictionary<string,object> )
						{
							Dictionary<string,object> d = o as Dictionary<string,object>;
							log.Debug(string.Format("adding child {0} to {1}", d["Id"], id ) );
							Widget w = Widget.StaticDeserialize( d );
							if( w != null )
								Add( w );
							else
								log.Error( "failed to deserialize child",data);
						}
						else if( o is Widget )
						{
							Add(o as Widget );
						}
						else
							log.Error("oops - o is not a dict<string,object>", o);
					}
					break;
				default:
					this[k] = data[k];
					
					break;
				}
			}
			if( setInitializedToTrue )
				initialized = true;
			Swizzle();
		}
		
		protected string GetMethodNameForEvent( string name )
		{
			Type t= this.GetType();
			EventInfo ei = t.GetEvent(name);
			MethodInfo mi = ei.GetRaiseMethod();
			if( mi != null )
				return mi.Name;
			else
				return null;
		}
		
		public static Widget StaticDeserialize( Dictionary<string,object> data )
		{
			Type t = TypeLoader.GetType( data["_type"].ToString() );
			if( t == null )
			{
				log.Debug( "could not deserialize type " + data["_type"] );
				return null;
			}
			try
			{
				Widget w = Context.Current.CreateUnkownWidget( t, null, null, null, false, false );
				w.IsDeserializing = true;
				w.Deserialize( data );
				w.IsDeserializing = false;
				return w;
			}
			catch( Exception e )
			{
				log.Error( "Could not deserialize type " + t, Util.BuildExceptionOutput( e ));
			}
			return null;
		}

        public List<T> GetChildrenByType<T>() where T : class
        {
            return GetChildrenByType<T>(true);
        }

        public List<T> GetChildrenByType<T>(bool recursive) where T : class
        {
            List<T> l = new List<T>();
            GetChildrenByType<T>(l, recursive);
            return l;
        }

        public void GetChildrenByType<T>(List<T> list) where T : class
        {
            GetChildrenByType<T>(list, true);
        }

        public void GetChildrenByType<T>(List<T> list, bool recursive ) where T : class
        {
            if (list == null || Widgets == null || Widgets.Count == 0 ) return;
            foreach (Widget c in Widgets)
            {
                if (typeof(T).IsInstanceOfType(c))
                {
                    list.Add(c as T);
                }
                if (recursive) c.GetChildrenByType(list, recursive);
            }
        }
       
		private int onDelayLength = 350;
		public int OnDelayLength
		{
			get
			{
				return onDelayLength;
			}
			set
			{
				onDelayLength = value;
				this.SetClientProperty( "onDelayLength", value.ToString() );
			}
		}

        private event EventHandler<DelayedMouseEventArgs> onDelayedMouseOver;
        public event EventHandler<DelayedMouseEventArgs> OnDelayedMouseOver
        {
            add { onDelayedMouseOver += value; ClientArguments["onDelayedMouseOver"] = "1"; }
            remove { onDelayedMouseOver -= value; }
        }

        private event EventHandler<DelayedMouseEventArgs> onDelayedMouseOut;
        public event EventHandler<DelayedMouseEventArgs> OnDelayedMouseOut
        {
            add { onDelayedMouseOut += value; ClientArguments["onDelayedMouseOut"] = "1"; }
            remove { onDelayedMouseOut -= value; }
        }

        private event EventHandler<DragAndDropEventArgs> onReceiveDrop;
        public event EventHandler<DragAndDropEventArgs> OnReceiveDrop
        {
            add { onReceiveDrop += value; ClientArguments["onReceiveDrop"] = "1"; }
            remove { onReceiveDrop -= value; }
        }

        private event EventHandler<ClickEventArgs> onClick;
        public event EventHandler<ClickEventArgs> OnClick
        {
            add {
            	onClick += value; 
            	ClientArguments["onClick"] = "1"; 
            }
            remove { onClick -= value; }
        }

		private event EventHandler<KeyPressEventArgs> onKeyPress;
        public event EventHandler<KeyPressEventArgs> OnKeyPress
        {
            add {
            	onKeyPress += value; 
            	ClientArguments["onKeyPress"] = "1"; 
            }
            remove { onKeyPress -= value; }
        }
        
        private event EventHandler<WidgetEventArgs> onBlur;
        public event EventHandler<WidgetEventArgs> OnBlur
        {
            add { 
            	onBlur += value; ClientArguments["onBlur"] = "1"; }
            remove { onBlur -= value; }
        }

        #region IDataBindable Members

        virtual public object Value
        {
            get
            {
                return Id;
            }
            set
            {
                Id = value.ToString();
            }
        }

		string defaultProperty = "Id";
        virtual public string DefaultProperty
        {
            get { return defaultProperty; }
            set { defaultProperty = value; }
        }

        public virtual string TagPrefix {
        	get {
        		return tagPrefix;
        	}
        	set {
        		tagPrefix = value;
        	}
        }

		Widget dragSource;
        public virtual Widget DragSource {
        	get {
        		return dragSource;
        	}
        	set {
        		if( value != null )
        		{
        			SetClientAttribute("isDragItem",1);
        			InvokeClientMethod("MakeDragSource",value.ClientId);
        		}
        		dragSource = value;
        	}
        }

		string dndType;		
		public virtual string DndType {
        	get {
        		return dndType;
        	}
        	set {
        		if( value != null )
        		{
        			ClientArguments["dndType"] = Util.ToJavaScriptString( value );
        		}
        		dndType = value;
        	}
        }

		string dndAccept;		
		public virtual string DndAccept {
        	get {
        		return dndAccept;
        	}
        	set {
        		if( value != null )
        		{
        			ClientArguments["dndAccept"] = Util.ToJavaScriptString( value );
        			ClientArguments["onReceiveDrop"] = "1";
        		}
        		dndAccept = value;
        	}
        }

        public bool WidgetDataBound {
        	get {
        		return dataBound;
        	}
        	set {
        		dataBound = value;
        	}
        }

        public bool Enabled {
        	get {
        		return enabled;
        	}
        	set {
				enabled = value;			
				RemoveClass(!enabled ? "enabled" : "disabled");        		
				AppendClass(enabled ? "enabled" : "disabled");
				SetClientElementProperty("disabled", enabled ? "false":"true");
        	}
        }

        public bool Debugging {
        	get {
        		return debugging;
        	}
        	set {
        		debugging = value;
        	}
        }

        public Permission Permission {
        	get {
        		return permission;
        	}
        	set {
        		permission = value;
        		RaisePropertyChangedNotification("Permission");
        		this.Visible = false;
        		RootContext.EnsureAccess( permission, 
        			delegate( object sender, EventArgs ea )
        			{
        				this.Visible = true;
        			}
        		);
        	}
        }

        public String VisibleTo
        {
            get { return this.visibleTo != null ? this.visibleTo.Name : String.Empty; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    return;
                }                
                this.VisibleToPermission = Permission.GetPermission(value);
				this.RaisePropertyChangedNotification("VisibleTo");
            }
        }

		public Permission VisibleToPermission
		{
			get
			{
				return this.visibleTo;
			}
			set
			{
				visibleTo = value;

				if( value == null )
				{					
					return;
				}

				if (this.RootContext != null)
                {
                    if (this.RootContext.CurrentUser == null)
                    {
                        this.Visible = false;
                    }
                    else
                    {
                        bool hasPermission = this.RootContext.CurrentUser.CheckPermission(this.visibleTo);
                        Visible = Visible && hasPermission;
                    }
                }
			}
		}
		
		public String NotVisibleTo
        {
            get { return this.notVisibleTo != null ? this.notVisibleTo.Name : String.Empty; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    return;
                }                
                this.NotVisibleToPermission = Permission.GetPermission(value);
				this.RaisePropertyChangedNotification("NotVisibleTo");
            }
        }

		public Permission NotVisibleToPermission
		{
			get
			{
				return this.notVisibleTo;
			}
			set
			{
				notVisibleTo = value;

				if( value == null )
				{					
					return;
				}

				if (this.RootContext != null)
                {
                    if (this.RootContext.CurrentUser != null)
                    {
                        bool hasPermission = this.RootContext.CurrentUser.CheckPermission(this.notVisibleTo);
                        Visible = Visible && !hasPermission;
                    }
                }
			}
		}

        public override string ToString()
        {
			string r = string.Format("[Widget id: {1} client id: {4}: default property:\"{3}\" name: ({2}) Visible: {5} Rendered: {6}]", 
        		null, 
        		this.id, //1
        		this.Name,  //2
        		this[DefaultProperty], //3 
        		this.ClientId, //4
        		this.visible, //5
        		this.rendered );
        	r = r.Replace("{","{{");
        	r = r.Replace("}","}}");
        	return r;
        }
        
        public void Log(string message, params object[] args )
        {
        	if( args != null && args.Length > 0 )
        		message = string.Format( message, args ); 
       		SendCommand( string.Format( "elog({0});", Util.ToJavaScriptString( message ) ) );
        }

        #endregion

        public virtual event EventHandler<ChangedEventArgs> OnChanged;

        protected void InvokeChangedEvent(object oldValue, object newValue)
        {
            if( OnChanged != null )
                OnChanged(this, new ChangedEventArgs( this, oldValue, newValue ) );
        }
    }
}
