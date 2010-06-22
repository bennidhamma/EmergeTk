
using System;
using EmergeTk;

namespace EmergeTk.Widgets.Html
{
	public class Fieldset : Generic
	{
		public Fieldset ()
		{
			this.TagName = "fieldset";
		}
		
		private string legend;
		public string Legend {
			get {
				return legend;
			}
			set {
				legend = value;
				SetupLegend ();
			}
		}
		
		Label legendLabel;
		
		public override void Initialize ()
		{
		}
		
		private void SetupLegend ()
		{
			if( legendLabel == null )
			{
				if( !string.IsNullOrEmpty(legend) )
				{
					legendLabel = Label.InsertLabel(this,"legend",legend);
				}
			}
			else
				legendLabel.Text = legend;
		}
	}
}
