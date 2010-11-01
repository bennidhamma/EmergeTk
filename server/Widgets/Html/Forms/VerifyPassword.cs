using System;
using EmergeTk;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Widgets.Html
{
    public class VerifyPassword : Generic
    {
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(VerifyPassword));		
		
        private TextBox passwordField;
        private TextBox verifyField;
        private Label errorLabel;

        private User user;
        public User User
        {
            get { return user; }
            set { user = value; }
        }

        public override void Initialize()
        {
            base.Initialize();

            this.passwordField = this.Find<TextBox>("PasswordField");
            this.passwordField.OnChanged += delegate { Verify(); };

            this.verifyField = this.Find<TextBox>("VerifyPassword");
            this.verifyField.OnChanged += delegate { Verify(); };

            this.errorLabel = this.Find<Label>("PasswordError");
        }

        private void Verify()
        {
            if (String.IsNullOrEmpty(this.verifyField.Text) || String.IsNullOrEmpty(this.verifyField.Text))
            {
                this.errorLabel.Visible = false;
                this.User.PasswordIsVerified = false;
            }
            else if (this.verifyField.Text != this.passwordField.Text)
            {
                this.errorLabel.Text = "Your passwords do not match.";
                this.errorLabel.Visible = true;
                this.User.PasswordIsVerified = false;
            }
            else
            {
                this.errorLabel.Visible = false;
                this.User.PasswordIsVerified = true;
                this.User.PlainTextPassword = this.passwordField.Text;
            }
        }
    }
}