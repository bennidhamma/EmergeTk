using System;
using System.Text.RegularExpressions;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class XmlWidget : Generic
	{
		string xml;
		public string Xml
		{
			get { return xml; }
			set {
				try
				{
					xml = value;
					if( string.IsNullOrEmpty( xml ) )
						return;
					ClearChildren();
					Regex ropen = new Regex("<(\\w+)");
					Regex rclose = new Regex("</(\\w+)");
					xml = ropen.Replace(xml, "<emg:$1");
					xml = rclose.Replace(xml, "</emg:$1");
					
					Parse("<Widget xmlns:emg=\"http://www.emergetk.com/\">" + xml + "</Widget>");
				}
				catch( Exception e )
				{
					log.Error( "Error binding xml to XmlWidget", Util.BuildExceptionOutput(e) );
				}
			}
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			
		}

	}
}
