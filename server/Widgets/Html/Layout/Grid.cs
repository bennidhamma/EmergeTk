// Grid.cs created with MonoDevelop
// User: ben at 1:01 A 05/01/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Drawing;
using EmergeTk;
using System.Xml;

namespace EmergeTk.Widgets.Html
{
	public class Grid : Generic
	{		
		public Grid()
		{
		}
		
		int rows = 3, columns = 3;
		HtmlElement [,] grid = null;
		bool useRowHeaders = true;
		public event EventHandler<ClickEventArgs> OnCellClickHandler;
		
		public int Rows {
			get {
				return rows;
			}
			set {
				rows = value;
			}
		}

		public int Columns {
			get {
				return columns;
			}
			set {
				columns = value;
			}
		}

		public HtmlElement[,] Cells {
			get {
				if( grid == null ) initGrid();
				return grid;
			}
			set {
				grid = value;
			}
		}
		
		public Point GetCellPoint( HtmlElement cell )
		{
			if( grid == null )
				return Point.Empty;
			for( int x = 0; x < columns; x++ )
				for( int y = 0; y < rows; y++ )
					if( grid[x,y] == cell )
						return new Point(x,y);
			return Point.Empty;
		}

		public bool UseRowHeaders {
			get {
				return useRowHeaders;
			}
			set {
				useRowHeaders = value;
			}
		}
	
		public override void ParseElement (XmlNode n)
		{
			if( grid == null ) initGrid();
			switch( n.LocalName )
			{
				case "Cell":
					HtmlElement td = RootContext.CreateWidget<HtmlElement>();
					int x = int.Parse(n.Attributes["Column"].Value);
					int y = int.Parse(n.Attributes["Row"].Value);
					n.Attributes.Remove(n.Attributes["Column"]);
					n.Attributes.Remove(n.Attributes["Row"]);
					grid[x,y] = td;
					td.ParseAttributes(n);
					td.ParseXml(n);
					break;
				default:
					base.ParseElement(n);
					break;
			}
		}

		private void initGrid()
		{
			grid = Array.CreateInstance(typeof(HtmlElement),columns,rows) as HtmlElement[,];
		}
		
		
		
		public override void Initialize ()
		{
			if( grid == null ) initGrid();
			TagName = "table";
			for( int y = 0; y < rows; y++ )
			{
				HtmlElement tr = RootContext.CreateWidget<HtmlElement>(this);
				tr.TagName = "tr";				
				for( int x = 0; x < columns; x++ )
				{
					HtmlElement td = RootContext.CreateWidget<HtmlElement>(tr);
					td.TagName = y == 0 && useRowHeaders ? "th" : "td";
					grid[x,y] = td;
					if( OnCellClickHandler != null )
					{
						td.OnClick += OnCellClickHandler;
					}
					//Literal l = RootContext.CreateWidget<Literal>(td);
					//l.Html = string.Format("{0},{1}", x, y);
				}
			}
		}
	
	}
}
