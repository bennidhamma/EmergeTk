using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace EmergeTk.Model
{
    public class ColumnInfoManager
    {
        static private Dictionary<Type, ColumnInfo[]> columnInfos = new Dictionary<Type, ColumnInfo[]>();

        static public void RegisterColumns(Type t, ColumnInfo[] fields)
        {
            columnInfos[t] = fields;
        }

		static public ColumnInfo[] RequestColumns( IRecordList dataSource, Type t )
		{
			if( dataSource != null && dataSource.Count > 0 )
			{
				//we can't create types for AdoRecordLists, because there can
				//be an open-ended number of different datasets.  Every new
				//dataset would need a type and that is going to drive the
				//runtime crazy.
				return dataSource[0].Fields;
			}
			else
			{
				return RequestColumns(t);
			}
		}

        static public ColumnInfo[] RequestColumns(Type t)
        {
            if (columnInfos.ContainsKey(t))
                return columnInfos[t];
            else
				return (ColumnInfo[])TypeLoader.InvokeGenericMethod(typeof(ColumnInfoManager),"RequestColumns",new Type[]{t},null, Type.EmptyTypes, new object[]{});
        }

        static public ColumnInfo[] RequestColumns<T>() where T : AbstractRecord, new()
        {
            if (columnInfos.ContainsKey(typeof(T)))
                return columnInfos[typeof(T)];
            //have to instantiate a mock record so we can get its most derived goodies.  
            //Otherwise we will invoke the most derived, defined static Fields property :(
            T t = new T();
            columnInfos[typeof(T)] = t.Fields;  ///have to be careful about recursion here.  
                                               ///we want to avoid peformance penalties, so let's acces FieldInfos directly.
            return t.Fields;
        }
        
        static public ColumnInfo RequestColumn<T>(string column) where T : AbstractRecord, new()
        {
        	foreach( ColumnInfo ci in RequestColumns<T>() )
        		if( ci.Name == column )
        			return ci;
        	return null;
        }

        static public bool TypeIsRegistered(Type t)
        {
            return columnInfos.ContainsKey(t);
        }
		
		static public ColumnInfo[] GetRelationTableColumns() {
			return new ColumnInfo[] { 
				new ColumnInfo( "Parent_Id", typeof(int), DataType.None, null, false ),
				new ColumnInfo( "Child_Id", typeof(int), DataType.None, null, false ) };
		}
    }
}
