// Axis.cs
//	
//

using System;
using System.Collections.Generic;

namespace EmergeTk.Widgets.Html
{
	public enum AxisAngle 
	{
		Horizontal,
		Vertical
	}
	
	public class Axis : IJSONSerializable
	{
		AxisAngle angle;
		string name;
		List<AxisLabel> labels;
		public bool IncludeZero {get;set;}
		
		public bool IsDeserializing {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
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

		public AxisAngle Angle {
			get {
				return angle;
			}
			set {
				angle = value;
			}
		}

		public List<AxisLabel> Labels {
			get {
				return labels;
			}
			set {
				labels = value;
			}
		}
		
		public Axis()
		{
		}
		
		public Axis(string name, AxisAngle angle)
		{
			this.name = name;
			this.angle = angle;
		}


		public System.Collections.Generic.Dictionary<string, object> Serialize ()
		{
			Dictionary<string,object> r = new Dictionary<string,object>();
			r["name"] = name;
			if( angle == AxisAngle.Vertical )
			{
				r["vertical"] = true;
			}
			if( labels != null )
			{
				r["labels"] = labels;
			}
			if( IncludeZero )
			{
				r["includeZero"] = true;
			}
			
			return r;
		}

		public void Deserialize (System.Collections.Generic.Dictionary<string, object> json)
		{
			throw new NotImplementedException();
		}
	}
}
