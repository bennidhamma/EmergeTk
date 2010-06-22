// HoverBox.cs created with MonoDevelop
// User: ben at 10:35 P 03/01/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using EmergeTk.Model;
using System.ComponentModel;

namespace EmergeTk.Widgets.Html
{
	public class HoverBox : Widget
	{
		public HoverBox()
		{
		}
		
		Widget hoverContainer;
		
		public Widget HoverContainer {
			get {
				return hoverContainer;
			}
			set {
				hoverContainer = value;
				if( value != null )
				{
					string id = value.ClientId + ".elem";
					SetClientAttribute("hoverContainer",id);
					if( hoverContainer.IsAncestorOf( this ) )
						hoverContainer.OnClone += new EventHandler<EmergeTk.CloneEventArgs>( cloneHandler );
				}
			}
		}
		
		public void cloneHandler( object sender, CloneEventArgs ea )
		{			
			HoverBox hb = ea.Destination.Find<HoverBox>(Id);
			hb.HoverContainer = ea.Destination;
		}
		
		public override void Initialize ()
		{
			AppendClass( "hoverbox" );			
		}

	}
}
