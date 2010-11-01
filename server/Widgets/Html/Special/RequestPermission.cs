// RequestPermission.cs created with MonoDevelop
// User: ben at 9:20 A 15/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using EmergeTk.Model.Security;

namespace EmergeTk.Widgets.Html
{		
	public class RequestPermission : Lightbox
	{		
		Permission requestedPermission;
		
		public Permission RequestedPermission {
			get {
				return requestedPermission;
			}
			set {
				requestedPermission = value;
			}
		}

		public bool ShowCancel {
			get {
				return showCancel;
			}
			set {
				showCancel = value;
			}
		}
		
		public string Title { get; set; }
		public string Description { get; set; }

		bool showCancel;
		
		public event EventHandler OnAuthSuccess; 
		public event EventHandler OnAuthFailed;
		public event EventHandler OnAuthCancel;
		
		public RequestPermission()
		{
		}
		
		public override void Initialize ()
		{			
			this.Label = Title ?? "Authorization Required";
			base.Initialize();
			
			if( ! test() )
			{
				Label head = RootContext.CreateWidget<Label>(this);
				head.Text = Description ?? "This action requires permission.  Please log in with an authorized account";
				Login login = RootContext.CreateWidget<Login>(this);
				login.ShowCancel = showCancel;
				RootContext.OnLogIn += new EventHandler<EmergeTk.UserEventArgs>( delegate( object sender, UserEventArgs e ) {
					if( ! test() )
					{
						Label error = RootContext.CreateWidget<Label>(this);
						error.ClassName = "error";
						error.Text = "This account has insufficient privileges.";
					}
				});
				login.OnCancel += new EventHandler( delegate( object sender, EventArgs e ) {
					if( OnAuthCancel != null )
						OnAuthCancel( this, null );
					if( OnAuthFailed != null )
						OnAuthFailed( this, null );
					this.Close();
				} );
			}
		}

		private bool test()
		{ 
			if( RootContext.CurrentUser != null &&
				( requestedPermission == null ||
				RootContext.CurrentUser.CheckPermission( requestedPermission ) ) )
			{
				if( OnAuthSuccess != null )
					OnAuthSuccess( this, null );
				Remove();
				return true;
			}
			return false;
		}
	}
}
