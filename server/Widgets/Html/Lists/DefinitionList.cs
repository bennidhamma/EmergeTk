// DefinitionList.cs created with MonoDevelop
// User: ben at 1:00 P 13/04/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Xml;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	public class DefinitionList<T> : Repeater<T> where T : AbstractRecord, new()
	{		
		Template term, description; 
		public DefinitionList()
		{
			this.BodyTagName = "dl";
			this.OnRowAdded += new System.EventHandler<RowEventArgs<T>>( rowAdded );
		}
		
		private void rowAdded( object sender, RowEventArgs<T> ea )
		{
			//TODO: resolve any issues that arise from having twice as many elements as expected in the parent.
			if( description != null )
			{
				Template termItem = ea.Template;
				Template descriptionItem = description.Clone() as Template;
				descriptionItem.DataBindWidget(termItem.Record);			
				termItem.InsertAfter( descriptionItem );
			}
			else
				log.Info("Term inserted into DefinitionList without description");
		}
		
		public override void ParseElement (XmlNode n)
		{
			switch( n.LocalName )
			{
				case "Term":
					term = RootContext.CreateWidget<Template>();
					term.TagName = "dt";
					term.ParseAttributes(n);
					term.ParseXml(n);
					Template = term;
					
					break;
				case "Description":
					description = RootContext.CreateWidget<Template>();
					description.TagName = "dd";
					description.ParseAttributes(n);
					description.ParseXml(n);
					break;
				default:
					base.ParseElement (n);	
					break;
			}			
		}

	}
}
