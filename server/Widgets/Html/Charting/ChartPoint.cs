// ChartPoint.cs
//	
//

using System;
using System.Collections.Generic;
using EmergeTk;

namespace EmergeTk.Widgets.Html
{
	public class ChartPoint : IJSONSerializable
	{
		float x;
		float y;
		float size;
		string name;
		string legend;		
		string color;
		string fontColor;
		
		public float Y {
			get {
				return y;
			}
			set {
				y = value;
				json["y"] = y;
			}
		}
		
		public float X {
			get {
				return x;
			}
			set {
				x = value;
				json["x"] = x;
			}
		}
		
		public float Size {
			get {
				return size;
			}
			set {
				size = value;
				json["size"] = size;
			}
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
				json["name"] = name;
			}
		}
		
		public string Legend {
			get {
				return legend;
			}
			set {
				legend = value;
				json["text"] = legend;
			}
		}
		
		public string FontColor {
			get {
				return fontColor;
			}
			set {
				fontColor = value;
				json["fontColor"] = fontColor;
			}
		}
		
		public string Color {
			get {
				return color;
			}
			set {
				color = value;
				json["color"] = color;
			}
		}

		public bool IsDeserializing {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}
		
		public ChartPoint(float y, string legend )
		{
			json = new Dictionary<string,object>();
			this.Y = y;
			this.Legend = legend;
		}
		
		Dictionary<string, object> json;
		public ChartPoint()
		{
			json = new Dictionary<string,object>();
		}

		public System.Collections.Generic.Dictionary<string, object> Serialize ()
		{
			return json;
		}

		public void Deserialize (System.Collections.Generic.Dictionary<string, object> json)
		{
			throw new NotImplementedException();
		}
	}
}
