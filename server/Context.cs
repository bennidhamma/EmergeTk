using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;
using System.Xml;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;
using System.Linq;

namespace EmergeTk
{
	public enum ContextState
	{
		Uninitialized,
		Initializing,
		Running,
		Finishing
	}

	/// <summary>
	/// Summary decription for Kontext.
	/// </summary>
	public class Context : Pane
	{
		private new static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Context));
		
		private static Dictionary<string,Context> activeContexts = new Dictionary<string, Context>();
		
        private string _clientClass = "Pane";
		private DateTime lastKeepAlive = DateTime.Now; 
		
        public override string ClientClass {
        	get { return _clientClass; }
        	set { _clientClass = value; }
        }
        
        bool requiresTransformation = false;
        public bool RequiresTransformation {
        	get {
        		return requiresTransformation;
        	}
			set {
				if( requiresTransformation != value )
				{
					requiresTransformation = value;
				}				
			}
        }

        public static Context GetContext(string SessionID, string ContextName)
        {
			if( activeContexts.ContainsKey(SessionID) )
				return activeContexts[SessionID];
			/*
            if ( HttpContext.Current.Session[SessionID] != null )
            {
                Context c = HttpContext.Current.Session[SessionID] as Context;
                if (c != null && c.registered)
                    return c;
            }
            */
            return null;
        }

        bool documentFooterOverridden = false;
        private string documentFooter;
        public string DocumentFooter
        {
            get
            {
                if (documentFooterOverridden)
                    return documentFooter;
                else
                    return "\r\nsendBookmark(); if( emerge_post_load ) emerge_post_load(); } dojo.addOnLoad( emerge_load );</script>\r\n" + FooterHtml + "\r\n    </body>\r\n</html>";
            }
            set
            {
                documentFooter = value;
                documentFooterOverridden = true;
            }
        }

        public event EventHandler<UserEventArgs> OnLogIn;
        public event EventHandler<UserEventArgs> OnLogOut;
        public event EventHandler<UserEventArgs> OnPermissionsChanged;

        private User currentUser;

        protected string DocumentHeader;
        private bool buildHeader = true;
        public string DefaultNamespace = "EmergeTk.Widgets.Html";
        private string baseElement = "document.body";
        private bool dataBindPostInit = false;
        string bookmark;
        public override string BaseElement { get { if( baseElement == null ) return base.BaseElement; else return baseElement; } set { baseElement = value; } }
        protected new event EventHandler<MouseEventArgs> OnClick; // Possible naming issues/bug with inherited OnClick from Widget.cs
        protected CometClient cometClient;
        private bool registered = false;
        private string sessionId;
        private bool isBot = false;
        private List<ContextHistoryFrame> history;
        private int currentFrame = 0;
		private bool useFriendlyClientIds = true;
		private string title;
		
        private Dictionary<string, int?> typeCounts = new Dictionary<string, int?>();

		protected ContextState State = ContextState.Uninitialized;
		public HttpContext HttpContext
		{
			get { return System.Web.HttpContext.Current; }
		}
		
		HttpServerUtility server;
		public HttpServerUtility Server
		{
			get {
				return server;
			}
		}

        public bool DataBindPostInit { get { return dataBindPostInit; } set { dataBindPostInit = value; } }

        string host;
        public string Host
        {
            get { return host; }
            set { host = value; }
        }
        
        
        string theme;
        public string Theme {
        	get {
        		if( theme == null )
        		{
        			if( System.Configuration.ConfigurationManager.AppSettings["Theme"] != null )
        			{
        				theme = System.Configuration.ConfigurationManager.AppSettings["Theme"];
        			}
        			else
        			{
        				theme = "Default";
        			}
        		}
        		return theme;
        	}
        	set {
        		theme = value;
        	}
        }

        public CometClient CometClient
        {
            get { return cometClient; }
        }

        public bool IsBot
        {
            get { return isBot; }
        }

        public User CurrentUser
        {
            get { return this.currentUser; }
            set {
            	this.currentUser = value;
            	RaisePropertyChangedNotification("CurrentUser");
            }
        }

        public bool IsLoggedIn
        {
            get { return this.currentUser != null; }
        }

        public bool LogIn(string username, string password)
        {
            User u = User.AuthenticateUser(username, password);

            if (this.currentUser != null)
            {
                this.LogOut();
            }

            if (u != null)
            {
            	LogIn( u );
                return true;
            }

            return false;
        }
        
        public void LogIn( User u )
        {
         	this.currentUser = u;
            this.currentUser.GenerateAndSetNewSessionToken();
            this.currentUser.Save();
            User.RegisterActiveUser( u );

            // TODO: use web.config settings to set if we should set a cookie and how long before it expires
            // TODO: also need to set a constant for the cookie name
			u.SetLoginCookie();
            
            if (this.OnLogIn != null)
            {
                this.OnLogIn(this, new UserEventArgs(this.currentUser));
            }
        }

        public void LogOut()
        {
        	User.UnregisterActiveUser( this.currentUser );
            this.CurrentUser = null;

            this.HttpContext.Response.Cookies["LoginToken"].Value = String.Empty;

            if (this.OnLogOut != null)
            {
                this.OnLogOut(this, new UserEventArgs(this.currentUser));
            }
        }

        public void EnsureAccess( Permission permission, EventHandler successCallback )
        {
        	EnsureAccess( permission, successCallback, null );
        }

		bool ensuringAccess = false;
        public void EnsureAccess( Permission permission, EventHandler successCallback, EventHandler failCallback )
		{
			if( ensuringAccess )
				throw new Exception("Already ensuring access");
	 		ensuringAccess = true;
			if( successCallback == null )
			{
				log.Error( "EnsureAccess invoked with no successCallback");
				return;
			}
			
			if( CurrentUser != null && permission == null )
			{
				successCallback(this, null);
				ensuringAccess = false;
				return;
			}
			
			if( CurrentUser != null && CurrentUser.CheckPermission( permission ) )
			{
				successCallback(this, null);
				ensuringAccess = false;
				return;
			}
			//insufficient permissions
			RequestPermission rp = CreateWidget<RequestPermission>(this);
			rp.RequestedPermission = permission;
			rp.OnAuthSuccess += successCallback;
			rp.OnAuthSuccess += delegate { ensuringAccess = false; };
			rp.OnAuthCancel += delegate { ensuringAccess = false; };
			rp.OnAuthFailed += delegate { ensuringAccess = false; };
			if( failCallback != null )
				rp.OnAuthFailed += failCallback;
		}

        public string FooterHtml
        {
            get
            {
				//TODO: Is this necessary?  
                if (File.Exists(Util.RootPath + "/" + this.GetType().Name + "_footer.html"))
                {
                    StreamReader sr = File.OpenText(this.GetType().Name + "_footer.html");
                    string result = sr.ReadToEnd();
                    sr.Close();
                    
                    return result;
                }
                else
                    return string.Empty;
            }
        }
        
        private string extendedHead = string.Empty;
        public string ExtendedHead
        {
        	get { return extendedHead; }
        	set { extendedHead = value; }
        }

        private bool isProxyContext = false;
        public bool IsProxyContext
        {
        	get { return isProxyContext; }
        	set { isProxyContext = value; }
        }
        
        private string proxyIdPrefix;
        public string ProxyIdPrefix
        {
        	get { return proxyIdPrefix; }
        	set { proxyIdPrefix = value; }
        }
        
        private Surface renderSurface;
        public Surface Surface
        {
        	get { return renderSurface; }
        	set { renderSurface = value; }
        } 
        
        private NotificationArea notificationArea;
        public NotificationArea NotificationArea {
        	get {
        		if( notificationArea == null )
        		{
        			notificationArea = RootContext.Find<NotificationArea>();
        			if( notificationArea == null )
        			{
        				notificationArea = RootContext.CreateWidget<NotificationArea>(this);
        				notificationArea.Init();
        			}
        		}
        		return notificationArea;
        	}
        	set {
        		notificationArea = value;
        	}
        }
		
		public Context():this(false)
		{
		}

       
        public Context( bool isProxyContext )
        {
			this.isProxyContext = isProxyContext;
			
			RootContext = this;
			if( ! isProxyContext )
			{
                sessionId = Util.GetBase32Guid();
                server = HttpContext.Server;
		    
	      		if( HttpContext.Request.ServerVariables["HTTP_USER_AGENT"] != null )
	       			isBot = HttpContext.Request.ServerVariables["HTTP_USER_AGENT"].Contains("Googlebot");
			}
        }
        
        private void setupHeader()
        {
//  <!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">

			char sep = System.IO.Path.DirectorySeparatorChar;
			string headerFormat = ThemeManager.Instance.RequestFileAsString("Views" + sep + this.GetType().FullName.Replace('.',sep) + ".documentHeaderFormat");
			if( headerFormat == null )
			{
				headerFormat = 
@"<!DOCTYPE HTML>
<html>
	<head>
	    <title>5to1</title>
		{0}
		<script type=""text/javascript"">
			document.domain = '{1}';
			sendUrl = '{5}';			
		</script>
		<script src=""{2}"" type=""text/javascript""></script>
		{7}
		{4}
		{6}
	</head>
	<body class=""tundra"">
	<script>
	</script>	
	<script type=""text/javascript"">
		checkForBookmark();
		function emerge_load() {{
			";
			}
			
		DocumentHeader = string.Format( headerFormat ,
		ThemeManager.Instance.RequestStylesBlock(this.GetType().FullName.Replace('.',System.IO.Path.DirectorySeparatorChar)),//0
		HttpContext.Request.Url.Host,//1
		ThemeManager.Instance.RequestScriptPath("dojo/dojo/dojo.js"),//2
		null,//3
        ThemeManager.Instance.RequestScriptsBlock(this.GetType().FullName.Replace('.',System.IO.Path.DirectorySeparatorChar)),//4
    	Setting.VirtualRoot + '/' + (this.GetType().Namespace != null ? this.GetType().Namespace + "/" : "" ).Replace('.','/') + this.GetType().Name,//5
    	extendedHead,//6
        "",//7
        Setting.VirtualRoot //8
			) ;
        }

        public string CacheKey 
        {
        	get
        	{
        		return buildCacheKey(this.Name); 
        	}
        }
        
        public virtual bool UseFriendlyClientIds {
        	get {
        		return useFriendlyClientIds;
        	}
        }


        public string Title 
        {
        	get {
        		return title;
        	}
			set {
				title = value;
				SendCommand("SetTitle({0}); ", Util.ToJavaScriptString(title) );
			}
        }

		private string buildCacheKey(string name) 
        {
        	return (sessionId + "_" + name);
        }

		public void Register( string name )
		{
			this.Name = name;
            this.host = HttpContext.Request.Url.Host;
            //HttpContext.Current.Session[sessionId] = this;
			activeContexts[sessionId] = this;
            this.registered = true;
			Parse();
		}


        public event EventHandler<ContextEventArgs> OnUnload;
        public event EventHandler<ContextEventArgs> OnPostTransform;
        public event EventHandler<CometEventArgs> CometConnected;

        public void ConnectComet(CometClient cc)
        {
            this.cometClient = cc;
            if (CometConnected != null)
                CometConnected(this, new CometEventArgs() );
        }

        public void DisconnectComet()
        {
            cometClient = null;
        }
        
        bool twoWayComet = false;
        public bool TwoWayComet {
        	get {
        		return twoWayComet;
        	}
        	set {
        		if( twoWayComet != value )
        		{
        			twoWayComet = value;
        			this.RawCmd("doTwoWayComet({0});",value.ToString().ToLower());
        		}
        	}
        }        

		public override void Unregister()
		{    
			log.Info("Unregistering Context ", this);
            if( OnUnload != null )
            {
            	//System.Console.WriteLine("unregistering context.");
                OnUnload(this, new ContextEventArgs());
            }
			activeContexts.Remove(sessionId);
			if (this.cometClient != null)
            {
                this.cometClient.Shutdown();
            }
            this.registered = false;
            this.State = ContextState.Uninitialized;
            
            if( currentUser != null )
            {
            	User.UnregisterActiveUser( currentUser );
            }
			
			base.Unregister();
		}

        public virtual void SocketDisconnected()
        {

        }

		public override bool Render( Surface surface )
		{
            if (this == this.RootContext)
            {
            	if( ! ( surface is CometClient ) )
            	{
	                HttpContext.Response.Cache.SetExpires(DateTime.MinValue);
	                HttpContext.Response.ContentType = "text/html";
	                if ( this.State == ContextState.Initializing && this.OnClick != null)
	                {
	                    surface.Write("document.addEventListener('click', DocumentOnClick, true );");
	                }
	            }

                RecurseRender(this, surface);

                if ( this.State == ContextState.Running && surface.BytesSent == 0)
                {
                    //quick fix to deal with no element error.
                    surface.Write("/*NO OP*/");
                }
            }
            else
            {            	
                base.Render(surface);
            }
            return true;
		}

        protected string contextPassThroughId(Widget w)
        {
            return w.Parent.UID;
        }

		public void RemoveWidget( Widget c )
		{
			if( c.rendered ) SendCommand( "{0}.Remove();", c.ClientId );
		}

		public void HideWidget( Widget c )
		{
			if( c.rendered ) SendCommand( "{0}.Hide();", c.ClientId );
		}

		public void ShowWidget( Widget c )
		{
			if( c.rendered )
			{
				SendCommand( "{0}.Show();", c.ClientId );
			}
			else if( c.VisibleToRoot && c.Parent != null && ( c.Parent.rendered || c.Parent == RootContext ) )
			{
				RenderWidget( c );
			}
		}

		public void RenderWidget( Widget c )
		{
            if (this != Context.Current && CometClient != null)
				this.RequiresTransformation = true;
			if( ClientEvents == null )
				ClientEvents = new Queue();
			ClientEvents.Enqueue( c );
		}

        public void AddFrameToHistory(ContextHistoryFrame frame)
        {
            if (history == null)
            {
                history = new List<ContextHistoryFrame>();
            }
            if( currentFrame < history.Count - 1 )
            {
                history.RemoveRange( currentFrame + 1, history.Count - (currentFrame+1) );
            }
            history.Add(frame);
            currentFrame = history.Count - 1;
           	SendCommand("AddBack(" + Util.ToJavaScriptString(frame.Id) + ");");
        }
        
         
        string bookmarkOnClient;
        public void SetBookmark( string bookmark )
        {
        	log.DebugFormat("setting bookmark from {0} to {1}", bookmarkOnClient, bookmark );
        	if( bookmark == bookmarkOnClient )
        		return;        	
			this.bookmarkOnClient = this.bookmark = bookmark;
        	RawCmd("setBookmark({0});", Util.ToJavaScriptString(bookmark) );
        }

		public void Reload()
		{
			RawCmd("document.location.reload();");
		}

   		public static void Connect( string name, System.Type type )
		{
			Context c = null;
			bool restored = false;
			
			if( HttpContext.Current.Request.QueryString["widget"] != null )
			{
				HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
				if( HttpContext.Current.Request.QueryString["sid"] == null )
					throw new Exception("we need a session id.");
				string sessionId = HttpContext.Current.Request.QueryString["sid"]; // HttpContext.Current.Server.UrlDecode(HttpContext.Current.Request.QueryString["sid"]); 
            	c = GetContext(sessionId, name);
            	CheckUserCookie(c);
            	if( c == null )
				{					
					log.Debug("Session not present, attempting to restore.");
					if( HttpContext.Current.Request.QueryString["event"] == "Restore" )
					{
						string data = HttpContext.Current.Request["arg"];
						if( data != null )
						{
							log.Debug("deserializing with data ", data);
							c = JSON.Default.Decode( data ) as Context;
							c.sessionId = sessionId;
							c.registered = true;
							HttpContext.Current.Session[sessionId] = c;
							restored = true;
							c.buildHeader = false;
                			c.DocumentFooter = "handleBookmark();if( emerge_post_load ) emerge_post_load();";
						}
					}
					else
					{
						HttpContext.Current.Response.Write("restore();");
						return;
					}
				}
            }
			if( c == null )
			{
                if (type != null)
                {
                	c = Activator.CreateInstance( type ) as Context;
                }
                else
                {
                    c = new Context();
                }
                HttpContext.Current.Items["context"] = c;
                CheckUserCookie(c);
                c.bookmarkOnClient = HttpContext.Current.Request.QueryString["bookmark"]; 
                c.Bookmark = c.bookmarkOnClient;	
				c.Register(name);
			}
			if( HttpContext.Current.Request.QueryString["baseElem"] != null || restored )
            {
            	c.BaseElement = string.Format("dojo.byId('{0}')", HttpContext.Current.Request["baseElem"]);
            	c.buildHeader = false;
            	c.DocumentFooter = "handleBookmark();if( emerge_post_load ) emerge_post_load();";
            }
            
            HttpContext.Current.Items["context"] = c;
						
            CheckUserCookie(c);

			/*int sqlExecCount = MySqlProvider.Provider.ExecutionCount;
			int sqlSelectCount = MySqlProvider.Provider.SelectCount;
			int sqlReplaceCount = MySqlProvider.Provider.ReplaceCount;
			
			float hits = AbstractRecord.Hits;
			float misses = AbstractRecord.Misses;
			float singulars = AbstractRecord.SingularHits;
			float plurals = AbstractRecord.PluralHits;			
		
			log.Debug("transforming ", 
				c.sessionId, ",", 
				HttpContext.Current.Request.Url, 
				" CurrentUser:", c.CurrentUser);*/
			c.Transform();
			/*string counts = string.Format( 
				@"done w/transform sql execs: {0}, selects: {1}, replaces {2}, cache hits: {3}, singluars: {4}, plurals: {5}, misses {6}", 
				MySqlProvider.Provider.ExecutionCount - sqlExecCount,
				MySqlProvider.Provider.SelectCount - sqlSelectCount,
				MySqlProvider.Provider.ReplaceCount - sqlReplaceCount,
				
				AbstractRecord.Hits - hits,
				AbstractRecord.SingularHits - singulars,
				AbstractRecord.PluralHits - plurals,
				AbstractRecord.Misses - misses
			);
			
			log.Debug(counts);*/
		}

        private static void CheckUserCookie(Context c)
        {
			try
			{
	            if (!c.IsLoggedIn)
	            {
	                HttpCookie cookie = HttpContext.Current.Request.Cookies["LoginToken"];
	                if (cookie != null && !String.IsNullOrEmpty(cookie.Value))
	                {
	                	c.currentUser = User.FindBySessionToken( cookie.Value );
	                    if (c.currentUser != null && c.OnLogIn != null)
	                    {
	                        c.OnLogIn(c, new UserEventArgs(c.CurrentUser));
	                    }
	                }
	            }
			} 
			catch (Exception e)
			{
				log.Warn("Unable to CheckUserCookie: " + e);
			}
				
	
        }

		public void Transform()
		{
			string evt = HttpContext.Request["event"];
			string args = null;
			if( HttpContext.Request["arg"] != null )
			{
               args = HttpContext.Request["arg"];
			}
			string widgetKey = HttpContext.Request["widget"];
			Transform( widgetKey, evt, args);
		}
		
		Widget lastEventWidget;
		public Widget LastEventWidget
		{
			get {
				if( lastEventWidget == null )
					lastEventWidget = this;
				return lastEventWidget; }
			set { lastEventWidget = value; }
		}
		
		Queue<string> logBuffer;
		public Queue<string> LogBuffer
		{
			get { return logBuffer; }
			set { logBuffer = value; }
		}
		
		public void Transform(string widgetKey, string evt, string args)
		{
			DateTime transformStart = DateTime.Now;
			log.Debug("transforming", widgetKey, evt, args );
			if( ! this.IsProxyContext )
			{
				if( CometClient != null )
				{
					renderSurface = CometClient;
				}
				else if( HttpContext.Current != null && HttpContext.Current.Request.Files.Count > 0 )
				{
					renderSurface = new HtmlSurface(HttpContext);
				}
				else			
					renderSurface = new HttpSurface(HttpContext);
			}
				
			//process order of events.
			if( State == ContextState.Uninitialized )
			{
            	RawCmd("setSessionId({0});", Util.ToJavaScriptString( HttpContext.Server.UrlEncode(sessionId ) ) );
                this.Init();
				this.State = ContextState.Initializing;
                if( dataBindPostInit ) DataBindWidget();
			}
			else
			{
				//process events.				
				if( widgetKey == null )
				{
					//oops -lost state.
					Unregister();
					throw new Exception("no widget was found.  something went wrong trying to transform this context.");                    
				}
				
				bool eventHandled = false;
				
				try
				{
					//string args = HttpContext.Request["arg"];
					//TODO: do we need to support proxy roots?
					if( widgetKey == "root" )
					{
						this.HandleEvents( evt, args );
						eventHandled = true;
					}
					else
					{
						Widget c = null;
						string subKey = null;
						if( widgetKey.Contains("/") )
						{
							string[] keyParts = widgetKey.Split('/');
							if( keyParts.Length == 0 )
								throw new Exception(string.Format("bad widget key {0}", widgetKey ));
							widgetKey = keyParts[0]; 
							if( keyParts.Length == 2 )
								subKey = keyParts[1];
							else if( keyParts.Length > 2 )
								subKey = Util.Join( keyParts, "/", false, 1 ); 
						}
		
						if( clientIdWidgetHash.ContainsKey(widgetKey ) )
						{
							c = clientIdWidgetHash[widgetKey];
						}
						else
						{
							c = Widgets.Find( widgetKey );
							log.Error( this + "Couldn't find any widget with key " + widgetKey + " printing widget hash:");
						}
							
						if( c != null )
						{
							c.HandleEvents( subKey, evt, args );
							lastEventWidget = c;
							eventHandled = true;
						}
	                    else if (widgetKey.IndexOf('_') >= 0)
	                    {
	                        string[] argParts = widgetKey.Split('_');
	                        int index = argParts.Length -1;
	                        while( c == null && index >= 0 )
	                            c = Widgets.Find(Util.Join(argParts,"_",false,0,index--));
	                        if (c != null)
	                        {
	                            c.ClientArguments["widgetKey"] = widgetKey;
	                            c.HandleEvents(subKey, evt, args);
	                            eventHandled = true;
	                            lastEventWidget = c;
	                        }
	                    }
	                    if( ! eventHandled )
	                    {
	                    	throw new Exception(string.Format("event unhandled: {0}, {1}, {2} ", widgetKey, evt, args ) );
	                    }
					}
				}
				catch(UserNotifiableException ex)
				{
					log.Error(ex);
					if(null != Context.Current)
					{
						Context.Current.SendClientNotification(ex.Message);	
					}
				}				
			}
			if( State == ContextState.Initializing && ! this.IsProxyContext && buildHeader )
			{
				setupHeader();
				HttpContext.Response.Write(this.DocumentHeader.Replace("<ExtendedHead/>",extendedHead));
			}
			
			if( HttpContext.Request["data"] == null || Convert.ToBoolean(HttpContext.Request["data"]) == false )
			{
				renderSurface.Start();
				Render( renderSurface );
				renderSurface.End();
			}
			
			if( State == ContextState.Initializing )
			{
				if( ! this.IsProxyContext )
				{
					HttpContext.Response.Write(this.DocumentFooter);
				}
				State = ContextState.Running;
			}
			if( OnPostTransform != null )
				OnPostTransform( this, new ContextEventArgs() );
				
			log.InfoFormat("Context Transform took {0}ms", (DateTime.Now - transformStart).TotalMilliseconds);

            StopWatch.Summary(log);
		}
		
		public void RawCmd(string cmd, params object[] args)
		{
			RawCmd( string.Format( cmd, args ) );
		}
		
		public void RawCmd(string cmd)
		{
			if( cometClient != null )
			{
				cometClient.Write( cmd );
			}
			else if( renderSurface != null )
			{
				renderSurface.Write(cmd);
			}
		}
		
		public void Log( string msg )
		{
			RawCmd("console.log('{0}');", msg );
		}
		
		bool bookmarkVisited = false;

		public override void HandleEvents( string evt, string args )
		{
			switch( evt )
			{
			case "OnClick":
				string[] argsArray = args.Split(',');
				int X = int.Parse( argsArray[0] );
				int Y = int.Parse( argsArray[1] );
				this.OnClick( this, new MouseEventArgs( X, Y ) );
				break;
			case "Back":
                if (history != null && currentFrame > 0)
                {
                    ContextHistoryFrame frame = history[--currentFrame];
                    frame.CallBack(frame.State);
                }
				break;
			case "Forward":
                if (history != null && currentFrame < history.Count-2)
                {
                    ContextHistoryFrame frame = history[++currentFrame];
                    frame.CallBack(frame.State);
                }
				break;
			case "OnBookmark":
			case "InitialBookmark":
				this.bookmark = args;
				bookmarkOnClient = this.bookmark;
				if( evt == "InitialBookmark" && bookmarkVisited )
				{
					this.Reload();
				}
				bookmarkVisited = true;
          		if( OnBookmark != null )
          			OnBookmark( this, new BookmarkEventArgs( args ) );
          		break;
          	case "Expire":
          		RawCmd("setExpireState({0});", JSON.Default.Encode( Serialize() ) );
          		break;
			case "KeepAlive":
				this.lastKeepAlive = DateTime.Now;
				break;
          	}
            base.HandleEvents( evt, args );
		}
		
		public event EventHandler<BookmarkEventArgs> OnBookmark;

        public Context CreateDynamicContext(string name, XmlNode xml)
        {
            return CreateContext<Context>(name, xml);
        }

        public T CreateContext<T>() where T : Context, new()
        {
            return CreateContext<T>(null);
        }

        public T CreateContext<T>(string name) where T : Context, new()
        {
            return CreateContext<T>(name, null);
        }
        
        public T CreateContext<T>(string name, XmlNode xml) where T : Context, new()
        {
        	return CreateContext<T>(name,xml,null,null,true);
        }

        public T CreateContext<T>(string name, XmlNode xml, Widget parent, AbstractRecord record, bool parseXml) where T : Context, new()
        {
            T context = CreateWidget<T>();
            context.Id = context.Name = name == null ? typeof(T).FullName.Replace(".","_") : name;            
            context.RootContext = this;
            (context as Context).baseElement = null;
            context.TagPrefix = this.TagPrefix;
            if( record != null )
            	context.Record = record;
            if( parent != null )
            {
            	parent.Add( context );
            }

			if( parseXml )
            	context.Parse(xml);           
            return context;
        }

		int widgetCount;
		public string GetNewClientId(Widget w)
		{
			if( this.RootContext != this )
			{
				throw new Exception(string.Format("{0} does not equal {1}!", this, this.RootContext));
			}
			string id;
			id = Util.ConvertToBase32(this.RootContext.widgetCount++);
			if( id.Length < 3 )
				id = "_" + id;
			if( this.isProxyContext )
				id = string.Format("{0}/{1}", this.proxyIdPrefix, id ); 
			w.ClientId = id;
			//SetClientId(w, id);
						
			return id;
		}
		
		public void SetClientId( Widget w, string id) 
		{
			//log.Debug( this  + " setting widget hash id " + id + " to: "  + w );	
			clientIdWidgetHash[id] = w;
		}
		
		public Widget GetWidgetByClientId( string id )
		{
			if( clientIdWidgetHash.ContainsKey( id ) )
				return clientIdWidgetHash[id];
			return null;
		}
		
		public void UpdateServerId(Widget w, string oldId, string newId )
		{
			if( oldId != null && serverIdWidgetHash.ContainsKey(oldId) )
				serverIdWidgetHash.Remove(oldId);
			if( newId != null )
			{
				serverIdWidgetHash[newId] = w ;
				if( widgetIdListeners != null && widgetIdListeners.ContainsKey(newId) )
				{
					widgetIdListeners[newId](w,null);
				}
			}
		}
		
		Dictionary<string,EventHandler> widgetIdListeners;
		
		public void ListenForWidgetId( string id, EventHandler eh )
		{
			if( widgetIdListeners == null )
				widgetIdListeners = new Dictionary<string,EventHandler>();
			if( ! widgetIdListeners.ContainsKey(id) )
				widgetIdListeners.Add(id, eh);
			else
				widgetIdListeners[id] += eh;
		}
		
		Dictionary<string,Widget> clientIdWidgetHash = new Dictionary<string,Widget>();
		Dictionary<string,Widget> serverIdWidgetHash = new Dictionary<string,Widget>();

		public T CreateWidget<T>() where T : Widget, new()
		{
			return CreateWidget<T>(null, null, null);
		}
		
		public T CreateWidget<T>(XmlNode xml) where T : Widget, new()
		{
			return CreateWidget<T>(null, null, xml);
		}
				
		public T CreateWidget<T>(Widget parent) where T : Widget, new()
		{
			return CreateWidget<T>(parent, null, null);
		}

		public T CreateWidget<T>(AbstractRecord record) where T : Widget, new()
		{
			return CreateWidget<T>(null, record, null);
		}
		
		public T CreateWidget<T>(Widget parent,AbstractRecord record) where T : Widget, new()
		{
			return CreateWidget<T>(parent, record, null);
		}
		
		public T CreateWidget<T>(Widget parent,AbstractRecord record, XmlNode xml) where T : Widget, new()
		{
			return CreateWidget<T>(parent, record, xml, true, true );
		}
		
        public T CreateWidget<T>
			(Widget parent,
			 AbstractRecord record,
			 XmlNode xml,
			 bool parseXml,
			 bool addToParent ) where T : Widget, new()
        {
            T newWidget = new T();

			Type type = typeof(T);
			if( type.IsGenericType )
			{
				newWidget.currentGenericType = type.GetGenericArguments()[0];
			}
			
            newWidget.RootContext = this.RootContext ?? this ;
            if( record != null )
            	newWidget.Record = record;

            //default id
            int? count = 0;
            string name = newWidget.ClientIdBase;
            if (type.IsGenericType && name.Contains("`"))
            {
                name = name.Substring(0, name.IndexOf('`'));
                foreach( Type t in type.GetGenericArguments() )
                {
                    name += t.Name;
                }
            }
            if (RootContext.typeCounts.ContainsKey(name))
            {
                count = RootContext.typeCounts[name];
            }
            if (String.IsNullOrEmpty(newWidget.Id))
            {
                newWidget.Id = name + count++;
            }
            serverIdWidgetHash[ newWidget.Id ] = newWidget;
            RootContext.typeCounts[name] = count;
            //newWidget.Parent = this
            //two stage parent association, 1st is to help with ancestor resolution in the widget
            if( parent != null )
            {
            	newWidget.Parent = parent;
            }
            newWidget.TagPrefix = this.TagPrefix;
            
            if( parent != null && addToParent )
            {
				parent.Add( newWidget );				
            }

			if( parseXml )
            {
            	if( xml == null )
            		newWidget.Parse();
            	else
            		newWidget.Parse(xml);
            }
            return newWidget;
        }

		public Widget CreateUnkownWidget(Type t, string name,Widget parent,AbstractRecord record)
		{
			return CreateUnkownWidget(t, name, parent, record, true, true);
		}
		
        public Widget CreateUnkownWidget(Type t, string name,Widget parent,AbstractRecord record, bool parseXml, bool addToParent )
        {
            Type[] argumentList = Type.EmptyTypes;
            object[] args = null;
            string method = "CreateWidget";
            if (t == typeof(Context) || t.IsSubclassOf(typeof(Context)))
            {
                method = "CreateContext";
            	argumentList = new Type[] { typeof(string),typeof(XmlNode), typeof(Widget),typeof(AbstractRecord), typeof(bool)  };
              	args = new object[] { name,null,parent,record, parseXml };
            }
            else
            {
               argumentList = new Type[] { typeof(Widget),typeof(AbstractRecord), typeof(XmlNode), typeof(bool), typeof(bool) };
               args = new object[] { parent,record, null, parseXml, addToParent };
            }
			return (Widget)TypeLoader.InvokeGenericMethod(typeof(Context),method, new Type[]{t},this,argumentList,args);
        }

        public Widget CreateUnkownWidget(Type t)
        {
            return CreateUnkownWidget(t, null, null, null);
        }

        public string MapPath(string virtualPath)
        {
        	if( this.isProxyContext )
        		return null;
            return HttpContext.Server.MapPath(virtualPath);
        }

		public IntPtr GetIntPtr( string Name )
		{
			MethodInfo mi = this.GetType().GetMethod(Name);
			return mi.MethodHandle.GetFunctionPointer();
		}

		public void Alert( string Message )
		{
			SendCommand( "alert('{0}');", Util.FormatForClient( Message ) );
		}

		public void ClearNotifications()
		{
			if( notificationArea != null )
			{
				notificationArea.ClearChildren();
				notificationArea.Visible = false;
			}
		}

		public Label SendClientNotification( string message )
		{
			return SendClientNotification(string.Empty, message);
		}
		
		public Label SendClientNotification( string cssClass, string message )
		{
			Label msgLabel = RootContext.CreateWidget<Label>(this.NotificationArea);
			msgLabel.ClassName = cssClass;
			msgLabel.Text = message;
			LinkButton lb = RootContext.CreateWidget<LinkButton>(msgLabel);
			lb.OnClick += new EventHandler<ClickEventArgs>( DismissNotification );
			lb.Label = "[x]";
			this.NotificationArea.Visible = true;
			return msgLabel;
		}

		public void DismissNotification(object sender, ClickEventArgs ea )
		{
			DismissNotification( (Label)ea.Source.Parent );			
		}
		
		public void DismissNotification( Label notification )
		{
			notification.Remove();
			if( this.NotificationArea.Widgets != null && this.NotificationArea.Widgets.Count == 0 )
			{
				this.NotificationArea.Visible = false;
			}
		}

        private Dictionary<string, IRecordList> recordLists;

        public Dictionary<string, IRecordList> RecordLists
        {
            get { if (recordLists == null) recordLists = new Dictionary<string, IRecordList>(); return recordLists; }
        }

		public static Context Current { 
			get
			{
				if( HttpContext.Current != null )
					return HttpContext.Current.Items["context"] as Context;
				return null;
			}
			set
			{
				if( HttpContext.Current != null )
					HttpContext.Current.Items["context"] = value;
			}
		}

		public virtual string Bookmark {
			get {
				return bookmark;
			}
			set {
				bookmark = value;
			}
		}

		public string SessionId {
			get {
				return sessionId;
			}
		}

		static Context()
		{
			//System.Environment.Version
			log.Info("Framework Version: ", System.Environment.Version);
#if MONO
			Type monoRuntime = TypeLoader.GetType("Mono.Runtime");
			log.Info("Mono Version Information: ", 
			         monoRuntime.InvokeMember("GetDisplayName", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding, null, null, null) );
#endif			
			Thread t = new Thread( delegate() {
				while(true)
				{
					try{
						foreach( Context c in activeContexts.Values.ToArray() )
						{
							if( (DateTime.Now - c.lastKeepAlive).TotalSeconds > 120 )
							{
								c.Unregister();	
							}
						}
					}
					catch(Exception e)
					{
						log.Error(e);
					}
					Thread.Sleep(60000);
					
				}
			});
			t.Start();
		}
		
		~Context()
        {
            if (this.cometClient != null)
            {
                this.cometClient.Shutdown();
            }
        }
	}
}
