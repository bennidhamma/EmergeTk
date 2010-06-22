using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Widgets.Html
{
    public class WikiLink : LinkButton
    {
        private string target;
        public string Target
        {
            get
            {
                if (target == null)
                {
                    WikiPane wpane = FindAncestor<WikiPane>();
                    if (wpane != null && wpane.DefaultTarget != null)
                        return wpane.DefaultTarget;
                }
                return target; 
            }
            set { target = value; }
        }

        private string name;
        public new string Name
        {
            get { return name; }
            set { name = value; if( this.Label == null || this.Label == string.Empty ) this.Label = value; }
        }

        public override string ClientClass
        {
            get
            {
                return "LinkButton";
            }
        }	
	
        public WikiLink()
        {
            this.OnClick += new EventHandler<ClickEventArgs>(WikiLink_OnClick);
            this.ClassName = "wlink";
        }

        void WikiLink_OnClick(object sender, ClickEventArgs ea)
        {
            WikiPane wiki;
            if (Target == null || Target == "this")
            {
                wiki = FindAncestor<WikiPane>();
            }
            else
            {
                wiki = RootContext.Find<WikiPane>(Target);
            }
            wiki.Name = name;
        }
    }
}
