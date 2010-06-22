using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Widgets.Html
{
    public class ButtonHandler
    {
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ButtonHandler));				
		
        public static void ToggleElementVisibility(object sender, EventArgs ea )
        {
            Button button = (Button)sender;
            button.RootContext.Find(button.Arg).Visible = !button.RootContext.Find(button.Arg).Visible; 
        }
    }
}
