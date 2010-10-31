using System;
using System.Collections.Generic;
using System.IO;
using EmergeTk.Model;
using ProtoSharp.Core;
using EmergeTk;

namespace ProtobufSerializer
{
	public class ProtoSerializer
	{
		public static void Serialize(AbstractRecord r, Stream outStream)
		{
			MessageWriter mw = new MessageWriter(outStream);
			int i = 1;
			//serialize the id first
			mw.WriteHeader(i, WireType.Varint);
			mw.WriteVarint(r.Id);
			if( r is IVersioned )
			{
				i = i + 1;
				mw.WriteHeader(i,WireType.Varint);
				mw.WriteVarint(r.Version);
			}
			//TODO: need to skip setting default values.  one trick could be to have a permanent 
			//instance lying around that we compare values to for equality.
			//we also need a way to skip read only / computed strings.
			foreach( ColumnInfo ci in r.Fields )
			{
				//always increment so that the tag number correspons to the field position when unpacking.
				i = i + 1;
				if( ci.IsList || ci.ReadOnly )
				{
					//print("skipping field " + ci.Name);
					continue;
				}
				
				//TODO: there's a lot of boxing going on here.  we really want to write custom builders for 
				//each type in the system.
				object val = r[ci.Name];
				if( val == null )
				{
					//print(string.Format("skipping {0} because value is null.  st: {1}", ci.Name, System.Environment.StackTrace));
					continue;
				}
				
				//print(string.Format("writing key {0} as value {1}", ci.Name, val));
				ProtocolTypeMap type = Map(ci.Type);
				mw.WriteHeader(i,WireType.Varint);
				//print("writing value: " + val);
                if (val is AbstractRecord)
                    mw.WriteVarint(((AbstractRecord)val).Id);
                else if (ci.Type.IsEnum)
                    mw.WriteVarint(((int)val));
                else if (type.Type == typeof(int))
                    mw.WriteVarint((int)val);
                else if (type.Type == typeof(long))
                    mw.WriteVarint((long)val);
                else if (type.Type == typeof(string))
                    mw.WriteString((string)val);
                else if (type.Type == typeof(decimal))
                    mw.WriteDecimal((decimal)val);
                else if (type.Type == typeof(bool))
                    mw.WriteVarint((bool)val);
                else if (type.Type == typeof(float))
                    mw.WriteFixed((float)val);
                else if (type.Type == typeof(double))
                    mw.WriteFixed((double)val);
                else if (type.Type == typeof(DateTime))
                    mw.WriteDateTime((DateTime)val);
				else if(ci.DataType == DataType.Json)
					mw.WriteString (JSON.Serializer.Serialize (val));	
			}
		}
		
		public static AbstractRecord Deserialize(Type t, Stream inStream)
		{
			AbstractRecord r = (AbstractRecord)Activator.CreateInstance(t);
			return Deserialize(r,inStream);
		}
		
		public static T Deserialize<T>(Stream inStream) where T : AbstractRecord, new()
		{
			T t = new T();
			t.SetLoadState(true);		
			Deserialize(t,inStream);
			t.SetLoadState(false);
			return t;
		}
		
