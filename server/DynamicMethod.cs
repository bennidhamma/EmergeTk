#region Copyright © 2007 FocusPoint Solutions
// This File is part of the neXus.CORE Project
//
// Copyright © 2007 FocusPoint Solutions
// All rights reserved

// This library is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this library; if not, write to the
// Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor Boston, MA  02110-1301 USA
#endregion

#region Revision history
// 2007.05.21 - Pedro Gomes (pedro.gomes@focuspoint-solutions.com)
//      Initial version.
// 2007.06.13 - Pedro Gomes (pedro.gomes@focuspoint-solutions.com)
//      Fixed major bug. Setters are now casting the input value.
// 2007.12.10 - Pedro Gomes (pedro.gomes@focuspoint-solutions.com)
//      Now supports generic methods invocation.
#endregion

#region Include directives
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
#endregion

namespace EmergeTk
{

	public delegate object GenericInvoker (object target, params object[] arguments);
	public delegate void GenericSetter(object target, object value);
	public delegate object GenericGetter(object target);

	public static class DynamicMethods
	{
		static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger (typeof(DynamicMethods));
		#region Constants
		#endregion

		#region Fields
		#endregion

		#region Constructors
		#endregion

		#region Properties
		#endregion

		#region Operators
		#endregion

		#region Events
		#endregion

		#region Methods
		private static void FindMethod (Type type, string methodName, Type[] typeArguments, Type[] parameterTypes, out MethodInfo methodInfo, out ParameterInfo[] parameters)
		{
			//log.DebugFormat("Finding method {0} on type {1}", methodName, type );
			methodInfo = null;
			parameters = null;

			if (null == parameterTypes) {
				methodInfo = type.GetMethod (methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				if (null == methodInfo)
					methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
				methodInfo = methodInfo.MakeGenericMethod (typeArguments);
				parameters = methodInfo.GetParameters ();
			} else {
				// Method is probably overloaded. As far as i know there's no other way to get the MethodInfo instance, we have to
				// search for it in all the type methods
				//methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic,
				//		               null, CallingConventions.Any, parameterTypes, null );

				if (methodInfo == null) {
					MethodInfo[] methods = type.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
					foreach (MethodInfo method in methods) {
						
						if (method.Name == methodName && method.IsGenericMethodDefinition ) {
							// create the generic method
							
							MethodInfo genericMethod = method.MakeGenericMethod (typeArguments);
							parameters = genericMethod.GetParameters ();
							List<Type> ptypelist = new List<Type>();
							foreach( ParameterInfo pi in parameters )
								ptypelist.Add(pi.ParameterType);
							//log.DebugFormat("Method Name: {0} IsGenericMethodDefinition: {1} params: {2} input params: {3}, seq eq? {4}", method.Name, method.IsGenericMethodDefinition,
							//               parameters.Join(", "), parameterTypes.Join(","), parameterTypes.SequenceEqual(ptypelist)  );
							// compare the method parameters
							if( parameterTypes.SequenceEqual(ptypelist) )
							{
								methodInfo = genericMethod;
								break;
							}
							/*
							if (parameters.Length == parameterTypes.Length)
							{
								
								for (int i = 0; i < parameters.Length; i++)
								{
									if (parameters[i].ParameterType != parameterTypes[i]) 
									{
										continue;
										// this is not the method we'r looking for
									}
								}

								// if we'r here, we got the rigth method
								methodInfo = genericMethod;
								break;
							}*/
						}
					}
				} else {
					MethodInfo genericMethod = methodInfo.MakeGenericMethod (typeArguments);
					parameters = genericMethod.GetParameters ();
					methodInfo = genericMethod;
				}

				if (null == methodInfo) {
					throw new InvalidOperationException ("Method not found");
				}
			}
		}

		static bool IsParams (ParameterInfo param)
		{
			return param.GetCustomAttributes (typeof(ParamArrayAttribute), false).Length > 0;
		}

		public static GenericInvoker GenericMethodInvokerMethod (Type type, string methodName, Type[] typeArguments, Type[] parameterTypes)
		{
			MethodInfo methodInfo;
			ParameterInfo[] parameters;

			// find the method to be invoked
			FindMethod (type, methodName, typeArguments, parameterTypes, out methodInfo, out parameters);
			string name = null;
			if (parameterTypes == null)
				name = string.Format ("__MethodInvoker_{0}_ON_{1}", methodInfo.Name, methodInfo.DeclaringType.Name); else
				name = string.Format ("__MethodInvoker_{0}_ON_{1}_{2}", methodInfo.Name, methodInfo.DeclaringType.Name, parameterTypes.Join ("_"));
			DynamicMethod dynamicMethod = new DynamicMethod (name, typeof(object), new Type[] {
				typeof(object), 
				typeof(object[])
			}, methodInfo.DeclaringType);

			ILGenerator generator = dynamicMethod.GetILGenerator ();

			// define local vars
			generator.DeclareLocal (typeof(object));

			if (!methodInfo.IsStatic) {
				// load first argument, the instace where the method is to be invoked
				generator.Emit (OpCodes.Ldarg_0);

				// cast to the correct type
				generator.Emit (OpCodes.Castclass, methodInfo.DeclaringType);
			}

			for (int i = 0; i < parameters.Length; i++) {
				// load paramters they are passed as an object array
				generator.Emit (OpCodes.Ldarg_1);

				// load array element
				generator.Emit (OpCodes.Ldc_I4, i);
				generator.Emit (OpCodes.Ldelem_Ref);

				// cast or unbox parameter as needed
				Type parameterType = parameters[i].ParameterType;
				if (parameterType.IsClass) {
					generator.Emit (OpCodes.Castclass, parameterType);
				} else {
					generator.Emit (OpCodes.Unbox_Any, parameterType);
				}
			}

			// call method
			if (!methodInfo.IsStatic) 
				generator.EmitCall (OpCodes.Callvirt, methodInfo, null);
			else
				generator.EmitCall (OpCodes.Call, methodInfo, null);
				
			// handle method return if needed
			if (methodInfo.ReturnType == typeof(void)) {
				// return null
				generator.Emit (OpCodes.Ldnull);
			} else {
				// box value if needed
				if (methodInfo.ReturnType.IsValueType) {
					generator.Emit (OpCodes.Box, methodInfo.ReturnType);
				}
			}

			// store to the local var
			generator.Emit (OpCodes.Stloc_0);

			// load local and return
			generator.Emit (OpCodes.Ldloc_0);
			generator.Emit (OpCodes.Ret);

			// return delegate
			return (GenericInvoker)dynamicMethod.CreateDelegate (typeof(GenericInvoker));
		}

		public static GenericInvoker GenericMethodInvokerMethod (Type type, string methodName, Type[] typeArguments)
		{
			return GenericMethodInvokerMethod (type, methodName, typeArguments, null);
		}
			
		///
		/// Creates a dynamic setter for the property
		///
		public static GenericSetter CreateSetMethod (PropertyInfo propertyInfo)
		{
			if( propertyInfo == null )
				throw new ArgumentNullException( "propertyInfo" );
			/*
		    * If there's no setter return null
		    */
			MethodInfo setMethod = propertyInfo.GetSetMethod ();
			if (setMethod == null)
				return null;

			/*
   			* Create the dynamic method
   			*/
			Type[] arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);

			DynamicMethod setter = new DynamicMethod (String.Concat ("_Set", propertyInfo.Name, "_"), typeof(void), arguments, propertyInfo.DeclaringType);
			ILGenerator generator = setter.GetILGenerator ();
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Castclass, propertyInfo.DeclaringType);
			generator.Emit (OpCodes.Ldarg_1);

