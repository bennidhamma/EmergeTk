using System;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class TogglePane : Generic
	{
		public Widget First { get; set; }
		public Widget Second { get; set; }
		public event EventHandler OnToggle;

		public override void Initialize ()
		{
			if( First != null && Second != null )
			{
				First.BindProperty( "Visible", Toggle );
				Second.BindProperty( "Visible", Toggle );
				Add( First, Second );
			}
		}

		bool toggling;
		public void Toggle()
		{
			if( First == null && Second == null && this.Widgets.Count >= 2)
			{
				First = this.Widgets[0];
				Second = this.Widgets[1];
			}
			if( toggling ) return;
			toggling = true;
			First.Visible = !First.Visible;
			Second.Visible = !Second.Visible;
			toggling = false;
			if( OnToggle != null )
				OnToggle(this, EventArgs.Empty);
		}

		public void Toggle(object sender, ClickEventArgs ea)
		{
			Toggle();
		}
	}
}
