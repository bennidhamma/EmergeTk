using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;
using EmergeTk.Model.Security;

namespace EmergeTk.Widgets.Html
{
    public class Login : Generic
    {
        private LabeledWidget<TextBox> usernameField;
        private LabeledWidget<TextBox> passwordField;
        private Label errorText;
		private bool loggingIn = false;
		private bool showCancel = false;
		
		public bool ShowCancel {
			get {
				return showCancel;
			}
			set {
				showCancel = value;
			}
		}
		
		
		public event EventHandler OnCancel;
		
		public void SetupStandardLogin()
		{
				
            this.usernameField = this.RootContext.CreateWidget<LabeledWidget<TextBox>>(this);
			this.usernameField.Widget.Id = "username";
            this.usernameField.LabelText = "User Name:";
			this.usernameField.AppendClass("username");
			this.usernameField.Widget.OnEnter += delegate {
				login();	
			};
			this.usernameField.Widget.SetClientAttribute("name","'username'");
			this.usernameField.Widget.Focus();

            this.passwordField = this.RootContext.CreateWidget<LabeledWidget<TextBox>>(this);
			this.passwordField.Widget.Id = "password";
            this.passwordField.LabelText = "Password:";
            this.passwordField.Widget.IsPassword = true;
			this.passwordField.Widget.OnEnter += delegate {
				login();	
			};
			this.passwordField.Widget.SetClientAttribute("name","'password'");			
			this.passwordField.AppendClass("password");
			
            this.errorText = this.RootContext.CreateWidget<Label>(this);
            this.errorText.ClassName = "error";
            this.errorText.Visible = false;

			Button submitBtn = this.RootContext.CreateWidget<Button>(this);
			submitBtn.Label = "Login";
			submitBtn.OnClick += delegate { login(); };
			

			if( showCancel )
			{
	            Button cancelBtn = this.RootContext.CreateWidget<Button>(this);
	            cancelBtn.Label = "Cancel";
	            cancelBtn.OnClick += new EventHandler<ClickEventArgs>(cancelBtn_OnClick);
			}
		}
		
		public void SetupFirstAccount()
		{
			Label l = RootContext.CreateWidget<Label>(this);
			l.TagName = "p";
			l.Text = "It looks like this is a fresh install.  Let's go ahead and create an administrative account to help get things started, shall we?";
			ModelForm<User> userForm = RootContext.CreateWidget<ModelForm<User>>(this);	
			userForm.OnAfterSubmit += delegate
			{
				User u = userForm.TRecord;
				Role r = Role.Administrator;
				if( r == null )
				{
					r = new Role();
					r.Name = "Administrator";
					r.Permissions.Add( Permission.Root );
					r.Save();
					r.SaveRelations("Permissions",r.Permissions, true );
				}
				u.Roles.Add( r );
				u.SaveRelations("Roles");
				RootContext.LogIn(u);
			};
		}

        public override void Initialize()
        {
            base.Initialize();

			this.AppendClass("Login");
				
			if( DataProvider.GetRowCount<User>() > 0 )
			{
				SetupStandardLogin();
			}
			else
			{
				SetupFirstAccount();
			}
        }

		void login()
		{
			if ( loggingIn ) 
			{
				log.Warn("Skipping, already clicked!");
				return;
			}
			
            bool firstTime = false;
            bool successful = false;

            try
            {
				loggingIn = true;
                successful = this.RootContext.LogIn(this.usernameField.Widget.Text, this.passwordField.Widget.Text);
            }
            catch (TableNotFoundException)
            {
                firstTime = true;
				loggingIn = false;
                // TODO: need to create the new user in the database here, or maybe just let them go in
            }

            if (successful || firstTime )
            {
                //this.Remove();
                //RootContext.SendClientNotification("info", "You are now logged in.");
                //this.errorText.Text = "You are now logged in.";
                //this.errorText.Visible = true;
            }
            else
            {
            	RootContext.SendClientNotification("error", "Bad username or password.");
                //this.errorText.Text = ;
                //this.errorText.Visible = true;
				loggingIn = false;
            }
		}

        void cancelBtn_OnClick(object sender, ClickEventArgs ea)
        {
            this.Remove();
            if( OnCancel != null )
            	OnCancel( this, null );
        }
    }
}
