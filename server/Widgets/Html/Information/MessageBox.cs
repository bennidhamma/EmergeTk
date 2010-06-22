/**
 * Project: emergetk: stateful web framework for the masses
 * File name: .cs
 * Description:
 *   
 * @author Ben Joldersma, All-In-One Creations, Ltd. http://all-in-one-creations.net, Copyright (C) 2006.
 *   
 * @see The GNU Public License (GPL)
 */
/* 
 * This program is free software; you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation; either version 2 of the License, or 
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License along 
 * with this program; if not, write to the Free Software Foundation, Inc., 
 * 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Widgets.Html
{
    [Flags]
    public enum MessageBoxButtons
    {
        None,
        Ok = 2 << 1,
        Cancel = 2 << 2,
        Yes = 2<<3,
        No = 2<<4
    }

    public class MessageBox : Pane
    {
        private string message;

        public string Message
        {
            get { return message; }
            set { message = value; if( l != null ) l.Text = message; RaisePropertyChangedNotification("Message");}
        }

        private MessageBoxButtons buttons = MessageBoxButtons.Ok;

        public MessageBoxButtons Buttons
        {
            get { return buttons; }
            set { buttons = value; RaisePropertyChangedNotification("Buttons");}
        }

        public event EventHandler<MessageBoxEventArgs> OnOk;
        public event EventHandler<MessageBoxEventArgs> OnCancel;
        public event EventHandler<MessageBoxEventArgs> OnYes;
        public event EventHandler<MessageBoxEventArgs> OnNo;
        public event EventHandler<MessageBoxEventArgs> OnButtonPressed;
		private Label l;
		public Label MessageLabel { get { return l; } }
        public override string ClientClass { get { return "Pane"; } }
        public override void Initialize()
        {
            ClassName = "MessageBox";
			l = this.RootContext.CreateWidget<Label>();
			l.Id = "msg";
			Add(l);
			if( message != null )
			{
				l.Text = message;
			}
			if( ( buttons & MessageBoxButtons.Ok ) > 0 )
                SetupButton(MessageBoxButtons.Ok);
            if (( buttons & MessageBoxButtons.Cancel ) > 0)
                SetupButton(MessageBoxButtons.Cancel);
            if ( ( buttons & MessageBoxButtons.Yes ) > 0)
                SetupButton(MessageBoxButtons.Yes);
            if ( ( buttons & MessageBoxButtons.No ) > 0)
                SetupButton(MessageBoxButtons.No);
            //SetClientElementStyle("position", "relative",true);
            //Center();
        }
        
        public void InitAsConfirm()
        {
        	message = "Are you sure?";
        	Buttons = MessageBoxButtons.Yes | MessageBoxButtons.No;
        	Init();
        }

        private void SetupButton(MessageBoxButtons button)
        {
            LinkButton lb = RootContext.CreateWidget<LinkButton>();
            lb.Label = button.ToString();
            lb.OnClick += new EventHandler<ClickEventArgs>(lb_OnClick);
            lb.Arg = button.ToString();
            Add(lb);
        }

        void lb_OnClick(object sender, ClickEventArgs ea)
        {
            LinkButton lb = sender as LinkButton;
            string args = lb.Arg;
            MessageBoxButtons buttonPressed = MessageBoxButtons.No;
            if (args == "Yes" && OnYes != null)
            {
            	buttonPressed = MessageBoxButtons.Yes;	
                OnYes(this, new MessageBoxEventArgs( this, buttonPressed ) );
           	}
            if (args == "No" && OnNo != null)
            {
            	buttonPressed = MessageBoxButtons.No;
                OnNo(this, new MessageBoxEventArgs( this, buttonPressed ));
            }
            if(args == "Cancel" && OnCancel != null )
            {
            	buttonPressed = MessageBoxButtons.Cancel;
                OnCancel(this, new MessageBoxEventArgs( this, buttonPressed ));
            }
            if (args == "Ok" && OnOk != null)
            {
            	buttonPressed = MessageBoxButtons.Ok;
                OnOk(this, new MessageBoxEventArgs( this, buttonPressed ));
            }
            if( OnButtonPressed != null )
            	OnButtonPressed( this, new MessageBoxEventArgs( this, buttonPressed ) );
            this.Remove();
        }

        public static void Show(string Message)
        {
            MessageBox mb = Context.Current.CreateWidget<MessageBox>();
            mb.Message = Message;
            mb.Init();
            Context.Current.Add(mb);
        }
    }
}
