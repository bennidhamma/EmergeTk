// Dialog.cs created with MonoDevelop
// User: ben at 5:11 PM 12/16/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using EmergeTk;

namespace EmergeTk.Widgets.Html
{
	public delegate void PromptCallback( string input );
	public delegate void DialogCallback();
	
	public class Dialog
	{
		private Dialog()
		{
		}

		public static void Message(string message, string title )
		{
			Context c = Context.Current;
			if( c == null )
				return;
			Lightbox lb = c.CreateWidget<Lightbox>();
			lb.AppendClass("dialog");
			lb.Label = title;
			Label l = c.CreateWidget<Label>();
			l.Text = message;
			LinkButton okButton = c.CreateWidget<LinkButton>();
			okButton.Label = "OK";
            okButton.Id = "OKButton";
            //cancelButton.Id = "CancelButton";

			okButton.OnClick += delegate {
				lb.Close();	
			};

			c.Add(lb);
			lb.Add(l,okButton);
			lb.Init();
		}

		public static void Confirm(string message, string title, DialogCallback confirmCallback )
		{
			Context c = Context.Current;
			if( c == null )
				return;
			Lightbox lb = c.CreateWidget<Lightbox>();
			lb.Label = title;
			Label l = c.CreateWidget<Label>();
			l.TagName = "p";
			l.AppendClass("dialog");
			LinkButton okButton = c.CreateWidget<LinkButton>();
			LinkButton cancelButton = c.CreateWidget<LinkButton>();

			l.Text = message;
			okButton.Label = "OK";
			cancelButton.Label = "Cancel";
            okButton.Id = "OKButton";
            cancelButton.Id = "CancelButton";

			okButton.OnClick += delegate {
				confirmCallback();
				lb.Close();
			};

			cancelButton.OnClick += delegate {
				lb.Close();
			};
			
			HtmlElement buttonp = HtmlElement.Create("p");
			buttonp.Add( okButton, cancelButton );
			buttonp.AppendClass("dialog");
			
			c.Add(lb);
			lb.Add(l,buttonp);
			lb.Init();
		}

		public static void Prompt(string message, string title, PromptCallback callback )
		{
			Context c = Context.Current;
			if( c == null )
				return;
			Lightbox lb = c.CreateWidget<Lightbox>();
			Label l = c.CreateWidget<Label>();
			lb.AppendClass("dialog");
			TextBox t = c.CreateWidget<TextBox>();
			LinkButton okButton = c.CreateWidget<LinkButton>();
			LinkButton cancelButton = c.CreateWidget<LinkButton>();
			lb.Label = title;
			l.Text = message;
			okButton.Label = "OK";
			cancelButton.Label = "Cancel";

            okButton.Id = "OKButton";
            cancelButton.Id = "CancelButton";
			
			t.OnEnter += delegate {
				callback( t.Text );
				lb.Close();
			};
			
			okButton.OnClick += delegate {
				callback( t.Text );
				lb.Close();
			};

			cancelButton.OnClick += delegate {
				lb.Close();
			};

			c.Add(lb);
			lb.Add(l,t,okButton,cancelButton);
			lb.Init();
		}
	}
}
