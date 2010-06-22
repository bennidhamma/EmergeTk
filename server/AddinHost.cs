using System;
using Mono.Addins;
using Mono.Addins.Description;
using log4net;
using log4net.Core;

//[assembly:AddinRoot ("EmergeTk", "1.0")]

namespace EmergeTk
{
	public class ProgressMonitor : IProgressStatus
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(ProgressMonitor));
		#region IProgressStatus implementation
		public void SetMessage (string msg)
		{
			log.Debug("Status: " + msg);
		}
		
		public void SetProgress (double progress)
		{
			log.Debug("Progress: " + progress);
		}
		
		public void Log (string msg)
		{
			log.Debug(msg);
		}
		
		public void ReportWarning (string message)
		{
			log.Warn(message);
		}
		
		public void ReportError (string message, Exception exception)
		{
			log.Error(message, exception);
		}
		
		public void Cancel ()
		{
			isCanceled = true;
		}
		
		
		public int LogLevel {
			get {
				log.Debug("requesting loglevel");
				return 5;
			}
		}
		
		bool isCanceled = false;
		public bool IsCanceled {
			get {
				return isCanceled;
			}
		}
		#endregion
		
	}
	
	public class AddinHost
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(AddinHost));
		static object startLock = new object();
		
		public static event EventHandler OnAddinStart;
		
		static AddinHost()
		{
			
		}
		
		private AddinHost()
		{
		}

		static public void Startup(string path)
		{
			lock(startLock)
			{
				if( running)
				{
					log.Warn("Already running");
					return;
				}			
				
				log.Debug("starting up addin manager at " + path );
	
				AddinManager.AddinLoaded += delegate(object sender, AddinEventArgs args) {
					log.Debug("addin loaded : "  + args.AddinId);	
				};
	
				AddinManager.AddinLoadError += delegate(object sender, AddinErrorEventArgs args) {
					log.DebugFormat("addin load error {0} {1} {2}" , args.AddinId, args.Message, args.Exception );	
				};
	
				AddinManager.AddinUnloaded += delegate(object sender, AddinEventArgs args) {
					log.Debug("addin unloaded: " + args.AddinId);	
				};
				
				log.Info("Initializing Addin Manager");
				AddinManager.Initialize( path );
				
				log.Info("Rebuilding Addin Registry");
				//AddinManager.Registry.Rebuild(new ProgressMonitor());
				
				log.Info("Updating Addin Registry");
				AddinManager.Registry.Update(new ProgressMonitor());
				
				Addin addinRoot = AddinManager.Registry.GetAddin("EmergeTk");
				log.Debug("EmergeTk addin: " + addinRoot );
				
				log.Debug("Initialized");
				log.Info("App Domain ID " + AppDomain.CurrentDomain.Id );
				log.Info("Addin Registry Default Folder " + AddinManager.Registry.DefaultAddinsFolder );
				log.Info("Addin Registry Addins " + AddinManager.Registry.GetAddins().Length );

				foreach (ExtensionNode node in AddinManager.GetExtensionNodes ("/Model/ISolrSearchProvider")) {
					log.Debug("SearchProvider: " + node.Id);
				}
	
				log.Debug("scanning addin-roots" );
				foreach( Addin a in AddinManager.Registry.GetAddinRoots() )
				{
					log.Debug("addin root " + a.Id );
					
					if( a.Enabled && ! AddinManager.IsAddinLoaded( a.Id ) )
					{
						log.Debug("loading addin root " + a.Id );
						AddinManager.LoadAddin(null,a.Id);
					}
					
					foreach( ExtensionPoint e in a.Description.ExtensionPoints )
					{
						log.DebugFormat("extension point {0} {1} {2}", e, e.Name, e.Path );
						foreach( ExtensionNodeType n in e.NodeSet.NodeTypes )
						{
							log.DebugFormat( "ext node {0} {1}", n.Id, n.NodeName );
						}
					}		
				}
				
				log.Debug("scanning addins" );
				foreach( Addin a in AddinManager.Registry.GetAddins() )
				{
					log.Debug("addin " + a );

					if( a.Enabled && ! AddinManager.IsAddinLoaded( a.Id ) )
					{
						log.Debug("loading addin " + a.Id );
						AddinManager.LoadAddin(null,a.Id);
					}
					
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
	
				log.Debug("Done starting up");
				
				running = true;
				if( OnAddinStart != null )
				{
					OnAddinStart(null, EventArgs.Empty);
				}

			}
		}

		static bool running = false;
		public static bool Running
		{
			get
			{
				return running;
			}
		}
	}
}

