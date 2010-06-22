// NotificationArea.cs created with MonoDevelop
// User: ben at 2:40 P 03/06/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace EmergeTk.Widgets.Html
{
	public class NotificationArea : Generic
	{
		public NotificationArea()
		{
			ClassName = "defaultNotificationArea";
			AppendClass("notification-area");
		}
	}
}
