using System;
using System.Collections.Generic;
using System.Text;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;
using Mono.Addins;
using Mono.Addins.Description;

namespace EmergeTk.Administration
{
	
	public class Addins : Generic, IAdmin
	{
		
		#region IAdmin implementation 
		
		public string AdminName {
			get {
				return "Addins";
			}
		}
		
		public string Description {
			get {
				return "Configure addins for your application.";
			}
		}
		
		public Permission AdminPermission
		{
			get {
				return Permission.GetOrCreatePermission("View Addins");
			}	
		}
		
		#endregion 
		

		
		public Addins()
		{
		}

		public override void Initialize ()
		{
			log.Debug("initializing Addins");
			if( ! AddinHost.Running )
				return;

			log.Debug("updating");
			
			//AddinManager.Registry.Update(null);

			log.Debug("Registry: ", AddinManager.Registry.RegistryPath);
			
			foreach( Addin a in AddinManager.Registry.GetAddinRoots() )
			{
				log.Debug("addin root", a );
				foreach( ExtensionPoint e in a.Description.ExtensionPoints )
				{
					log.Debug("extension point ", e, e.Name, e.Path );
					foreach( ExtensionNodeType n in e.NodeSet.NodeTypes )
					{
						log.Debug( "ext node", n.Id, n.NodeName );
					}
				}		
			}

			foreach( Addin a in AddinManager.Registry.GetAddins() )
			{
				log.Debug("addin", a );
				
				Add(SetupAddinManager(a));
				
				foreach( ModuleDescription md in a.Description.AllModules )
				{
					//ExtensionNodeDescription end = md.Extensions[0].ExtensionNodes[0];
					//log.Debug("module", end.NodeName, end.Id, end.GetNodeType().TypeName);
					
					foreach( Extension e in md.Extensions )
					{
						log.Debug("extension: ", e.Path );
						foreach( ExtensionNodeDescription end in e.ExtensionNodes )
						{
							ExtensionNodeType ent = end.GetNodeType();
							
							log.Debug("ext node:", end.NodeName );
							if( ent != null )
							{
								log.Debug("e node type",
								          ent.ObjectTypeName, 
								          ent.Description, 
								          ent.TypeName,
								          ent.NodeTypes );
							}
						}
					}	
				}
			}
		}

		private Widget SetupAddinManager(Addin a)
		{
			Pane p = RootContext.CreateWidget<Pane>();
			SelectItem si = RootContext.CreateWidget<SelectItem>(p);
			si.Selected = a.Enabled;
			si.OnChanged += delegate(object sender, ChangedEventArgs e) {
				if( si.Selected )
					AddinManager.Registry.EnableAddin(a.Id);
				else
					AddinManager.Registry.DisableAddin(a.Id);

				log.DebugFormat("Addin {0} Enabled State: {1}", a.Id, a.Enabled );
			};
			Label.InsertLabel( p, "h2", a.Name );
			Label.InsertLabel( p, "p", a.Description.Description );

			return p;
		}
	}
}
