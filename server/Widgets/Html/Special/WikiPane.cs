using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk;
using EmergeTk.Model;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Xml;

namespace EmergeTk.Widgets.Html
{
    public class WikiPane : Pane
    {
        private Widget frame;
        private ModelForm<WikiPage> form;
        private WikiPage data;
        private ImageButton editButton,backButton,forwardButton;
        private Pane framePane;
        private List<WikiPage> history;
        private int pageIndex = -1;
		bool useDefaultText = true, useBookmarks = true;
		
        private string name;
        public new string Name
        {
            get { return name; }
            set 
            {
                name = value;
                setupPane();
            }
        }

        float buttonDisabledOpacity = 0.2f;

        private string defaultTarget;

        public string DefaultTarget
        {
            get { return defaultTarget; }
            set { defaultTarget = value; }
        }
	

        public override string ClientClass
        {
            get
            {
                return "Pane";
            }
        }

        public virtual bool UseDefaultText {
        	get {
        		return useDefaultText;
        	}
        	set {
        		useDefaultText = value;
        	}
        }

        public virtual bool UseBookmarks {
        	get {
        		return useBookmarks;
        	}
        	set {
        		useBookmarks = value;
        	}
        }

        private void setupPane()
        {
            data = WikiPage.Load<WikiPage>(new FilterInfo("Name", name, FilterOperation.Equals));

            if (data == null)
            {
                data = new WikiPage();
                data.Name = name;
            }

            if (history != null && history.Count > pageIndex + 1)
            {
                history.RemoveRange(pageIndex + 1, history.Count - (pageIndex + 1));
                forwardButton.Opacity = buttonDisabledOpacity;
            }

            if (data != null && useBookmarks )
            {
                if (history == null)
                    history = new List<WikiPage>();
                history.Add(data);
                pageIndex++;
                RootContext.AddFrameToHistory(
                    new ContextHistoryFrame(
                        new ContextHistoryHandler(goToPageIndex),
                        pageIndex, data.Name ) );
                if (pageIndex > 0 && backButton != null ) backButton.Opacity = 1.0f;
            }

            drawContext();
        }

        void goToPageIndex(object state)
        {
            int index = (int)state;
            if (index >= 0 && index < history.Count)
            {
                pageIndex = index;
                data = history[pageIndex];
                drawContext();
            }
        }

