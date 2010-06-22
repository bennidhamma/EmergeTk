using System;
namespace EmergeTk.Widgets.Html
{
	public class Lightbox : Pane
	{
		public event EventHandler OnClose; 
		public override string ClientClass { get { return "dialog"; } }
		Pane lightbox = null; // never set
		// Pane decorator, black;
		// HtmlElement border;
		// Image shadow;
		
		bool center = true,removeOnClose = true;
		
		
		public Pane Front { get { return lightbox; } } 

		public virtual bool ComputeCenter {
			get {
				return center;
			}
			set {
				center = value;
				RaisePropertyChangedNotification("ComputeCenter");
			}
		}

		public virtual bool RemoveOnClose {
			get {
				return removeOnClose;
			}
			set {
				removeOnClose = value;
			}
		}
		
		
		
		//store a local isinitialized to allow classes to derive from lightbox and use the initialize function.
		// TODO: remove unreachable code below
		// bool isInitialized = false;
		public override void Initialize()
		{
			SetClientAttribute("title",Util.ToJavaScriptString(this.Label));
			BindProperty("Label",delegate {
				SetClientAttribute("title",Util.ToJavaScriptString(this.Label));
			});
			
			return;
			
			/* UNREACHABLE: 
			if( isInitialized ) return;
			
			this.ClassName = "lightbox";
			
			black = RootContext.CreateWidget<Pane>();
			black.Opacity = 0.5f;
			black.ClassName = "back";
			black.InvokeClientMethod("StretchToExtents");
			
			border = RootContext.CreateWidget<HtmlElement>();
			border.ClassName = "border";
			
			shadow = RootContext.CreateWidget<Image>();
			shadow.ClassName = "shadow";
			shadow.Url = "/EmergeTk/Images/shadow.png";
			
			lightbox = RootContext.CreateWidget<Pane>();
			lightbox.ClassName = "front";
			
			decorator = RootContext.CreateWidget<Pane>();
			decorator.ClassName = "decorator";
			Pane closeDiv = RootContext.CreateWidget<Pane>();
			closeDiv.ClassName = "closeLink";
			//Image close = RootContext.CreateWidget<Image>();
			//close.Url = "Images/window-close.png";
			//closeDiv.Add(close);
			
			//close.OnClick  += new OnClickHandler(Close);
			
			decorator.Add( closeDiv );
			
			Label l = RootContext.CreateWidget<Label>();
			if( this.Label != null )
				l.Bind(this,"Label");
			l.ClassName = "title";
			decorator.Add( l );
			decorator.Add( lightbox );
			decorator.Opacity = 0.001f;
			base.Add( black );
			base.Add( border );
			border.Add( shadow );
			border.Add( decorator );
			//base.Add( decorator );
			//lightbox.Center();
			isInitialized = true;
			*/
		}
		
		public void Close( Widget w, string args )
		{
			Close();
		}
		
		public void Close()
		{
			this.InvokeClientMethod("Hide");	
			if( removeOnClose )
					this.Remove();
			else
				this.Visible = false;
			if( OnClose != null )
				OnClose( this, null );
		}



		protected override void PostRender()
		{
			InvokeClientMethod("ShowDialog");
		}
	}
}
