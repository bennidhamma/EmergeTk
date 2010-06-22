// PieChart.cs
//	
//

using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Widgets.Html
{
	
	
	public class Chart : Widget
	{
		List<Plot> plots;
		List<Axis> axes;
		Theme theme;

		public List<Plot> Plots {
			get {
				return plots;
			}
			set {
				plots = value;
			}
		}

		public List<Axis> Axes {
			get {
				return axes;
			}
			set {
				axes = value;
			}
		}

		public Theme Theme {
			get {
				return theme;
			}
			set {
				theme = value;
			}
		}
		
		public void AddPlot( Plot p )
		{
			if( plots == null )
				plots = new List<Plot>();
			plots.Add( p );
		}
		
		public void AddAxis( Axis a )
		{
			if( axes == null )
				axes = new List<Axis>();
			axes.Add( a );
		}
		
		public void Commit()
		{
			if( plots != null )
				SetClientAttribute("plots", JSON.Default.Encode(plots) );
			if( axes != null )
				SetClientAttribute("axes", JSON.Default.Encode( axes ) );
			if( theme != null )
				SetClientAttribute("theme", Util.ToJavaScriptString(theme.Path) );
		}
		
		public Chart()
		{
		}
	}
}
