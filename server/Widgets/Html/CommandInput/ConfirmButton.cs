using EmergeTk;
using System;

namespace EmergeTk.Widgets.Html
{
	public class ConfirmButton : Button
	{
		public override string ClientClass { get { return "Button"; } }
		private string confirmText = "Are you sure?";
		private string confirmTitle = "Confirmation Required";
		
		public string ConfirmText { 
			get { return confirmText; }
			set { confirmText = value; 
				RaisePropertyChangedNotification("ConfirmText");			
			}
		}

		public string ConfirmTitle {
			get {
				return confirmTitle;
			}
			set {
				confirmTitle = value;
				RaisePropertyChangedNotification("ConfirmTitle");
			}
		}
		
		public override void Initialize()
		{
			OnClick += confirm;
		}
		
		private void confirm(object sender, ClickEventArgs ea)
		{			
			Dialog.Confirm(confirmText,confirmTitle, onOk);			
		}
		
		private void onOk()
		{
			if( OnConfirm != null )
				OnConfirm( this, new ClickEventArgs(this) );
		}
		
		public event EventHandler<ClickEventArgs> OnConfirm;
	}
}