			if (propertyInfo.PropertyType.IsClass)
				generator.Emit (OpCodes.Castclass, propertyInfo.PropertyType); 
			else
				generator.Emit (OpCodes.Unbox_Any, propertyInfo.PropertyType);

			generator.EmitCall (OpCodes.Callvirt, setMethod, null);
			generator.Emit (OpCodes.Ret);

			/*
		   	* Create the delegate and return it
		   	*/
			return (GenericSetter)setter.CreateDelegate (typeof(GenericSetter));
		}

		///
		/// Creates a dynamic getter for the property
		///
		public static GenericGetter CreateGetMethod (PropertyInfo propertyInfo)
		{
			/*
  			 * If there's no getter return null
   			 */
			MethodInfo getMethod = propertyInfo.GetGetMethod ();
			if (getMethod == null)
				return null;

			/*
		   	* Create the dynamic method
		   	*/
			Type[] arguments = new Type[1];
			arguments[0] = typeof(object);

			DynamicMethod getter = new DynamicMethod (String.Concat ("_Get", propertyInfo.Name, "_"), typeof(object), arguments, propertyInfo.DeclaringType);
			ILGenerator generator = getter.GetILGenerator ();
			generator.DeclareLocal (typeof(object));
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Castclass, propertyInfo.DeclaringType);
			generator.EmitCall (OpCodes.Callvirt, getMethod, null);

			if (!propertyInfo.PropertyType.IsClass)
				generator.Emit (OpCodes.Box, propertyInfo.PropertyType);

			generator.Emit (OpCodes.Ret);

						/*
		   * Create the delegate and return it
		   */
		return (GenericGetter)getter.CreateDelegate (typeof(GenericGetter));
		}
		#endregion
	}
}
