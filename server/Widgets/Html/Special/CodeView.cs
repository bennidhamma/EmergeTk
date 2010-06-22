/**
 * Project: emergetk: stateful web framework for the masses
 * File name: CodeView.cs
 * Description: A CodeView widget.
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
using System.IO;
using System.ComponentModel;

namespace EmergeTk.Widgets.Html
{
    public enum Language
    {
        NotSet = 0,
        CSharp,
        Sql,
        Xml,
        JavaScript
    }

    [DefaultProperty("CodeSource")]
    public class CodeView : PlaceHolder
    {
        private string codeSource;

        public string CodeSource
        {
            get { return codeSource; }
            set { 
            	codeSource = value;
            	RaisePropertyChangedNotification("CodeSource");
            }
        }

        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set { 
                fileName = value;
                StreamReader sr = null;
                if (System.IO.File.Exists(FileName))
                    sr = File.OpenText(FileName);
                else if (File.Exists(RootContext.MapPath(FileName)))
                    sr = File.OpenText(RootContext.MapPath(FileName));
                else
                    return;
                codeSource = sr.ReadToEnd();
                sr.Close();
                RaisePropertyChangedNotification("FileName");            }
        }

        private bool codeVisible = false;
        public bool CodeVisible
        {
            get { return codeVisible; }
            set { codeVisible = value; 
            	RaisePropertyChangedNotification("CodeVisible");
            }
        }
	

        private Language language;

	    public Language Language
	    {
		    get { return language;}
		    set { language = value;}
	    }
	
        public override void Initialize()
        {
            ClientClass = "PlaceHolder";
            
            if( codeSource == null )
                return;
                
            TextBox tb = RootContext.CreateWidget<TextBox>();
            tb.Text = codeSource;
            tb.Rows = 1;
            tb.Columns = 80;
            if (language == Language.NotSet)
            {
                if (FileName.EndsWith(".cs"))
                    language = Language.CSharp;
                else if (FileName.EndsWith(".js"))
                    language = Language.JavaScript;
                else if (FileName.EndsWith(".sql"))
                    language = Language.Sql;
                else if (FileName.EndsWith(".xml"))
                    language = Language.Xml;
            }

            if (language != Language.NotSet)
            {
                tb.IsCodeView = true;
                string lang = string.Empty;
                switch (language)
                {
                    case Language.CSharp:
                        lang = "c#";
                        break;
                    case Language.JavaScript:
                        lang = "js";
                        break;
                    case Language.Sql:
                        lang = "sql";
                        break;
                    case Language.Xml:
                        lang = "xml";
                        break;
                }
                tb.SetClientAttribute("lang", Util.ToJavaScriptString(lang));
            }
            tb.Visible = codeVisible;
            Label l = RootContext.CreateWidget<Label>();
            l.Text = FileName;
            l.Bold = true;
            LinkButton lb = RootContext.CreateWidget<LinkButton>();
            lb.OnClick += ButtonHandler.ToggleElementVisibility;
            lb.Label = "Show/Hide";
            l.Add(lb);
            Add(l,tb);
            lb.Arg = tb.UID;
        }
    }
}
