using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Web;

namespace EmergeTk
{
	public class TypeLoader
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(TypeLoader));
		
		static Assembly standardAssembly = null;
		public TypeLoader()
		{
		}

		static TypeLoader()
		{
            Assembly assembly = null;
            String assemblyPath = String.Empty;

			if( false && HttpContext.Current != null )
			{
				//loadAssemblyPath(  HttpContext.Current.Server.MapPath("/bin") );
			}
			else
			{
                log.Info("TypeLoader ctor - HttpContext is null, so going to attempt getting path from Assembly.GetEntryAssembly");
                assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    assemblyPath = new FileInfo(assembly.Location).Directory.FullName;
                    log.InfoFormat("TypeLoader ctor - GetEntryAssembly returned assemblyPath {0}, using that to load assemblies", assemblyPath);
                }

                if (String.IsNullOrEmpty(assemblyPath))
                {
                    assemblyPath = System.Environment.CurrentDirectory;
                    log.InfoFormat("TypeLoader ctor - GetEntryAssembly returned null - using System.Environment.CurrentDirectory = {0} to load assemblies", assemblyPath);
                }
				loadAssemblyPath(assemblyPath);
			}
		}

		static private void loadAssemblyPath(string path )
		{
			foreach( string s in Directory.GetFiles(path, "*.dll") )
			{
				FileInfo fi = new FileInfo(s);
				log.Debug("loading assembly ", fi.Name );
				Assembly.Load( fi.Name.Replace(".dll","") );
			}
		}
		
		static Dictionary<string, Type> typeRef = new Dictionary<string,Type>();
		public static Type GetType( string type )
		{
			if( typeRef.ContainsKey( type ) )
				return typeRef[type];
			string genericParameter = null;
			string originalTypeName = type;
			if( type.Contains( "`1" ) )
			{
				string[] parts = type.Split(new string[]{"`1[["}, 2, StringSplitOptions.None);
				if( parts.Length == 2 )
				{
					type = parts[0];
					genericParameter = parts[1].Trim(']');
				}
			}
			else if( type.Contains( "<" ) )
			{
				string[] parts = type.Split(new char[]{'<','>',',',' '}, StringSplitOptions.RemoveEmptyEntries );
				type = parts[0];
				genericParameter = parts[1];
			}

			Type t = null;
			if( ( t = Type.GetType(type) ) != null )
			{
				//we want to store a separate instance, to avoid future changes to the type (i.e if the type is made generic
				typeRef[type] = Type.GetType(type);
				return t;
			}
			else if( standardAssembly != null && ( t = standardAssembly.GetType( type, false,true ) ) != null )
			{
				//get a new copy of the type.
				typeRef[type] = standardAssembly.GetType( type, false,true );
				return t;
			}
			else
			{
				foreach( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
				{
					//System.Console.WriteLine("looking for {1} in assembly {0}", a.FullName, type);
					if( ( t = a.GetType(type,false,true) ) != null )
					{
						standardAssembly = a;
						typeRef[type] = t;
						return t;
					}
                    else if ((t = a.GetType(type + "`1", false, true)) != null)
                    {
                    	standardAssembly = a;
                    	if( genericParameter != null )
                    	{
                    		log.Debug( "creating generic type with parameter", genericParameter ); 
                    		Type gp = GetType(genericParameter);                    		
                    		typeRef[originalTypeName] = t.MakeGenericType(gp);
                    		//get a new copy of the type.
                    		t = t.MakeGenericType(gp);
                    		
                    	}
                    	else
                    	{
                    		//store copy
                    		typeRef[type] = a.GetType(type + "`1", false, true);
                    	}
                        return t;
                    }
				}
			}
			typeRef[type] = null;
			return null;
		}
		
		public static Type CreateGenericType( Type genericType, Type genericParameter )
		{
			return genericType.MakeGenericType( genericParameter );
		}
		
		//TODO: consider caching type/base types listings.
		public static Type[] GetTypesOfBaseType( Type baseType )
		{
			List<Type> types = new List<Type>();
			
			foreach( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
			{
				try
				{
					foreach( Type t in a.GetTypes() )
					{
						if( t.IsSubclassOf( baseType ) )
							types.Add( t );
					}
				}
				catch (Exception e)
				{
					log.Error ("Error loading assembly: " + a, e);
				}
				
			}
			return types.ToArray();
		}
		
		public static Type[] GetTypesOfInterface( Type iface )
		{
			if( iface == null )
				return null;
			List<Type> types = new List<Type>();
			
			foreach( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
			{
				try
				{
					foreach( Type t in a.GetTypes() )
					{
						if( t.GetInterface(iface.Name ) != null )
						{
							types.Add( t );
						}
					}
				}
				catch (Exception e)
				{
					log.Error ("Error loading assembly: " + a, e);
				}
			}
			return types.ToArray();
		}
		
		public static Type[] GetTypesWithAttribute( Type attribute, bool inherit, out Attribute[] attributes )
		{			
			List<Type> types = new List<Type>();
			List<Attribute> listAtts = new List<Attribute>();
			
			foreach( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
			{
				try
				{
					foreach( Type t in a.GetTypes() )
					{
						object[] atts = t.GetCustomAttributes(attribute, inherit);
						if( atts != null && atts.Length > 0 )
						{
							types.Add( t );
							listAtts.Add( (Attribute)atts[0] );
						}
					}		
				}
				catch (Exception e)
				{
					log.Error("Error loading assembly " + a, e );					
				}
			}
			attributes = listAtts.ToArray();
			return types.ToArray();
		}
		
		public static GenericInvoker MakeGenericMethod( Type baseType, string methodName, params Type[] genericTypeParams )
		{
			return MakeGenericMethod(baseType,methodName,genericTypeParams,null);
		}

		bool okayToReadInvokers = true;
		public static GenericInvoker MakeGenericMethod( Type baseType, string methodName, Type[] genericTypeParams, Type[] parameterTypes )
		{
			GenericInvokerInfo invoker = new GenericInvokerInfo(baseType,methodName,genericTypeParams,parameterTypes);
//			log.DebugFormat("looking for generic invoker type {0}, name {1}, genericTypes [{2}], paramTypes [{3}] ",
//			               baseType, methodName, genericTypeParams.Join(","), parameterTypes.Join(",") );
			try
			{
				return invokers[invoker];
			}
			catch
			{
				//log.Warn("failed to find invoker ", baseType, methodName, genericTypeParams.JoinToString(","), parameterTypes.Join(","), invoker.GetHashCode()  );
                //log.Warn("failed to find invoker ", baseType, methodName, genericTypeParams.JoinToString(","), parameterTypes.Join(","));
				invoker.Invoker = DynamicMethods.GenericMethodInvokerMethod(baseType, methodName, genericTypeParams, parameterTypes);
					invoker.IsValid = true;
				lock(invokers)
				{
					invokers[invoker] = invoker.Invoker;
				}
				return invoker.Invoker;	
			}			
		}
				
				
		public static object InvokeGenericMethod
			( Type baseType, string methodName, Type[] genericTypeParams, object baseObject, object[] arguments )
		{
			return InvokeGenericMethod(baseType, methodName, genericTypeParams, baseObject, null, arguments );	
		}
		
		public static object InvokeGenericMethod
			( Type baseType, string methodName, Type[] genericTypeParams, object baseObject, Type[] parameterTypes, object[] arguments )
		{
//			log.Debug("invoking method with arugments: ", arguments, 1 );
//			foreach( object o in arguments )
//				log.DebugFormat("Arg '{0}' type '{1}' null ? {2}", o, o != null ? o.GetType() : null, o == null );
			GenericInvoker invoker = MakeGenericMethod(baseType, methodName, genericTypeParams, parameterTypes );
			return invoker( baseObject, arguments );
		}
		
		public static Attribute GetAttribute(Type attribute, PropertyInfo prop)
        {
        	object[] atts = prop.GetCustomAttributes(attribute, true);
            
            if (atts != null && atts.Length > 0)
            {
                return (Attribute)atts[0];
            }
            return null;
        }
		
		private static Dictionary<GenericInvokerInfo,GenericInvoker> invokers = new Dictionary<GenericInvokerInfo,GenericInvoker>();
		private static Dictionary<GenericPropertyKey,GenericPropertyInfo> properties = new Dictionary<GenericPropertyKey,GenericPropertyInfo>();
		
		public static void SetProperty( object o, string p, object v )
		{
			GetGenericPropertyInfo(o,p).Setter(o,v);
		}
		
		public static GenericPropertyInfo GetGenericPropertyInfo(object o, string p)
		{			
			GenericPropertyKey key = new GenericPropertyKey(o.GetType(),p);
			try
			{
				return properties[key];
			}
			catch
			{
				//log.Debug("missed");
				GenericPropertyInfo gpi;
				PropertyInfo pi = null;
				try
	            {
	                pi = key.Type.GetProperty(p);
	            }
	            catch (System.Reflection.AmbiguousMatchException e)
	            {
	                pi = key.Type.GetProperty(p, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
	                log.Warn("Ambiguous match. ", Util.BuildExceptionOutput(e));
	            }
				
				gpi = new GenericPropertyInfo();
				gpi.Type = key.Type;
				gpi.Property = p;
				if( pi != null )
				{
					gpi.Setter = DynamicMethods.CreateSetMethod(pi);
					gpi.Getter = DynamicMethods.CreateGetMethod(pi);
				}
				gpi.PropertyInfo = pi;
				lock(properties)
				{
					properties[key] = gpi;
				}
				return gpi;
			}
		}
		
		public static object GetProperty( object o, string p )
		{
			return GetGenericPropertyInfo(o,p).Getter(o);
		}
	}
	
	public struct GenericInvokerInfo
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(GenericInvokerInfo));
		
		public bool IsValid;
		public Type Type;
		public string MethodName;
		public Type[] GenericTypeParams;
		public Type[] ParameterTypes;
		public GenericInvoker Invoker;
		
		public GenericInvokerInfo(Type t, string m, Type[] gens, Type[] parms )
		{
			this.Type = t;
			this.MethodName = m;
			this.GenericTypeParams = gens;
			this.ParameterTypes = parms;
			this.IsValid = false;
			this.Invoker = null;
		}
		
		public override int GetHashCode()
		{
            if (ParameterTypes != null)
            {
                return (int)(((long)this.Type.GetHashCode() + 
                         (long)this.MethodName.GetHashCode() + 
                         (long) getTypeArrayHashCode(GenericTypeParams) + 
                         (long)getTypeArrayHashCode(ParameterTypes)) % int.MaxValue);

            }
            else
            {
                return (int)(((long)this.Type.GetHashCode() + 
                        (long) this.MethodName.GetHashCode() + 
                        (long) getTypeArrayHashCode(GenericTypeParams)) % int.MaxValue);
            }
		}
		
		private int getTypeArrayHashCode(Type[] types)
		{
			long hash = 0;
			foreach(Type t in types)
				hash = ( hash + t.GetHashCode() ) % int.MaxValue;
			return (int)hash;
		}
		
		public override bool Equals( object obj )
		{
			//log.Debug("calling equals on GenericInvokerInfo");
			GenericInvokerInfo x= this, y = (GenericInvokerInfo)obj;
			if( x.Type != y.Type )
				return false;
			if( x.MethodName != y.MethodName )
				return false;
			if( x.GenericTypeParams.Length != y.GenericTypeParams.Length )
				return false;
			for( int i = 0; i < x.GenericTypeParams.Length; i++ )
				if( x.GenericTypeParams[i] != y.GenericTypeParams[i] )
					return false;
			if( x.ParameterTypes == null && y.ParameterTypes == null )
				return true;
			if( x.ParameterTypes == null ) 
				return false;
			if( y.ParameterTypes == null )
				return false;
			if( x.ParameterTypes.Length != y.ParameterTypes.Length )
				return false;
			for( int i = 0; i < x.ParameterTypes.Length; i++ )
				if( x.ParameterTypes[i] != y.ParameterTypes[i] )
					return false;
			return true;
		}
	}
	
	public struct GenericPropertyKey
	{
		public Type Type;
		public string Property;		
		
		public GenericPropertyKey(Type t, string p)
		{
			this.Type = t;
			this.Property = p;
		}
	}
	
	public struct GenericPropertyInfo
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(GenericPropertyInfo));
		
		public Type Type;
		public string Property;
		public PropertyInfo PropertyInfo;
		public GenericSetter Setter;
		public GenericGetter Getter;
		
		public override int GetHashCode ()
		{
			return this.Type.GetHashCode() + this.Property.GetHashCode();
		}
		
		public override bool Equals (object obj)
		{
			//log.Debug("calling equals on GenericPropertyInfo");
			GenericPropertyInfo other = (GenericPropertyInfo)obj;
			return this.Type == other.Type && this.Property == other.Property;
		}
	}
}
