// Plot.cs
//	
//

using System;
using System.Collections.Generic;

namespace EmergeTk.Widgets.Html
{
	public enum PlotType
	{
		StackedColumns,
		Bars,
		Columns,
		StackedLines,
		Markers,
		Areas,
		Scatter,
		ClusteredColumns,
		Bubble,
		Pie
	}
	
	public class Plot : IJSONSerializable
	{
		PlotType type;
		string name;
		List<Series> series;
		string font;
		string fontColor;
		int labelOffset;
		
		int gap = 2;
		public int Gap { get { return gap; } set {gap = value;} }
		
		public PlotType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public List<Series> Series {
			get {
				return series;
			}
			set {
				series = value;
			}
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public bool IsDeserializing { get; set; }

		public string Font {
			get {
				return font;
			}
			set {
				font = value;
				jsonData["font"] = font;
			}
		}

		public int LabelOffset {
			get {
				return labelOffset;
			}
			set {
				labelOffset = value;
				jsonData["labelOffset"] = value;
			}
		}

		public string FontColor {
			get {
				return fontColor;
			}
			set {
				fontColor = value;
				jsonData["fontColor"] = value;
			}
		}
		
		public void AddSeries( params Series[] seriesParam )
		{
			if( series == null )
				series = new List<Series>();
			series.AddRange( seriesParam );
		}
		
		public Plot()
		{
		}
		
		Dictionary<string,object> jsonData = new Dictionary<string,object>();
		public Plot( PlotType t, string name )
		{
			this.type = t;
			this.name = name;
		}

		public System.Collections.Generic.Dictionary<string, object> Serialize ()
		{
			jsonData["name"] = name;
			jsonData["series"] = series;
			jsonData["type"] = type;
			jsonData["gap"] = Gap;
						
			return jsonData;
		}

		public void Deserialize (System.Collections.Generic.Dictionary<string, object> json)
		{
			throw new NotImplementedException();
		}
	}
}
