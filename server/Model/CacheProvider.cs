// ICacheProvider.cs created with MonoDevelop
// User: ben at 10:46 AM 12/24/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Mono.Addins;
using Mono.Addins.Description;

namespace EmergeTk.Model
{
	//[TypeExtensionPoint ("/EmergeTk/Model")]
	
	public class CacheProvider
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(CacheProvider));
		
		static bool enableCaching;
		public static bool EnableCaching {
			get 
			{
				return enableCaching && Instance != null;
			}
		}
		
		static bool isSingleServer;
		public static bool IsSingleServer {
			get 
			{
				return isSingleServer;
			}
		}
		
		static private ICacheProvider instance;
		static public ICacheProvider Instance
		{
			get
			{
				//log.Debug("getting instance", instance, AddinHost.Running);
				if( instance == null )
				{
					start();
				}
				return instance;
			}
			set
			{
				instance = value;
			}
		}

		static void debug()
		{
			log.Info("debugging addins " + AppDomain.CurrentDomain.Id );

			//AddinManager.LoadAddin(null, "EmergeTk");
			//AddinManager.LoadAddin(null, "MemCached");
			
			log.Info("addin EmergeTk loaded?" + AddinManager.IsAddinLoaded("EmergeTk") );
			log.Info("addin MemCached loaded?" + AddinManager.IsAddinLoaded("MemCached") );

				foreach( Addin a in AddinManager.Registry.GetAddinRoots() )
				{
					log.Debug("addin root ", a, a.Enabled );
					foreach( ExtensionPoint e in a.Description.ExtensionPoints )
					{
						log.DebugFormat("extension point {0} {1} {2}", e, e.Name, e.Path );
						foreach( ExtensionNodeType n in e.NodeSet.NodeTypes )
						{
							log.DebugFormat( "ext node {0} {1}", n.Id, n.NodeName );
						}
					}
				}
	
				foreach( Addin a in AddinManager.Registry.GetAddins() )
				{
					log.Debug("addin" + a );
					
					foreach( ModuleDescription md in a.Description.AllModules )
					{
						//ExtensionNodeDescription end = md.Extensions[0].ExtensionNodes[0];
						//log.Debug("module", end.NodeName, end.Id, end.GetNodeType().TypeName);
						
						foreach( Extension e in md.Extensions )
						{
							log.Debug("extension: " + e.Path );
							foreach( ExtensionNodeDescription end in e.ExtensionNodes )
							{
								ExtensionNodeType ent = end.GetNodeType();
								
								log.Debug("ext node:" + end.NodeName );
								if( ent != null )
								{
									log.DebugFormat("e node type {0} {1} {2} {3}",
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

		static void start()
		{
			string path = "/EmergeTk/Model/CacheProvider";
			if( AddinHost.Running )
			{
				try
				{
					debug();
					log.Debug("looking for extensions to " + path);
					foreach (ICacheProvider c in AddinManager.GetExtensionObjects ( path ) )
					{
						instance = c;
						log.Debug("Using CacheProvider: ", instance );
					}
	
					log.Debug("looking for extensions to " + path);
	
					foreach( ExtensionNode node in AddinManager.GetExtensionNodes(path) )
					{
						log.Debug("found extensions for ", node );
					}
					
					AddinManager.AddExtensionNodeHandler(path, OnExtensionChanged);
				}
				catch(Exception e )
				{
					log.Error("Error loading cache provider addins", e );
				}

				
			}
			else
			{
				AddinHost.OnAddinStart += delegate {
					instance = null;	
				};
			}
			
			if( instance == null )
			{
				instance = CacheManager.Manager;
			}
			
			log.Debug("Using CacheProvider: ", instance, AddinHost.Running );
		}
	
		static CacheProvider()
		{
			enableCaching = Setting.GetValueT<bool>("EnableCaching");
			isSingleServer = Setting.GetValueT<bool>("IsSingleServer");
		}

		static void OnExtensionChanged( object sender, ExtensionNodeEventArgs args )
		{
			log.Debug("extension changed", args.Change, args.Path, args.ExtensionNode.Id );
			if( args.Change == ExtensionChange.Remove )
			{
				instance = null;
			}
			else
			{
				TypeExtensionNode node = args.ExtensionNode as TypeExtensionNode;
				object i = node.CreateInstance();
				Type icp1 = i.GetType().GetInterface("ICacheProvider");
				Type icp2 = typeof(ICacheProvider);
				log.Debug("adding from node ", 
				          node, 
				          i, 
				          icp1 == icp2,
				          icp1.AssemblyQualifiedName,
				          icp2.AssemblyQualifiedName);
				instance = (ICacheProvider)node.CreateInstance();
			}

			log.Debug("Using CacheProvider: ", instance );
		}
	}
}
