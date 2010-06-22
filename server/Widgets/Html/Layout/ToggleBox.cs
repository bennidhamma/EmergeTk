// /home/ben/workspaces/emergeTk/trunk/Widgets/Html/ToggleBox.cs created with MonoDevelop
// User: ben at 3:25 PM 7/30/2007
//
using System;
using EmergeTk.Model;
using System.ComponentModel;

namespace EmergeTk.Widgets.Html
{
	public class ToggleBox : Generic
	{
		Generic toggleTop;
		Generic toggleMain;
		
		string label;
		Generic labelWidget;
		bool open = false;
		//string openedUrl = "/Images/Icons/16/close.png",closedUrl = "/Images/Icons/16/expand.png";
		string topClass;
		
		
		public virtual EmergeTk.Widgets.Html.Generic ToggleTop {
			get {
				return toggleTop;
			}
			set {
				toggleTop = value;
				RaisePropertyChangedNotification("ToggleTop");
			}
		}

		public virtual EmergeTk.Widgets.Html.Generic ToggleMain {
			get {
				return toggleMain;
			}
			set {
				toggleMain = value;
				RaisePropertyChangedNotification("ToggleMain");
			}
		}

		public virtual string Label {
			get {
				return label;
			}
			set {
				label = value;
				if( labelWidget is Label )
					((Label)labelWidget).Text = value;
				RaisePropertyChangedNotification("Label");
			}
		}
		
		public override void Initialize ()
		{
			this.AppendClass("ToggleBox");
            setupTogglePanes();			
		}
		
		public bool Open { 
			get {
				return open;
			}
			set {
				open = value;
				update();
				RaisePropertyChangedNotification("Open");
			}
		}

		public string TopClass {
			get {
				return topClass;
			}
			set {
				topClass = value;
			}
		}

		public Generic LabelWidget {
			get {
				return labelWidget;
			}
			set {
				labelWidget = value;
			}
		}

		public override object Clone()
		{
			ToggleBox tb = ShallowClone() as ToggleBox;
			
			tb.toggleMain = null;
			if( toggleMain != null )
				tb.setupTogglePanes( toggleMain.Clone() as Generic );
			return tb;
		}
		
		private void setupTogglePanes()
		{
			setupTogglePanes(null);
		}
		
        private void setupTogglePanes(Generic main )
        {
            if (toggleMain == null)
            {
            	if( toggleTop == null )
                	toggleTop = RootContext.CreateWidget<Generic>();
                toggleMain = main ?? RootContext.CreateWidget<Generic>();
                toggleTop.ClassName = "toggleTop";
                if( topClass != null )
                	toggleTop.AppendClass( topClass );
                base.Add(toggleTop);
                base.Add(toggleMain);
                toggleMain.Visible = open;
                toggleMain.Id = "toggleMain";
                toggleMain.ClassName = "toggleMain";
                if( labelWidget == null )
                {
                	labelWidget = RootContext.CreateWidget<Label>();
                	((Label)labelWidget).Text = label;
                	labelWidget.OnClick += new EventHandler<ClickEventArgs>( Toggle); 
                }
                labelWidget.ClassName =  open ? "open" : "closed";
				toggleTop.Add( labelWidget );				
            }
        }

		public override void Add (Widget c)
		{
			if( IsCloning || deserializing )
			{
				base.Add( c );
			}
			else
			{
				setupTogglePanes();
            	toggleMain.Add(c);
            }
		}
		
		public override void Insert (Widget c, int index)
		{
			setupTogglePanes();
			toggleMain.Insert (c, index);
		}

		public event EventHandler OnToggle;
		
		public void Toggle(object sender, ClickEventArgs ea)
		{
           Toggle();
		}

		public void Toggle()
		{ 			
			open = !open;
			update();
		}
		
		private void update()
		{
			if( toggleMain != null )
			{
				ToggleMain.Visible = open;
				labelWidget.ClassName = open ? "open" : "closed";
				if( OnToggle != null )
					OnToggle( this, null );
			}
		}
	}
}
