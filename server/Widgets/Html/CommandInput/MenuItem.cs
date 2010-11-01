using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Widgets.Html
{
    /// <summary>
    /// Wrapper for Dojo menu items (used for ContextMenus and Toolbars)
    /// </summary>
    public class MenuItem : Widget
    {
        private string label;
        public string Label
        {
            get { return this.label; }
            set
            {
                this.label = value;
                this.SetClientAttribute("label", Util.ToJavaScriptString(value));
            }
        }

        private string icon;
        public string Icon
        {
            get { return this.icon; }
            set
            {
                this.icon = value;
                this.SetClientAttribute("icon", Util.ToJavaScriptString(value));
            }
        }

        private string subMenu;
        public string SubMenu
        {
            get { return this.subMenu; }
            set
            {
                this.subMenu = value;
                this.SetClientAttribute("subMenuId", Util.ToJavaScriptString(RootContext.Find(value).ClientId + "_cm"));
            }
        }
    }
}