		private static AbstractRecord Deserialize(AbstractRecord t, Stream inStream)
		{
			int offset = 2;
			MessageReader mr = new MessageReader(inStream);
			//first read the id
			MessageTag tag = mr.ReadMessageTag();
			t.SetId(mr.ReadInt32());
			if( t is IVersioned )
			{
				offset = 3;
				MessageTag versionTag = mr.ReadMessageTag();
				t.SetVersion( mr.ReadInt32() );
			}
			//print("id: " + t.Id);
			//print("tag number: " + tag.Number);
			ColumnInfo[] fields = t.Fields;
			while (mr.TryReadMessageTag(ref tag))
			{
				//offset index by 2 to get correct position - 1 for one-based indexing, 1 for id.
				int index = tag.Number - offset;
				//print("reading index of " + index );
				//print("field length: " + fields.Length);
				ColumnInfo ci = fields[index];
				//print(string.Format("Reading field {0} of type {1} with tag {2} ", ci.Name, ci.Type, tag.Number) );
				if( ci.IsRecord )
				{
					int id = mr.ReadInt32();
					//print("read id " + id);
					if( id == 0 )
						continue;
					
					if( !t.LazyLoad )
						t[ci.Name] = AbstractRecord.Load(ci.Type,id);
					else
						t.SetOriginalValue(ci.Name, id);
					continue;
				}
				else if( ci.Type == typeof(int) || ci.Type == typeof(int?) || ci.Type.IsEnum)
					t[ci.Name] = mr.ReadInt32();
				else if( ci.Type == typeof(long) || ci.Type == typeof(long?))
					t[ci.Name] = mr.ReadInt64();
				else if( ci.Type == typeof(string))
					t[ci.Name] = mr.ReadString();
				else if( ci.Type == typeof(decimal) || ci.Type == typeof(decimal?))
					t[ci.Name] = mr.ReadDecimal();
				else if( ci.Type == typeof(bool) || ci.Type == typeof(bool?))
					t[ci.Name] = mr.ReadBoolean();
				else if( ci.Type == typeof(float) || ci.Type == typeof(float?))
					t[ci.Name] = mr.ReadFixedSingle();
				else if( ci.Type == typeof(double) || ci.Type == typeof(double?))
					t[ci.Name] = mr.ReadFixedDouble();
				else if( ci.Type == typeof(DateTime) || ci.Type == typeof(DateTime?))
					t[ci.Name] = mr.ReadDateTime();
				else if (ci.DataType == DataType.Json)
					t[ci.Name] = JSON.DeserializeObject (ci.Type, mr.ReadString ());
				//print("read value: "  + t[ci.Name]);
				t.SetOriginalValue(ci.Name,t[ci.Name]);
			}
			return t;
		}
		
		private static void print(object o)
		{
			Console.WriteLine(o);	
		}
		
		public static ProtocolTypeMap Map(Type inputType)
		{
			if( inputType.IsEnum || inputType.IsSubclassOf(typeof(AbstractRecord)) )
			{
				return new ProtocolTypeMap("int32",typeof(void),WireType.Varint);
			}
            else if (typeMap.ContainsKey(inputType))
                return typeMap[inputType];
			return new ProtocolTypeMap("string",typeof(void),WireType.String);
		}
		
		static ProtocolTypeMap[] map;
		static Dictionary<string,ProtocolTypeMap> stringMap;
		static Dictionary<Type,ProtocolTypeMap> typeMap;
		static ProtoSerializer()
		{
			map = new ProtocolTypeMap[] {
				new ProtocolTypeMap("int32", typeof(int), WireType.Varint),
				new ProtocolTypeMap("int64", typeof(long), WireType.Varint),
				new ProtocolTypeMap("string", typeof(string), WireType.String),
				new ProtocolTypeMap("double", typeof(double), WireType.Unknown),
				new ProtocolTypeMap("float", typeof(float), WireType.Unknown),
				new ProtocolTypeMap("bool", typeof(bool), WireType.Unknown),
				new ProtocolTypeMap("bytes", typeof(byte[]), WireType.Unknown),
				new ProtocolTypeMap("int64", typeof(decimal), WireType.Varint),
				new ProtocolTypeMap("int64", typeof(DateTime), WireType.Fixed64)
			};
			
			stringMap = new Dictionary<string, ProtocolTypeMap>() {
				{"int32", map[0]},
				{"int64", map[1]},
				{"string",map[2]},
				{"double",map[3]},
				{"float", map[4]},
				{"bool", map[5]},
				{"bytes",map[6]}
				//{"decimal",map[7]} -- NO MAPPING HERE.
			};
			
			typeMap = new Dictionary<Type, ProtocolTypeMap>() {
				{typeof(int),map[0]},
                {typeof(int?), map[0]},
				{typeof(long),map[1]},
                {typeof(long?),map[1]},
				{typeof(string),map[2]},
				{typeof(double),map[3]},
                {typeof(double?),map[3]},
				{typeof(float),map[4]},
                {typeof(float?),map[4]},
				{typeof(bool),map[5]},
                {typeof(bool?),map[5]},
				{typeof(byte[]),map[6]},
				{typeof(decimal),map[7]},
                {typeof(decimal?),map[7]},
				{typeof(DateTime),map[8]},
                {typeof(DateTime?),map[8]}
			};
		}
	}
				
	public struct ProtocolTypeMap
	{
		public string ProtoType;
		public Type Type;
		public WireType WireType;
				
		public ProtocolTypeMap(string protoType, Type type, WireType wireType)
		{
			this.ProtoType = protoType;
			this.Type = type;
			this.WireType = wireType;
		}
	}
}
