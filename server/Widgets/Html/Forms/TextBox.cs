using System;
using System.ComponentModel;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
    [DefaultProperty("Text")]
    public class TextBox : Widget, IDataBindable
    {
        //isPassword, isDisabled, rows, cols
        private const string UpdateFormat = "{0}.SetText('{1}');";

        private bool isPassword = false, isDisabled = false, isRich = false, 
        	isCodeView = false, isInline = false, autoUpdate = true, saveOnChange = false;
        private int rows = 1, cols = 0;
        private string text = "", currentTextOnClient;

		private string helpText;
		
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                if (text != value)
                {
					//log.Debug( "setting text", text, value ); 
                    text = value;
                    if (this.rendered && text != currentTextOnClient)
                        InvokeClientMethod("SetText", Util.ToJavaScriptString(value));
                    currentTextOnClient = text;
                    ClientArguments["defaultValue"] = Util.Quotize(Util.FormatForClient(value));
                    RaisePropertyChangedNotification("Text");
                }
            }
        }

        public bool IsRich
        {
            get
            {
                return isRich;
            }
            set
            {
                isRich = value;
                RaisePropertyChangedNotification("IsRich");
                SetClientAttribute("isRich", isRich ? "1" : "0");
            }
        }

        public bool IsInline
        {
            get
            {
                return isInline;
            }
            set
            {
                isInline = value;
                SetClientAttribute("isInline", isInline ? "1" : "0");
                RaisePropertyChangedNotification("IsInline");
                SetClientAttribute("autoUpdate", autoUpdate ? "1" : "0");
            }
        }

		public bool SaveOnChange
		{
			get { return saveOnChange; }
			set { saveOnChange = value; }
		}

        public bool AutoUpdate
        {
            get
            {
                return autoUpdate;
            }
            set
            {
                autoUpdate = value;
                SetClientAttribute("autoUpdate", autoUpdate ? "1" : "0");
                RaisePropertyChangedNotification("AutoUpdate");
            }
        }

        public bool IsDisabled
        {
            get
            {
                return isDisabled;
            }
            set
            {
                isDisabled = value;
                ClientArguments["isDisabled"] = isDisabled ? "1" : "0";
                RaisePropertyChangedNotification("IsDisabled");
            }
        }

        public bool IsPassword
        {
            get
            {
                return isPassword;
            }
            set
            {
                isPassword = value;
                ClientArguments["isPassword"] = isPassword ? "1" : "0";
                RaisePropertyChangedNotification("IsPassword");
            }
        }

        public bool IsCodeView
        {
            get
            {
                return isCodeView;
            }
            set
            {
                isCodeView = value;
                ClientArguments["isCodeView"] = isCodeView ? "1" : "0";
                RaisePropertyChangedNotification("IsCodeView");
            }
        }

        public int Rows
        {
            get
            {
                return rows;
            }
            set
            {
                rows = value;
                ClientArguments["rows"] = rows.ToString();
                RaisePropertyChangedNotification("Rows");
            }
        }

        public int Columns
        {
            get
            {
                return cols;
            }
            set
            {
                cols = value;
                ClientArguments["cols"] = cols.ToString();
                RaisePropertyChangedNotification("Columns");
            }
        }

        public void Changed(string newText)
        {
            currentTextOnClient = newText;
            string oldText = Text;
            Text = newText;

            InvokeChangedEvent(oldText, newText);
            
			if( saveOnChange && Record != null )
			{
				Record.Save();
				
				if( this.Bindings != null && this.Bindings.Count == 1 )
				{
					RootContext.ClearNotifications();
					RootContext.SendClientNotification("", this.Bindings[0].Fields[0] + " saved.");
				}
			}
        }

        private event EventHandler<KeyPressEventArgs> onKeyUp;
        private event EventHandler<ClickEventArgs> onEnter;
        public event EventHandler<KeyPressEventArgs> OnKeyUp
        {
            add { onKeyUp += value; ClientArguments["onKeyUp"] = "1"; }
            remove { onKeyUp -= value; }
        }

        public event EventHandler<ClickEventArgs> OnEnter
        {
            add { onEnter += value; ClientArguments["onEnter"] = "1"; }
            remove { onEnter -= value; }
        }

        public TextBox(string id, string text)
        {
            this.Id = id;
            this.text = text;
        }

        public TextBox() {ClientArguments["defaultValue"] = "''"; }

        public override void HandleEvents(string evt, string args)
        {
            if (evt == "OnChanged")
            {
                //System.Console.WriteLine("changing {0} from {1} to {2}", this.Id, this.Text, args);
                Changed(args);
                RootContext.SendCommand("removeWaitFor({0});", this.ClientId);
            }
            else if (evt == "OnEnter")
            {
				ClickEventArgs ea = new ClickEventArgs(this );				
                Changed(args);
                if (onEnter != null)
                    onEnter(this, ea );
            }
			else if( evt == "OnKeyUp" )
			{
				KeyPressEventArgs ea = new KeyPressEventArgs(this, args );
				Changed(ea.Value);
                if (onKeyUp != null)
                    onKeyUp(this, ea );
			}
            else
            {
                base.HandleEvents(evt, args);
            }
        }

        public override string ToString()
        {
            return Text;
        }

        override public object Value
        {
            get
            {
                return this.Text;
            }
            set
            {
                if (value != null)
                    Changed(value.ToString());
                RaisePropertyChangedNotification("Value");
            }
        }

        public bool IsPassThrough
        {
            get { return false; }
        }

        override public string DefaultProperty
        {
            get { return "Text"; }
        }

        private Binding binding;
        public Binding Binding
        {
            get { return binding; }
            set { binding = value; }
        }

        public string HelpText {
        	get {
        		return helpText;
        	}
        	set {
        		helpText = value;
			 	SetClientProperty("helpText",Util.ToJavaScriptString(helpText));
        	}
        }

        public void Focus()
        {
            InvokeClientMethod("Focus");
        }

    }
}
