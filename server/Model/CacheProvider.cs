// ICacheProvider.cs created with MonoDevelop
// User: ben at 10:46 AM 12/24/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

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
			set
			{
				enableCaching = value;
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
				return instance;
			}
			set
			{
				instance = value;
			}
		}

		static CacheProvider()
		{
			enableCaching = Setting.GetConfigT<bool>("EnableCaching");
			isSingleServer = Setting.GetConfigT<bool>("IsSingleServer");
		}
	}
}
