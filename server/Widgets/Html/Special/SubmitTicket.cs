// SubmitTicket.cs created with MonoDevelop
// User: damien at 2:50 PM 8/25/2008
//
// Copyright Skull Squadron, All Rights Reserved.

using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;
using CookComputing.XmlRpc;
using TracPusher;

namespace EmergeTk.Widgets.Html
{	
	
	public class SubmitTicket : Generic
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(SubmitTicket));
		
		public TextBox subject,description;
		
		public SubmitTicket()
		{
		}
		
		public virtual void ShowTicketForm( object sender, ClickEventArgs ea )
        {
        	log.Info("SHOW TICKET!");
			
			Lightbox box = this.RootContext.CreateWidget<Lightbox>(this);
			Label label = this.RootContext.CreateWidget<Label>(box);
			label.Text = "Subject";
			
			subject = this.RootContext.CreateWidget<TextBox>(box);			
			subject.Text = "New Ticket";

			label = this.RootContext.CreateWidget<Label>(box);
			label.Text = "Description";
			description = this.RootContext.CreateWidget<TextBox>(box);	
			//description.Text = BuildInfo;
			
			LinkButton submit = this.RootContext.CreateWidget<LinkButton>(box);
			submit.Label = "Submit";
			submit.OnClick += delegate
			{
				log.Debug("Submit!");
				
				MailTicket(subject.Text, BuildInfo + "\n\n" + description.Text);
				
				//CreateTicket(subject.Text, BuildInfo + "\n\n" + description.Text);
				
				box.Visible = false;
			};
			
			LinkButton cancel = this.RootContext.CreateWidget<LinkButton>(box);
			cancel.Label = "Cancel";
			cancel.OnClick += delegate
			{
				log.Debug("Cancel!");
				box.Visible = false;
			};
		}		
		
		public string BuildInfo
		{
			get
			{
				string build = "";
				System.IO.FileInfo buildInfo = new System.IO.FileInfo(this.RootContext.HttpContext.Server.MapPath("build.txt"));	
				if (buildInfo.Exists)
				{	
					// create reader & open file
					System.IO.TextReader tr = new System.IO.StreamReader(buildInfo.FullName);
					
					// read a line of text
					build = tr.ReadLine();
					
					// make build link
					//build = "<a href=\"http://sdh.skullsquad.com:8080/buildresults/ari?log=log" + build + "\">" + build + "</a>";
					
					// close the stream
					tr.Close();
				}
				return build;
			}
		}		
		
		public void MailTicket(string summary, string description)
		{
			if( ! Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["MailTickets"])) 
			{
				log.Debug("Going to skip mailing ticket, disabled in settings");
				return;
			}
			else
				log.Debug("Mailing ticket...");
			
			string from = System.Configuration.ConfigurationManager.AppSettings["MailTicketsFrom"];
			string to = System.Configuration.ConfigurationManager.AppSettings["MailTicketsTo"];
			
			MailUtil.SendMail(from,to,summary,description);
			
			log.Debug("Mailed ticket.");
		}
			
		public void CreateTicket(string summary, string description)
		{
			if( ! Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["TracTickets"])) 
			{
				log.Debug("Going to skip creating ticket, disabled in settings");
				return;
			}
			else
				log.Debug("Creating ticket...");
			
			Trac proxy;
			
			// Fill these in appropriately
			string user = System.Configuration.ConfigurationManager.AppSettings["TracUser"];			
			string password = System.Configuration.ConfigurationManager.AppSettings["TracPassword"];
			
			/// Create an instance of the Trac interface
			proxy = XmlRpcProxyGen.Create<Trac>();			
			
			// If desired, point this to your URL. If you do not do this,
			// it will use the one specified in the service declaration.
			// proxy.Url = "https://trac-rules.org/xmlrpc";
			proxy.Url = System.Configuration.ConfigurationManager.AppSettings["TracXmlRpcUrl"];
			
			// Attach your credentials
			proxy.Credentials = new System.Net.NetworkCredential(user, password);	
			  			
			PageAttributes attr;	
			attr.comment = "foobar";
			int ticketNum = proxy.createTicket(summary,description,attr,true);
		    log.Debug("Created Ticket Num: " + ticketNum);
		}
	}
}

namespace TracPusher {

	public interface Trac : IXmlRpcProxy
	{
		[XmlRpcMethod("wiki.getAllPages")]
		string[] getAllPages();
		
		[XmlRpcMethod("wiki.putPage")]
		bool putPage(string pagename, string content, PageAttributes attr);
		
		[XmlRpcMethod("ticket.create")]	
		int createTicket(string summary, string description, PageAttributes attr, bool notify);	
	}
	
	// define the structure needed by the putPage method
	public struct PageAttributes {
		public string comment;
	}
}
