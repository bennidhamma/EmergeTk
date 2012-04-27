using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using SimpleJson;

namespace EmergeTk.WebServices
{
	public delegate void MessageEndPoint (MessageEndPointArguments arguments);

	public class WebServiceManager
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(WebServiceManager));
		public static WebServiceManager Manager = new WebServiceManager ();
		bool disableServiceSecurity;

		private WebServiceManager ()
		{
			disableServiceSecurity = Setting.GetValueT<bool>("DisableServiceSecurity",false);
		}

		//a map of base paths for routing requests.
		Dictionary<string, RequestProcessor> routeMap = new Dictionary<string, RequestProcessor> ();

		//a map of the service objects handler objects themselves
		Dictionary<string, object> serviceMap = new Dictionary<string, object> ();
		
		Dictionary<Type,IRestServiceManager> restServiceManagers = new Dictionary<Type, IRestServiceManager>();
		Dictionary<Type,RestTypeDescription> restTypeDescriptions = new Dictionary<Type, RestTypeDescription>();
		Dictionary<string,Type> restNameMap = new Dictionary<string, Type> ();

		public Type GetTypeForRestService (string name)
		{
			if (restNameMap.ContainsKey (name))
				return restNameMap[name];
			return null;
		}

        public static bool DoAuth()
        {
            return !Manager.disableServiceSecurity && System.Web.HttpContext.Current != null && !User.IsRoot;
        }

		
		public IRestServiceManager GetRestServiceManager(Type recordType)
		{
			if( restServiceManagers.ContainsKey(recordType) )
			{
				return restServiceManagers[recordType];
			}
			return null;
		}
		
		public RestTypeDescription GetRestTypeDescription(Type recordType)
		{
			if( restTypeDescriptions.ContainsKey(recordType) )
			{
				return restTypeDescriptions[recordType];
			}
			return new RestTypeDescription();
		}

		public Dictionary<string, RequestProcessor> RouteMap {
			get {
				return routeMap;
			}
		}
		
		static bool started = false;
		public void Startup ()
		{
			if( started ) return;
			started = true;
			Attribute[] attributes;
			Type[] types = TypeLoader.GetTypesWithAttribute (typeof(WebServiceAttribute), true, out attributes);

			/*
			 * for each type that matched above, we want to create a couple of objects:
			 * 1. an instance of the web service type itself.
			 * 2. a request processor.
			 * 3. if the service manager type is different than the current type, a new instance of that service manager type.
			 */

			//first scan for all non-generic services.
			for (int i = 0; i < types.Length; i++) 
			{
				try
				{
					Type type = types[i];
					log.DebugFormat("initializing {0} generic? {1} attribute: {2}", type, type.IsGenericParameter, attributes[i]);
					//we don't automagically process generic typedefs.
					if (type.IsGenericTypeDefinition)
						continue;
					WebServiceAttribute att = (WebServiceAttribute)attributes[i];
					object service = Activator.CreateInstance (type);
					IMessageServiceManager serviceManager = (IMessageServiceManager)Activator.CreateInstance(att.ServiceManager);
					RegisterWebService (type, service, att, serviceManager);
				}
				catch(Exception e)
				{
					log.Error(e);	
				}
			}

			//next scan for all restful types and register an instance of our ModelServiceHandler for them.
			Attribute[] restAttributes;
			Type[] restTypes = TypeLoader.GetTypesWithAttribute (typeof(RestServiceAttribute), false, out restAttributes);

			for (int i = 0; i < restTypes.Length; i++) 
			{
				try
				{
					RestServiceAttribute attribute = (RestServiceAttribute)restAttributes[i];
					IRestServiceManager restServiceManager = (IRestServiceManager)Activator.CreateInstance(attribute.ServiceManager);
					List<RestTypeDescription> mangeableTypes = restServiceManager.GetTypeDescriptions();
					if( mangeableTypes != null )
					{
						foreach( RestTypeDescription description in mangeableTypes )
						{
							TypeLoader.InvokeGenericMethod (typeof(WebServiceManager), "RegisterRestService", new Type[] { description.RestType }, this, new object[]{restServiceManager, description});
						}
					}
					else
					{
						RestTypeDescription description = new RestTypeDescription()
						{
							ModelName = attribute.ModelName,
							ModelPluralName = attribute.ModelPluralName,
							RestType = restTypes[i],
							Verb = attribute.Verb
						};
						
						TypeLoader.InvokeGenericMethod (typeof(WebServiceManager), "RegisterRestService", new Type[] { restTypes[i] }, this, new object[]{restServiceManager, description});
					}
				}
				catch(Exception e)
				{
					log.Error(e);	
				}
			}
			
			//load security types
			LoadSecurityServiceManagers();
		}

		void LoadSecurityServiceManagers ()
		{
			RegisterSecurityRestService<User>("user");
			RegisterSecurityRestService<Role>("role");
			RegisterSecurityRestService<Permission>("permission");
		}

		private void RegisterSecurityRestService<T>(string modelName) where T : AbstractRecord, new()
		{
			AdministrativeServiceManager man = new AdministrativeServiceManager();
			RestServiceAttribute attribute = new RestServiceAttribute()
			{
				ModelName = modelName
			};
			RestTypeDescription description = new RestTypeDescription()
			{
				ModelName = attribute.ModelName,
				ModelPluralName = attribute.ModelPluralName,
				RestType = typeof(T),
				Verb = attribute.Verb
			};
			WebServiceAttribute modelServiceHandlerAttribute = (WebServiceAttribute)typeof(ModelServiceHandler<T>).GetCustomAttributes (typeof(WebServiceAttribute), false)[0];
			ModelServiceHandler<T> service = new ModelServiceHandler<T> ();
			service.RestDescription = description;
			modelServiceHandlerAttribute.BasePath += description.ModelName + '/';
			service.ServiceManager = man;
			restServiceManagers[typeof(T)] = man;
			restTypeDescriptions[typeof(T)] = description;
			RegisterWebService (typeof(ModelServiceHandler<T>), service, modelServiceHandlerAttribute, service, description.ModelName, description.ModelPluralName);
		}

		#pragma warning disable 169
		private void RegisterRestService<T> (IRestServiceManager restServiceManager, RestTypeDescription description) where T : AbstractRecord, new()
		{
			log.Info("Registering rest service ", description.ToString() );
			WebServiceAttribute modelServiceHandlerAttribute = (WebServiceAttribute)typeof(ModelServiceHandler<T>).GetCustomAttributes (typeof(WebServiceAttribute), false)[0];
			ModelServiceHandler<T> service = new ModelServiceHandler<T> ();
			service.RestDescription = description;
			modelServiceHandlerAttribute.BasePath += description.ModelName + '/';
			service.ServiceManager = restServiceManager;
			restServiceManagers[typeof(T)] = restServiceManager;
			restTypeDescriptions[typeof(T)] = description;
			restNameMap[description.ModelName] = typeof(T);
			RegisterWebService (typeof(ModelServiceHandler<T>), service, modelServiceHandlerAttribute, service, description.ModelName, description.ModelPluralName);
		}
		#pragma warning restore 169
		
		public void RegisterWebService (Type type, object service, WebServiceAttribute att, IMessageServiceManager serviceManager)
		{
			RegisterWebService(type,service,att,serviceManager,null,null);
		}
			
		private void RegisterWebService (Type type, object service, WebServiceAttribute att, IMessageServiceManager serviceManager, string modelName, string pluralName)
		{
			RequestProcessor processor = new RequestProcessor (att);
			processor.ServiceManager = serviceManager;
			MethodInfo[] methods = type.GetMethods ();

			foreach (MethodInfo method in methods) {
				object[] methodAttributes = method.GetCustomAttributes (typeof(MessageServiceEndPointAttribute), true);
				if (methodAttributes != null && methodAttributes.Length > 0) {
					MessageServiceEndPointAttribute messageAttribute = (MessageServiceEndPointAttribute)methodAttributes[0];
					messageAttribute.Regex = new Regex (messageAttribute.Pattern);
                    messageAttribute.EndPoint = (MessageEndPoint)Delegate.CreateDelegate(typeof(MessageEndPoint), service, method.Name);
					messageAttribute.MethodName = method.Name;
					object[] descAtts = method.GetCustomAttributes (typeof(MessageDescriptionAttribute), true);
					if( descAtts != null && descAtts.Length > 0 )
					{
						MessageDescriptionAttribute descAttr = (MessageDescriptionAttribute)descAtts[0];
						messageAttribute.Description = descAttr.Description;
						if( modelName != null )
						{
							messageAttribute.Description = messageAttribute.Description.Replace("{ModelName}",modelName).Replace("{ModelPluralName}",pluralName);
						}
					}
					processor.AddMessageEndPoint (messageAttribute);
					//log.InfoFormat("creating endPoint '{0}' on service '{1}'", method.Name, service);
				}
			}
			log.InfoFormat("Registering web service '{0}' at '{1}'", service, att.BasePath);
			routeMap[att.BasePath] = processor;
			serviceMap[att.BasePath] = service;
		}
		
		public RequestProcessor GetRequestProcessor (string path)
		{
			string[] segs = path.TrimEnd('/').Split('/');
			for( int i = 0; i < segs.Length; i++ )
				segs[i] += '/';
			return GetRequestProcessor(segs);
		}
		
		public RequestProcessor GetRequestProcessor (string[] segs)
		{
			string basePath = string.Empty;
			//reverse search to allow different handlers to implement sub-namespaces.
			//for (int i = 0; i < segs.Length; i++) {
			for( int i = segs.Length; i >= 0; i-- )
			{
				basePath = string.Join("", segs, 0, i );
				//log.Debug("looking for router for base path ", basePath);
				if (routeMap.ContainsKey (basePath)) {
					return routeMap[basePath];
				}
			}
			return null;
		}
	}
			
	public class AdministrativeServiceManager : IRestServiceManager
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(AdministrativeServiceManager));
		
		#region IRestServiceManager implementation
		public string GetHelpText ()
		{
			throw new System.NotImplementedException();
		}
		
		
		public void Authorize (RestOperation operation, JsonObject recordNode, AbstractRecord record)
		{
			if ( operation != RestOperation.Get )
			{
				if (User.Current != null && record != null && record.GetType() == typeof(User) && record == User.Current)
					return;
				
				log.Debug("Going to throw UnauthorizedAccessException: Record is only allowed operation of type 'Get'");
				throw new UnauthorizedAccessException("Record is only allowed operation of type 'Get' for non-Root user.");
			}
			else {
				log.Debug("Authorize ok for non-Get");
			}
			
			if ( User.Current == null )
			{
				log.Debug("Going to throw UnauthorizedAccessException: No current user");
				throw new UnauthorizedAccessException("No current user.");
			} 
			else if ( record != null && record.GetType() == typeof(User) && record != User.Current )				
			{
				log.Debug("Going to throw UnauthorizedAccessException: Record does not match current user");
				throw new UnauthorizedAccessException("Record does not match current user.");
			}		
			else
				log.Debug("Authorize successful",operation,record);
		}
		
		
		public bool AuthorizeField (RestOperation op, AbstractRecord record, string property)
		{
			// TODO: if operation is put, and it's not the password field, then throw exception
			return true;
		}
		
		
		public AbstractRecord GenerateExampleRecord ()
		{
			throw new System.NotImplementedException();
		}
		
		
		public string GenerateExampleFields (string method)
		{
			throw new System.NotImplementedException();
		}
		
		
		public List<RestTypeDescription> GetTypeDescriptions ()
		{
			return null;
		}
		
		#endregion
		
	}
}
