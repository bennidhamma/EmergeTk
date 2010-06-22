// Theme.cs
//	
//

using System;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Widgets.Html
{
	public class Theme
	{
		string path;
			
		public Theme( string p )
		{
			path = p;
		}
		
		public static Theme Blue = new Theme("dojox.charting.themes.PlotKit.blue");		
		public static Theme Cyan = new Theme("dojox.charting.themes.PlotKit.cyan");
		public static Theme Green = new Theme("dojox.charting.themes.PlotKit.green");
		public static Theme Greys = new Theme("dojox.charting.themes.ET.greys");
		public static Theme Orange = new Theme("dojox.charting.themes.PlotKit.orange");		
		public static Theme Purple = new Theme("dojox.charting.themes.PlotKit.purple");
		public static Theme Red = new Theme("dojox.charting.themes.PlotKit.red");
		
		public string Path {
			get {
				return path;
			}
			set {
				path = value;
			}
		}
		
	}
}
