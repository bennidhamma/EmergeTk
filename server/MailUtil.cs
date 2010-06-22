// MailUtil.cs created with MonoDevelop
// User: damien at 4:02 PM 8/25/2008
//
// Copyright Skull Squadron, All Rights Reserved.

using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;
using EmergeTk.Model;
using EmergeTk;

using System;
using System.Data;
using System.Net.Mail;
using System.Collections;
using System.Collections.Generic;

namespace EmergeTk
{
	
	
	public class MailUtil
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(MailUtil));	
		
		public MailUtil()
		{
		}
		
		public static void send( string message, string title, string to, string from )
		{
			if( Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["EnableNotifications"]))			
				SendMail(from,to,title,message);
			else
				log.Warn("EnableNotifications is false, skipping send");
		}
		
		public static void send(String message,String title,String to)
		{			
			send( message, title, to, "emergeTk@" + System.Configuration.ConfigurationManager.AppSettings["NotificationsFromDomain"] ); 
		}
		
		
		public static void SendMail(string from, string to,
                     string subject, string body)
		{
		   //string mailServerName = "localhost";
		   string mailServerName = Setting.GetValueT<string>("MailServer","localhost");
		   log.Debug("SendMail",from,to,subject,body,mailServerName);
		   try
		   {
		      //MailMessage represents the e-mail being sent
		      using (MailMessage message = new MailMessage(from,
		             to, subject, body))
		      {
		         message.IsBodyHtml = true;
		         SmtpClient mailClient = new SmtpClient();
		         mailClient.Host = mailServerName;
		         mailClient.Credentials = null;
		         //mailClient.UseDefaultCredentials = false;
		         //Send delivers the message to the mail server
				mailClient.Send(message);
		      }
				
			  
		   }
		   catch (SmtpException ex)
		   {
		      throw new ApplicationException
		         ("SmtpException has oCCured: " + ex.Message);
		   }
		   catch (Exception ex)
		   {
		      throw ex;
		   }
		}	

	}
}
