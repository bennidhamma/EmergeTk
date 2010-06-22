// Series.cs
//	
//

using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class Series : IJSONSerializable
	{
		List<ChartPoint> points;		
		string name;
		string fill;
		string stroke;
		
		public string Stroke {
			get {
				return stroke;
			}
			set {
				stroke = value;
			}
		}
		
		public List<ChartPoint> Points {
			get {
				return points;
			}
			set {
				points = value;
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
		
		public string Fill {
			get {
				return fill;
			}
			set {
				fill = value;
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
		
		public void AddPoints( params ChartPoint[] pointsParam )
		{
			if( points == null )
				points = new List<ChartPoint>();
			points.AddRange( pointsParam );
		}
		
		Dictionary<string, object> json = new Dictionary<string,object>();
		public Series()
		{
			
		}

		public Dictionary<string, object> Serialize ()
		{
			if( points == null )
				return null;
			json["points"] = Points;
			return json;
		}

		public void Deserialize (Dictionary<string, object> json)
		{
			throw new NotImplementedException();
		}
	}
}
