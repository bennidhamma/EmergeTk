// AxisLabel.cs
//	
//

using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Widgets.Html
{
	
	
	public class AxisLabel : IJSONSerializable
	{
		public float Value { get; set; }
		public string Text { get; set; }

		public bool IsDeserializing {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}
		
		public AxisLabel()
		{
		}
		
		public AxisLabel(float v, string t )
		{
			this.Value = v;
			this.Text = t;
		}

		public System.Collections.Generic.Dictionary<string, object> Serialize ()
		{
			Dictionary<string,object> r = new Dictionary<string,object>();
			r["value"] = Value;
			r["text"] = Text;
			
			return r;
		}

		public void Deserialize (System.Collections.Generic.Dictionary<string, object> json)
		{
			throw new NotImplementedException();
		}
		
		
	}
}