        private void drawContext()
        {
            bool needToInit = false;
            if (framePane == null)
            {
                framePane = RootContext.CreateWidget<Pane>();
                framePane.Id = name;
                framePane.ClientId = name;
                //contextPane.SetClientElementStyle("height", "'100%'");
                if (Widgets != null && Widgets.Count > 0)
                {
                    editButton.InsertBefore(framePane);
                }
                else
                {
                    Add(framePane);
                }
            }
            else if (frame != null)
            {
                needToInit = true;
                //contextPane.InvokeClientMethod("FadeShow", "500");
                framePane.Visible = true;
                frame.ClearChildren();
            }

            string xml = data.ContextXml;
            if (xml != null)
            {
                if (!xml.StartsWith("<Widget"))
                	xml = "<Widget xmlns:emg=\"http://www.emergetk.com/\">" + xml + "</Widget>";
                xml = wikizeInput(xml);
               	//System.Console.WriteLine("wiki xml: \n\n " + xml);
            }
            else if( useDefaultText )
            {
                data.ContextXml = "==" + name + "==\r\n";
                xml = string.Format("<Widget xmlns:emg=\"http://www.emergetk.com/\">=={0}==No page here yet.  Click on the edit icon below to start editing!</Widget>",
                    name);
            }
            try
            {
            	XmlDocument doc = new XmlDocument();
            	doc.LoadXml( xml );
                frame = RootContext.CreateWidget<Pane>( doc.SelectSingleNode("Widget") );
                framePane.Add(frame);
                
                if (needToInit)
                {
                    frame.Init();
                }
                if (OnPageLoad != null)
                    OnPageLoad(this, new WikiEventArgs( this, data ) );
            }
            catch (Exception e)
            {
                Label error = RootContext.CreateWidget<Label>();
                error.Text = string.Format(
                    @"Error occurred loading wiki page: \n\n
                    '''Message:''' {0}\n\n
                    '''StackTrace:''' {1}\n\n",e.Message,e.StackTrace.Replace("\n","\\n"));
                error.ForeColor = "red";
                framePane.Add(error);
            }
        }

        private string wikizeInput(string xml)
        {
            ///support:
            ///!CamelCasing -> WikiLink (CamelCasing) (regex: ![A-Z][a-z]+[A-Z][a-z]+ )
            ///[[Main Page]] -> WikiLink (Main Page) (regex: \[\[(?<name>.*?)\]\]
            ///[[Main Page -> MainPane]] (Main Page in pane MainPane) \[\[(?<name>.*?)\s*->\s*(?<target>.*?)\]\]
            ///[[Main Page|index]] becomes index. 
            ///[http://www.example.org link name] (resulting in "link name" as link)
            
	        //Regex newlines = new Regex(@"(?<!==|\>)\n");
	       	//xml = newlines.Replace( xml, "<br/>");
            
            //targeted
            Regex targetedWikiLink = new Regex(@"!(\w+)->(\w+)", RegexOptions.Compiled);
            Regex targetedMWikiLink = new Regex(@"\[\[(?<name>.*?)\s*?->\s*?(?<target>.*?)\]\]", RegexOptions.Compiled);
            xml = targetedWikiLink.Replace(xml, new MatchEvaluator(delegate(Match m)
             { return string.Format("<emg:WikiLink Name=\"{0}\" Target=\"{1}\"/>", m.Groups[1].Value.TrimStart('!'),m.Groups[2].Value); }));
            xml = targetedMWikiLink.Replace(xml, new MatchEvaluator(delegate(Match m)
             { return string.Format("<emg:WikiLink Name=\"{0}\" Target=\"{1}\"/>", m.Groups[1].Value.TrimStart('!'), m.Groups[2].Value); }));
            
            //locals
            Regex localWikiLink = new Regex(@"!(?<name>\w+)", RegexOptions.Compiled);
            Regex localMWikiLink = new Regex(@"\[\[(?<name>.*?)(?<display>\|.*?)?\]\]", RegexOptions.Compiled);
            xml = localWikiLink.Replace(xml, new MatchEvaluator(localLink));
            xml = localMWikiLink.Replace(xml, new MatchEvaluator(localLink));

            return xml;
        }

        private string localLink(Match m)
        {
            string name = m.Groups["name"].Value;
            string text = m.Groups["display"] != null && m.Groups["display"].Success ? m.Groups["display"].Value.Trim('|') : name;
            return string.Format("<emg:WikiLink Name=\"{0}\" Label=\"{1}\"/>",name,text ); 
        }

        public override void Initialize()
        {
			AppendClass("wiki");

			Pane buttonPane = RootContext.CreateWidget<Pane>();
        	buttonPane.ClassName = "buttons";

            editButton = RootContext.CreateWidget<ImageButton>();
            editButton.Url = ThemeManager.Instance.RequestClientPath( "/Images/Edit.png" );
            editButton.ClassName = "wikiButton";
            backButton = RootContext.CreateWidget<ImageButton>();
            backButton.Url = ThemeManager.Instance.RequestClientPath( "/Images/Back.png" );
            backButton.ClassName = "wikiButton";
            backButton.Opacity = buttonDisabledOpacity;
            backButton.OnClick += new EventHandler<ClickEventArgs>(backButton_OnClick);
            forwardButton = RootContext.CreateWidget<ImageButton>();
            forwardButton.Url = ThemeManager.Instance.RequestClientPath( "/Images/Forward.png" );
            forwardButton.ClassName = "wikiButton";
            forwardButton.Opacity = buttonDisabledOpacity;
            forwardButton.OnClick += new EventHandler<ClickEventArgs>(forwardButton_OnClick);
            editButton.OnClick += new EventHandler<ClickEventArgs>(editButton_OnClick);
            buttonPane.Add( backButton, editButton, forwardButton );
            Add(buttonPane);
            //Add(editButton, forwardButton, backButton);
            //Pane dummy = RootContext.CreateWidget<Pane>();
            //Add(dummy);
            if (RootContext.HttpContext.Request[this.ClientId ] != null)
                this.Name = RootContext.HttpContext.Request[this.ClientId];
        }

        void backButton_OnClick(object sender, ClickEventArgs ea)
        {
            if (pageIndex > 0)
            {
                forwardButton.Opacity = 1.0f;
                data = history[--pageIndex];
                drawContext();
                if (pageIndex == 0)
                    backButton.Opacity = buttonDisabledOpacity;
            }
        }

        void forwardButton_OnClick(object sender, ClickEventArgs ea)
        {
            if (pageIndex < history.Count-1)
            {
                data = history[++pageIndex];
                drawContext();
                backButton.Opacity = 1.0f;
                if (pageIndex == history.Count - 1)
                    forwardButton.Opacity = buttonDisabledOpacity;
            }
        }

        void editButton_OnClick(object sender, ClickEventArgs ea)
        {
            if (data == null)
                data = new WikiPage();
            editButton.Visible = false;
            if (frame != null)
                framePane.Visible = false;
            form = RootContext.CreateWidget<ModelForm<WikiPage>>();
            form.DataBindWidget(data);
            form.OnAfterSubmit += new EventHandler<ModelFormEventArgs<WikiPage>>(form_OnSubmit);
            form.OnCancel += new EventHandler<ModelFormEventArgs<WikiPage>>(form_OnCancel);
            form.Init();
            Add(form);
        }

        void form_OnCancel(object sender, ModelFormEventArgs<WikiPage> ea)
        {
            editButton.Visible = true;
            if (frame != null)
                framePane.Visible = true;
        }

        void form_OnSubmit(object sender, ModelFormEventArgs<WikiPage> ea)
        {
            drawContext();
            framePane.Visible = true;
            editButton.Visible = true;
            form.Visible = false;
            data = form.TRecord;
            setupPane();
        }

        public event EventHandler<WikiEventArgs> OnPageLoad;
    }    
}
