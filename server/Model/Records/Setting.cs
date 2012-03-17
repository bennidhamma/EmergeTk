using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace EmergeTk.Model
{
    public class Setting : AbstractRecord
	{
		string dataKey;
		string dataValue;
		DateTime created = DateTime.UtcNow;
	
		public virtual string DataValue {
			get {
				return dataValue;
			}
			set {
				dataValue = value;
			}
		}

		public virtual string DataKey {
			get {
                return dataKey;
			}
			set {
                dataKey = value;
			}
		}

		public virtual System.DateTime Created {
			get {
				return created;
			}
			set {
				created = value;
			}
		}

		static public T GetValueT<T>(String name)
		{
			Setting s = Get(name);
			if( s!= null && ! string.IsNullOrEmpty( s.DataValue ) )
				return (T)PropertyConverter.Convert(s.DataValue, typeof(T));
			else
				return default(T);
		}
		
		static public T GetConfigT<T>(String name)
		{
			Setting s = Get(name, null, true);
			if( s!= null && ! string.IsNullOrEmpty( s.DataValue ) )
				return (T)PropertyConverter.Convert(s.DataValue, typeof(T));
			else
				return default(T);
		}

		static public T GetConfigT<T>(String name, T defaultValue)
		{
			Setting s = Get(name, null, true);
			if (s != null && !String.IsNullOrEmpty(s.DataValue))
			{
				return (T)PropertyConverter.Convert(s.DataValue, typeof(T));
			}
			else
			{
				return defaultValue;
			}
		}
		
		static public T GetValueT<T>(String name, T defaultValue)
		{
			
			Setting s = Get(name);
			if (s != null && !String.IsNullOrEmpty(s.DataValue))
			{
				return (T)PropertyConverter.Convert(s.DataValue, typeof(T));
			}
			else
			{
				return defaultValue;
			}
		}

		public Setting()
		{
		}
		
		static Dictionary<string,string> settings = new Dictionary<string, string>();
		
		static Setting()
		{
			Setup ();
		}
		
		static bool setup = false;
		
		public static void Setup ()
		{
			if (!setup)
			{
				//let's load all the app config settings, if possible.	
				foreach( string k in ConfigurationManager.AppSettings.Keys )
				{
					settings[k] = ConfigurationManager.AppSettings[k];
					log.Debug("config key: ", k, settings[k] );	
				}
				setup = true;
			}
		}
		
		public static void AddApplicationSetting(string key, string value)
		{
			settings[key] = value;	
		}
		
		public static Setting Get(string key)
		{
			return Get(key, null, false);
		}
		
		public static Setting GetConfig (string key)
		{
			return Get (key, null, true);
		}

		
		static Dictionary<string,Setting> settingsCache = new Dictionary<string, Setting>();

		public static Setting Get(string key, string defaultValue, bool configOnly )
		{
			if( settingsCache.ContainsKey(key) )
				return settingsCache[key];

			Setting s = null;
			try
			{
				string v = settings.ContainsKey( key ) ? settings[key] : null;
					
				if( v != null )
				{
					s = new Setting();
					s.dataKey = key;
					s.DataValue = v;
				}
				else if ( ! configOnly && ! DataProvider.DisableDataProvider)
				{
					s = Setting.Load<Setting>("DataKey", key);
				}
			}
			catch (Exception e)
			{
				log.Error("Error getting setting",e);
			}
			
			if (s == null)
			{
				string v = System.Environment.GetEnvironmentVariable(key);
				if ( v != null )
				{
					s = new Setting();
					s.dataKey = key;
					s.DataValue = v;
				}
			}
						
			if (s == null && defaultValue != null)
			{
				s = new Setting();
				s.dataKey = key;
				s.dataValue = defaultValue;
			}
			settingsCache[key] = s;
			return s;
		}
		
		//// <value>
		/// Use VirtualRoot when you need to discover the subfolder the application is running in a webserver.
		/// Manually configured via the 'virtualRoot' appSetting.
		/// VirtualRoot is a static config file accessor because it is required by the system at a point 
		/// prior to DataProvider functionality.
		/// </value>
		public static string VirtualRoot
		{
			get
			{
				return ConfigurationManager.AppSettings["virtualRoot"] ?? "";
			}
		}
		
		//// <value>
		/// DefaultContext instructs EmergeTk to load a context of this type in the event a context type is 
		/// not recognized, such as when the root directory is accessed in the browser.
		/// DefaultContext is a static config file accessor because it is required by the system at a point 
		/// prior to DataProvider functionality.
		/// </value>
		public static string DefaultContext
		{
			get
			{
				return ConfigurationManager.AppSettings["defaultContext"];
			}
		}
	}
}
