// ThemeManager.cs created with MonoDevelop
// User: ben at 10:36 A 20/06/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using EmergeTk.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Text;
using System.Xml;

namespace EmergeTk
{
	public class ThemeManager
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ThemeManager));
		public static ThemeManager Instance = new ThemeManager();
		
		bool setup = false;
		object lockObject = new object();
		private string themesRoot, virtualRoot, physicalRoot;
		private List<string> activeThemes = new List<string>();
		private List<string> presentPathReference = new List<string>();
		private List<string> notPresentPathReference = new List<string>();
		private Dictionary<string,string> fileCache = new Dictionary<string,string>();
		private Dictionary<string,XmlNode> sXmlDocs = new Dictionary<string,XmlNode>();
		
		private ThemeManager()
		{
			Setup();
		}	
		
		public void Setup()
		{
			lock( lockObject )
			{
				if( ! setup )
				{
					log.Debug("setting up ThemeManager", HttpContext.Current);
					if( HttpContext.Current != null )
					{
						virtualRoot = System.Configuration.ConfigurationManager.AppSettings["application.root.url"] ?? "/";
                        try
                        {
                            physicalRoot = HttpContext.Current.Server.MapPath(virtualRoot);
                        }
                        catch (Exception ex)
                        {
                            physicalRoot = Setting.Get("ApplicationRoot").DataValue;
                            log.Error("Failed to MapPath", virtualRoot, physicalRoot, ex);
                        }
						themesRoot = Path.Combine( physicalRoot, "Themes" + System.IO.Path.DirectorySeparatorChar);
						log.Debug("roots:", physicalRoot, virtualRoot, themesRoot );
						DirectoryInfo di = new DirectoryInfo(themesRoot);
						
						if( Setting.GetValueT<bool>("MonitorFileSystem",false) && di != null )
						{
							
							//setup a watcher for each theme
							foreach( DirectoryInfo themeDir in di.GetDirectories() )
							{								
								if( ( themeDir.Attributes & FileAttributes.Hidden ) == 0 )
									SetupWatcher( themeDir.Name, themeDir.FullName );
							}
						}
						
					}
					setup = true;
				}
			}
		}
		
		public FileInfo RequestNewPhysicalFilePath( string name )
		{
			string path = Path.Combine( Context.Current.Theme, name );
			
			return new FileInfo( Path.Combine( themesRoot, path ) );
		}
		
		public FileInfo RequestFile( string name )
		{
			////log.Debug("RequestFile: ", name );
			
			return RequestFile( Context.Current.Theme, name );
		}
		
		public string RequestFileAsString( string name )
		{
			////log.Debug("RequestFile: ", name );
			if( fileCache.ContainsKey(name) )
				return fileCache[name];
			FileInfo fi = RequestFile( Context.Current.Theme, name );
			//log.Info(name, fi);
			if( fi == null || ! fi.Exists )
			{
				
				fileCache[name] = null;
				return null;
			}
			if(! fileCache.ContainsKey( fi.FullName ) )
			{
				TextReader tr = new StreamReader(fi.FullName);	
				fileCache[fi.FullName] = tr.ReadToEnd();
			}
			
			return fileCache[fi.FullName];
		}
		
		public FileInfo RequestFile( string theme, string name )
		{
			/* param in would be something like: EmergeTk.Widgets.Html.FileController.xml
			
			test order: 
			
			/Themes/Views/Martian/EmergeTk/Widgets/Html/FileController.xml
			/Themes/Views/Default/EmergeTk/Widgets/Html/FileController.xml
			
			*/

			//log.Debug("RequestFile: ", theme, name );
			
			//log.Debug( "COMBINING: ", theme, name, Path.Combine( theme, name ) , Path.Combine("Default", name ) );
			
			return InternalResolveFile( Path.Combine( theme, name ) ) ?? InternalResolveFile( Path.Combine("Default", name ) );			
		}
		
		public string RequestClientPath(string name)
		{
			//log.Debug("requesting path ", name);
			FileInfo fi = RequestFile( name );
			//log.Debug("returined fileinfo: ", fi);
			if( fi != null )
			{
				string ret = Setting.VirtualRoot + "/" + fi.FullName.Replace(physicalRoot,string.Empty).Replace("\\", "/");
				//log.Debug(ret);
				return ret;
			}
			return string.Empty;
		}
		
		public string RequestScriptPath(string name)
		{
			//log.Debug("Trying to get script at ",  Path.Combine("Scripts/", name ) );
			return RequestClientPath( Path.Combine("Scripts/", name ) );
		}
		
		public string RequestScriptBlock(string name)
		{			
			//log.Warn("RequestScriptBlock",name);
			string path = RequestScriptPath( name );
			//log.Warn("RequestScriptPath path is ",path);
			if( path != string.Empty )
			{
				return string.Format("\t\t\t<script src=\"{0}\" type=\"text/javascript\"></script>\n", path );
			}
			else if ( name.Contains("/") )
			{
				return string.Format("\t\t\t<script src=\"{0}/{1}\" type=\"text/javascript\"></script>\n", Setting.VirtualRoot, name );
			}
			//log.Error("RequestScriptBlock returning empty",name);
			return string.Empty;
		}
		
		/// <summary>
		/// Builds a scripts block to be included in the html header of a context request.
		/// Will load scripts from Themes/Default/Scripts, Themes/[CURRENT_THEME]/Scripts, and
		/// optionally from a subfolder in either of the two above mentioned paths correlating to
		/// the context name.
		/// </summary>
		/// <param name="contextPath">
		/// A <see cref="System.String"/> that represents an additional path to search for scripts.
		/// For example, the full type-path of the EmergeTk.Context.
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> containing HTML to include the discovered scripts.
		/// </returns>
		public string RequestScriptsBlock(string contextPath)
		{
			//first add default styles, then theme overrides.
			List<string> filePaths = new List<string>();
			string basePath = Path.Combine( themesRoot, "Default/Scripts" );
			//log.Warn("1base path",basePath);
			GetAllFilePaths( basePath, filePaths, false );
			
			StringBuilder sb = new StringBuilder();
								
			foreach( string path in filePaths )
			{
				//log.Warn("1script path",path);
				if( path.EndsWith(".js") )
					sb.Append( RequestScriptBlock(path.Replace(physicalRoot, string.Empty).Replace("\\", "/")));

				if( path.EndsWith(Context.Current.HttpContext.Request.Browser.Type) || 
				    path.EndsWith(Context.Current.HttpContext.Request.Browser.Browser) )
					sb.Append( RequestScriptBlock(path.Replace(physicalRoot, string.Empty).Replace("\\", "/")));
			}
			
			if( Context.Current != null && Context.Current.Theme != null )
			{	
				string themeRoot = Path.Combine( themesRoot, Context.Current.Theme );
				basePath = Path.Combine( themeRoot, "Scripts" );
				//log.Warn("2base path",basePath);
				filePaths.Clear();
				GetAllFilePaths( basePath, filePaths, false );
				//log.Debug("number of filePaths: " + filePaths.Count);
				
				foreach (string path in filePaths ) {
					//log.Warn("2script path",path);
					if( path.EndsWith(".js") )
						sb.Append( RequestScriptBlock(path.Replace(physicalRoot, string.Empty).Replace("\\", "/")));
					
					if( path.EndsWith(Context.Current.HttpContext.Request.Browser.Type) || 
					    path.EndsWith(Context.Current.HttpContext.Request.Browser.Browser) )
						sb.Append( RequestScriptBlock(path.Replace(physicalRoot, string.Empty).Replace("\\", "/")));
				}
				
				if( contextPath != null )
				{
					basePath = Path.Combine( basePath, contextPath );
					//log.Warn("3base path",basePath);
					//log.Warn("3contextPath",contextPath);
					filePaths.Clear();
					GetAllFilePaths( basePath, filePaths, false );
					
					foreach (string path in filePaths ) {
						//log.Warn("3script path",path);
						if( path.EndsWith(".js") )
							sb.Append( RequestScriptBlock(path.Replace(physicalRoot, string.Empty).Replace("\\", "/")));
						
						if( path.EndsWith(Context.Current.HttpContext.Request.Browser.Type) || 
						    path.EndsWith(Context.Current.HttpContext.Request.Browser.Browser) )
							sb.Append( RequestScriptBlock(path.Replace(physicalRoot, string.Empty).Replace("\\", "/")));
					}	
				}
			}

			return sb.ToString();
		}
		
		public string RequestStylePath(string name)
		{
			return RequestClientPath( Path.Combine("Styles/", name ) );
		}
		
        /// <summary>
        /// Builds an xhtml style block for inclusion in web pages.
        /// Browser specific css files should contain 2 or more periods like style.IE.css
        /// </summary>
        /// <param name="contextPath">typename of current EmergeTk context - looks for stylesheets using corresponding folder structure</param>
        /// <returns>string containing style tag and children</returns>
		public string RequestStylesBlock(string contextPath)
		{
			List<string> filePaths = new List<string>();
			string basePath = Path.Combine( themesRoot, "Default/Styles" );
            string browserTypeSuffix = Context.Current.HttpContext.Request.Browser.Type + ".css";
            string browserSuffix = Context.Current.HttpContext.Request.Browser.Browser + ".css";
			GetAllFilePaths( basePath, filePaths, false );
			
			StringBuilder sb = new StringBuilder();
			sb.Append("<style type=\"text/css\">\n");

            //first add default styles, then theme overrides.
            foreach (string path in filePaths)
			{
				if( path.EndsWith(".css") && (path.IndexOf(".") == path.IndexOf(".css")) )
					sb.Append( string.Format( "\t@import \"{0}/{1}\";\n", Setting.VirtualRoot, path.Replace(physicalRoot, string.Empty).Replace("\\", "/") ) );
				
                if (path.EndsWith(browserTypeSuffix) ||
                    path.EndsWith(browserSuffix))
					sb.Append( string.Format( "\t@import \"{0}/{1}\";\n", Setting.VirtualRoot, path.Replace(physicalRoot, string.Empty).Replace("\\", "/") ) );									
			}
			
			if( Context.Current != null && Context.Current.Theme != null )
			{	
				string themeRoot = Path.Combine( themesRoot, Context.Current.Theme );
				basePath = Path.Combine( themeRoot, "Styles" );
				filePaths.Clear();
				GetAllFilePaths( basePath, filePaths, false );
								
				foreach (string path in filePaths ) {
//					log.Debug("RequestStyleBlock adding styles from path2",path);
                    if (path.EndsWith(".css") && (path.IndexOf(".") == path.IndexOf(".css")))
						sb.Append( string.Format( "\t@import \"{0}/{1}\";\n", Setting.VirtualRoot, path.Replace(physicalRoot, string.Empty).Replace("\\", "/") ) );
					
                    if (path.EndsWith(browserTypeSuffix) ||
                        path.EndsWith(browserSuffix))
						sb.Append( string.Format( "\t@import \"{0}/{1}\";\n", Setting.VirtualRoot, path.Replace(physicalRoot, string.Empty).Replace("\\", "/") ) );
				}
				
				if( contextPath != null )
				{
					basePath = Path.Combine( basePath, contextPath );
					filePaths.Clear();
					GetAllFilePaths( basePath, filePaths, false );
									
					foreach (string path in filePaths ) {
					    if (path.EndsWith(".css") && (path.IndexOf(".") == path.IndexOf(".css")))
							sb.Append( string.Format( "\t@import \"{0}/{1}\";\n", Setting.VirtualRoot, path.Replace(physicalRoot, string.Empty).Replace("\\", "/") ) );
						
                        if (path.EndsWith(browserTypeSuffix) ||
                            path.EndsWith(browserSuffix))
							sb.Append( string.Format( "\t@import \"{0}/{1}\";\n", Setting.VirtualRoot, path.Replace(physicalRoot, string.Empty).Replace("\\", "/") ) );					
					}	
				}
			}

			sb.Append("</style>\n");
			return sb.ToString();
		}
		
		public void GetAllFilePaths( string source, List<string> files, bool recursive )
		{
			DirectoryInfo di = new DirectoryInfo( source );
			if( ! di.Exists || ( di.Attributes & FileAttributes.Hidden ) == FileAttributes.Hidden )
				return;
			foreach( FileInfo fi in di.GetFiles() )
			{
				if( EnsureValidFile( fi ) )
					files.Add( fi.FullName );
			}
			if( recursive )
			{
				foreach( DirectoryInfo di2 in di.GetDirectories() )
				{
					GetAllFilePaths( di2.FullName, files, true );
				}
			}
		}
		
		public bool EnsureValidFile( FileInfo fi )
		{
			string filepath = fi.FullName;
			#if DEBUG
				//log.Debug( "ensuring valid file path for ", filepath, ! filepath.Contains("~") && ! filepath.Contains("#") ); 
			#endif
			return ! filepath.Contains("~") && ! filepath.Contains("#") && ( fi.Attributes & FileAttributes.Hidden ) != FileAttributes.Hidden
				&& (fi.Attributes & FileAttributes.Hidden ) != FileAttributes.Temporary;
		}
		
		public XmlNode RequestView( string name )
		{
			return RequestView( name, "Widget" );
		}
		
		public XmlNode RequestView( string name, string rootNodeName )
		{
			////log.Debug("RequestXmlDocument: ", name );
			return RequestView( Context.Current.Theme, name, rootNodeName );
		}

		public XmlNode RequestView( string theme, string name, string rootNodeName )
		{
			FileInfo fi = RequestFile( theme, Path.Combine("Views/", name ) );
			
			if( fi == null )
				return null;
			string path = fi.FullName.Replace( themesRoot, string.Empty );

			if( sXmlDocs.ContainsKey( path ) )
			{
				if( sXmlDocs[path] != null  )
					return sXmlDocs[path].Clone();
			}
			else
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(fi.FullName);
                XmlNode rootNode = doc.SelectSingleNode(rootNodeName);
                sXmlDocs[path] = rootNode.Clone();
                return rootNode; 
		    }
		    return null;
		}
		
		private FileInfo InternalResolveFile( string path )
		{
			//log.Debug( "InternalResolveFile: ", themesRoot, path, Path.Combine( themesRoot, path ) );
			string physicalPath = Path.Combine( themesRoot, path );
			FileInfo fi = null;
			
			if( presentPathReference.BinarySearch( path ) >= 0 )
			{
				//log.Debug("file in presentPathRef");
				fi = new FileInfo( physicalPath );
			}
			else if( notPresentPathReference.BinarySearch( path ) < 0 )
			{
				if( File.Exists( physicalPath ) )
				{
					//log.Debug("file exists");
					presentPathReference.Add( path );
					fi = new FileInfo(physicalPath);
				}
				else
				{
					//log.Debug("file does not exist.  adding to not present ref");
					notPresentPathReference.Add( path );
				}
			}
			//log.Debug("returning ", fi);
			return fi;
		}
		
		private void SetupWatcher( string name, string path )
		{
			log.Debug("Setting up watcher for ", path );
			activeThemes.Add( name );
			FileSystemWatcher watcher = new FileSystemWatcher( path );
			//watcherToTheme.Add( watcher, name );
			watcher.IncludeSubdirectories = true;
			watcher.Created += new FileSystemEventHandler( CreatedEvent );
			watcher.Deleted += new FileSystemEventHandler( DeletedEvent );
			watcher.Renamed += new RenamedEventHandler( RenamedEvent );
			watcher.Changed += new FileSystemEventHandler( ChangedEvent );
			watcher.EnableRaisingEvents = true;
		}
		
		private void CreatedEvent(object o, FileSystemEventArgs fea )
		{
			FileInfo fi = new FileInfo(fea.FullPath);
			if( ! EnsureValidFile( fi ) )
				return;
			log.Debug("CreatedEvent: ", fea.ChangeType, fea.FullPath, fea.Name );
			AddFile( fea.FullPath );
		}
		
		private void DeletedEvent(object o, FileSystemEventArgs fea )
		{
			FileInfo fi = new FileInfo(fea.FullPath);
			if(  ! EnsureValidFile( fi )  )
				return;
			log.Debug("DeletedEvent: ", fea.ChangeType, fea.FullPath, fea.Name );
			RemoveFile( fea.FullPath );
		}
		
		private void AddFile(string f )
		{
			log.Debug("AddFile: ", f);
			string themePath = f.Replace(themesRoot,string.Empty);
			if( presentPathReference.BinarySearch( themePath ) < 0 )
			{
				//log.Debug("Adding ", f);
				presentPathReference.Add( themePath );
				presentPathReference.Sort();
			}
			if( sXmlDocs.ContainsKey( themePath ) )
			{
				//log.Debug("Removing XmlDoc cache for ", path );
				sXmlDocs.Remove( themePath );
			}
			if( fileCache.ContainsKey(f) )
				fileCache.Remove(f);
		}
		
		private void RemoveFile(string f )
		{
			log.Debug( "RemoveFile: ", f );
			string themePath = f.Replace(themesRoot,string.Empty);
			if( presentPathReference.BinarySearch( themePath ) >= 0 )
			{
				//log.Debug( "Removing theme file ", f );
				presentPathReference.Remove( themePath );
			}
			if( sXmlDocs.ContainsKey( themePath ) )
			{
				//log.Debug("Removing XmlDoc cache for ", path );
				sXmlDocs.Remove( themePath );
			}
			if( fileCache.ContainsKey(f) )
				fileCache.Remove(f);

		}
		
		private void RenamedEvent( object sender, RenamedEventArgs rea )
		{
			log.Debug("RenamedEvent: ", rea.ChangeType, rea.FullPath, rea.Name, rea.OldFullPath, rea.OldName );
			AddFile( rea.FullPath );
			RemoveFile( rea.OldFullPath );
		}
		
		private void ChangedEvent(object o, FileSystemEventArgs fea )
		{
			log.Debug( "ChangedEvent:", fea.FullPath  ); 
			FileInfo fi = new FileInfo(fea.FullPath);
			if( ! EnsureValidFile( fi )  )
				return;

			//log.Debug("ChangedEvent: ", fea.ChangeType, fea.FullPath, fea.Name );
			string path = fea.FullPath.Replace(themesRoot,string.Empty);
			if( sXmlDocs.ContainsKey( path ) )
			{
				//log.Debug("Removing XmlDoc cache for ", path );
				sXmlDocs.Remove( path );
			}
			if( fileCache.ContainsKey( fea.FullPath ) )
			{
				fileCache.Remove( fea.FullPath );
			}
		}
		
	}
}
