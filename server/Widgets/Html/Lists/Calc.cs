// Calc.cs
//	
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Widgets.Html
{
	public class Calc : Label
	{
		private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Calc));
		
		string column;
		
		string function;
		
		public string Column {
			get {
				return column;
			}
			set {
				column = value;
			}
		}

		public string Function {
			get {
				return function;
			}
			set {
				function = value;
			}
		}
		
		public Calc()
		{
			log.Debug( "constructed a calc" ); 
			this.TagName = "span";
		}
		
		public override void Initialize ()
		{
			object result = Aggregate( this, function, column );
			if( result != null )
				this.Text = result.ToString();
		}
		
		private static MethodInfo[] enumerableMethods;
		
		public static object Calculate<U>(IRecordList items, string function, string column )
		{
			U[] vector = items.GetVector<U>(column);			
			
			MethodInfo mi = typeof(Enumerable).GetMethod(function,new Type[]{ typeof(IEnumerable<U>) });
			if( mi != null )
			{
				return mi.Invoke( null, new object[] { vector } );
			}
			else
			{
				if( enumerableMethods == null )
					enumerableMethods = typeof(Enumerable).GetMethods();
				foreach( MethodInfo mi2 in enumerableMethods )
				{
					if(mi2.Name == function && mi2.IsGenericMethod && mi2.GetParameters().Length == 1 )
					{
						MethodInfo mi3 = mi2.MakeGenericMethod(typeof(U));
						object o = mi3.Invoke( null, new object[] { vector } );											
						return o;
					}		
				}
			}
			log.Error("Method not found", function, column, typeof(U), items );
			throw new Exception("Method not found " + function);
		}
		
		public static object CalculateU(IRecordList items, string function, string column )
		{
			if( items == null || items.Count == 0 )
			{
				if ( function == "Count" )
					return 0;
				return null;
			}

			if( function == "Join" )
			{
				return Util.Join(items.ToStringArray(column),", ");
			}
			
			object o = items[0][column];
			
			//1. need to get a typed column vector
			//2. need to invoke a legal override of the aggregate function
			MethodInfo mi = typeof(Calc).GetMethod("Calculate",BindingFlags.Public|BindingFlags.Static);
			
			//log.Error( "looking for method", o, mi ); 
			
			
			mi = mi.MakeGenericMethod(o.GetType());
			
			object result = mi.Invoke(null, new object[]{ items, function, column });
			return result;
		}
		
		public static object Aggregate(Widget referenceWidget, string function, string column )
		{
			IDataSourced ids = (IDataSourced)referenceWidget.FindAncestor(typeof(IDataSourced));
			IRecordList items = ids.DataSource;
			
			//TODO: listen for changes to underlying records.
						
			if( ids is IGroupable )
			{
				IGroupable ig = (IGroupable)ids;
				int level = ig.CurrentGroupLevel;
				if( level != -1 )
				{
					items = items.Copy();
					for( int i = 0; i <= level; i++ )
					{
						string f = ig.Groups[i].Field;
						items.Filters.Add( new FilterInfo( f , ig.GroupDeterminant[f] ) );
					}
					items.Filter();				
				}
			}
			
			//get the first item to get it's type.
			if( items == null || items.Count == 0 )
				return null;
				
			object o = items[0][column];
			
			//1. need to get a typed column vector
			//2. need to invoke a legal override of the aggregate function
			MethodInfo mi = typeof(Calc).GetMethod("Calculate",BindingFlags.Public|BindingFlags.Static);
			
			
			mi = mi.MakeGenericMethod(o.GetType());
			
			
			
			object result = mi.Invoke(null, new object[]{ items, function, column });
			
			return result;
			
		}
	}
}
