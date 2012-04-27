using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EmergeTk.Model;
using System.Globalization;
using SimpleJson;
using System.Linq;

namespace EmergeTk
{
    public enum JSONType
    {
        Array,
        Object
    }

	public class EmergeTkJsonSerializerStrategy : PocoJsonSerializerStrategy
	{
	    public override object DeserializeObject(object value, Type type)
	    {
			if (type.Name.StartsWith ("HashSet"))
			{
				return TypeLoader.InvokeGenericMethod (typeof(EmergeTkJsonSerializerStrategy), "DeserializeHashSet", type.GetGenericArguments (), 
				                                      this, new object[] {value});
			}
	       	
	        return base.DeserializeObject(value, type);
	    }
		
		public HashSet<T> DeserializeHashSet<T> (JsonArray value)
		{
			HashSet<T> s = new HashSet<T> ();
			foreach (T v in value)
			{
				s.Add (v);
			}
			return s;
		}
	}
	
    public class JSON
    {
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(JSON));
		static JSON def = new JSON();
		static EmergeTkJsonSerializerStrategy strategy = new EmergeTkJsonSerializerStrategy ();
		
		static public JSON Default {
			get {
				return def;
			}
		}
				
		static public string Serialize (object o)
		{
			return SimpleJson.SimpleJson.SerializeObject (o);
			//return o != null ? JsonSerializer.SerializeToString (o) : "null";
		}
		
		static public T Deserialize<T> (string source)
		{
			return SimpleJson.SimpleJson.DeserializeObject<T> (source, strategy);
			//return JsonSerializer.DeserializeFromString<T> (source);
		}
		
		static public object DeserializeObject (Type t, string source)
		{
			return SimpleJson.SimpleJson.DeserializeObject (source, t, strategy);
			//return JsonSerializer.DeserializeFromString (source, t);
		}
		
		
		static public object DeserializeObject (string source)
		{
			return SimpleJson.SimpleJson.DeserializeObject (source);
			//return JsonSerializer.DeserializeFromString (source, t);
		}
        
        public string ArrayToJSON<T>(List<T> list)
        {
			return "[" + Util.Join(list) + "]";
        }
        
        public string HashToJSON(Dictionary<string, string> source)
        {
        	return HashToJSON( source, false );
        }

        public string HashToJSON(Dictionary<string, string> source, bool strict)
        {
            if (source == null) return null;
            List<string> items = new List<string>();

            foreach (string key in source.Keys)
            {
                if (strict)
                    items.Add("\"" + key + "\":" + source[key]);
                    //items.Add(string.Format("\"{0}\":{1}", key, source[key]));
                else
                    items.Add(key + ":" + source[key]);
                    //items.Add(string.Format("{0}:{1}", key, source[key]));
            }
            return "{" + Util.Join(items) + "}";
        }
        
        

        public Dictionary<string, object> JSONToHash(string source)
        {
            source = source.Trim(new char[] { '{', '}' });
            string[] pairs = tokenize(source);
            Dictionary<string,object> ret = new Dictionary<string,object>();
            foreach( string pair in pairs )
            {
                string[] tuple = pair.Split(new char[]{':'},2);
                object r = Decode(tuple[1]);
                ret[tuple[0].Trim('"')] = r;
            }
            return ret;
        }
        
        public object[] JSONToArray( string source )
        {
        	source = source.Trim(new char[] { '[', ']' });
        	string[] vals = tokenize(source);
        	ArrayList result = new ArrayList();
        	for(int i = 0; i < vals.Length; i++ )
        		result.Add( Decode( vals[i] ) );
			return result.ToArray();
        }
		
		public Dictionary<string,object> DecodeObject(string s)
		{
			return (Dictionary<string,object>)Decode(s);
		}
        
        public object Decode( string s )
        {
        	if( s == null )
        		return null;
			s = s.Trim();
			if( s == string.Empty || s == "null" )
				return null;
        	if( s.StartsWith("[" ) )
        		return JSONToArray( s );
        	
        	else if( s.StartsWith("{") )
        	{
        		Dictionary<string, object> ret = JSONToHash( s );
        	
        		if( ret != null && ret.ContainsKey( "_type" ) )
        		{
        			Type type = TypeLoader.GetType(ret["_type"].ToString());
        			if( type == null )
        				throw new TypeLoadException("Failed to load type " + ret["_type"] );
        			if( type.GetInterface("IJSONSerializable") != null  )
        			{
        				return Decode( type, ret );
        			}
        		}
        		return ret;
        	}
        	else
			{
				//first trim any gunk
				s = s.Trim();
				//then kill the quotes
				StringBuilder unescaped = new StringBuilder();
				for (int i = 0; i < s.Length; i++)
				{
					char c = s[i];
					if (c != '\\')
					{
						unescaped.Append(c);
					}
					else if (s[i + 1] == '\\')
					{
						i++;
						unescaped.Append(c);
					}
				}
				s = unescaped.ToString();
				if( s[0] == '\"' ) 
					s = s.Substring(1,s.Length-2);
        		return s;
			}
        }
        
        public object Decode( Type t, Dictionary<string,object> ret )
        {
        	if( t.IsSubclassOf(typeof(AbstractRecord) ) && ret != null && ret.ContainsKey("_id" ) )
        	{
        		return AbstractRecord.Load(t, ret["_id"].ToString());
        	}
        	return TypeLoader.InvokeGenericMethod(typeof(JSON),"DecodeType",new Type[]{t},this,new object[]{ret});
        }
        
        public T DecodeType<T>( Dictionary<string,object> ret ) where T : IJSONSerializable, new()
        {
        	try
        	{
	        	T t = new T();
	        	//log.Debug(ret);
	        	t.IsDeserializing = true;
	        	t.Deserialize( ret );
	        	t.IsDeserializing = false;
	        	return t;
	        }
	        catch(Exception e)
	        {
	        	log.Error("error deserializing " + typeof(T) + "\n" + Util.BuildExceptionOutput(e));
	        	log.Error(ret);
	        }
	        return default(T);
        }
        
        private string[] tokenize( string s )
        {
        	//splits a string recursively into comma delim'd list
			List<string> items = new List<string>();
        	StringBuilder sb = new StringBuilder();
        	int nestLevel = 0;
        	bool inQuotes = false;
			
			//handle escaped characters
			//see http://www.ietf.org/rfc/rfc4627.txt?number=4627 section 2.5
        	for( int i = 0; i < s.Length; i++ )
        	{
        		char c = s[i];
				if( c == '\\' )
				{
					sb.Append('\\');
					switch(s[i+1])
					{
					case 'u':
						short codePoint = short.Parse(s.Substring(i+2,4),NumberStyles.HexNumber);
						sb.Append(UnicodeEncoding.Unicode.GetChars(BitConverter.GetBytes(codePoint)));
						i+=5;
						break;
					case 'r':
						sb.Append('\r');
						i++;
						break;
					case 'n':
						sb.Append('\n');
						i++;
						break;
					case '"':
						sb.Append('\"');
						i++;
						break;
					case 't':
						sb.Append('\t');
						i++;
						break;
					case '\\':
						sb.Append('\\');
						i++;
						break;
					case '/':
						sb.Append('/');
						i++;
						break;					
					}
					continue;
				}
				
				//now test for json control-flow characters
        		switch( c )
        		{
        			case ',':
        				if( ! inQuotes && nestLevel == 0 )
        				{
        					items.Add(sb.ToString().Trim());
                            sb = new StringBuilder();
        					continue;
        				}
        				break;
        			case '[':
        			case '{':
        				if( ! inQuotes )
        					nestLevel++;
        				break;
        			case ']':
        			case '}':
        				if( ! inQuotes )
        					nestLevel--;
        				break;
        			case '"':
        				inQuotes = ! inQuotes;
        				break;	        				 
        		}
				sb.Append(c);
        	}
        	if( sb.Length > 0 )
        	{
                items.Add(sb.ToString().Trim());
        	}
        	return items.ToArray();
        }
        
        public string Encode( string o )
        {
        	if( o == null )
        		return "null";
        	return Util.ToJavaScriptString(o);
        }
        
        public string Encode( DateTime o )
        {
        	return Util.ToJavaScriptString(o.ToString());
        }
        
        public string Encode( int o )
        {
        	return o.ToString();
        }
        
        public string Encode( float o )
        {
        	return o.ToString();
        }
        
        public string Encode( double o )
        {
        	return o.ToString();
        }
		
		public string Encode( decimal o )
		{
			return o.ToString();
		}
        
        public string Encode( bool o )
        {
        	return o ? "true" : "false";
        }
        
        public string Encode( IJSONSerializable o )
        {
        	return JSON.Default.Encode(o.Serialize());
        }
        
        public string Encode( IEnumerable o )
        {
        	if( o == null )
        		return "null";
        	List<string> items = new List<string>();
        	foreach( object c in o )
        		items.Add( Encode(c) );
        	return '[' + Util.Join( items ) + ']';
        }
        
        public string Encode( object o )
        {
        	if( o is bool )
        		return Encode( (bool)o );
        	else if( o is double )
        		return Encode( (double)o );
        	else if( o is float )
        		return Encode( (float)o );
			else if( o is decimal )
        		return Encode( (decimal)o );
			else if( o is int )
        		return Encode( (int)o );
        	else if( o is string )
        		return Encode( (string)o );
        	else if( o is IDictionary )
        		return Encode( (IDictionary)o );
        	else if( o is IEnumerable )
        		return Encode( o as IEnumerable );
        	else if( o is IJSONSerializable )
        		return Encode( o as IJSONSerializable ); 
        	if( o == null )
        		return "null";
        	return Util.ToJavaScriptString(o.ToString());
        }
        
        
        public string Encode( IDictionary o )
        {
        	if( o == null )
        		return "null";
        	List<string> items = new List<string>();
        	foreach( object k in o.Keys )
        	{
        		items.Add( string.Format("\"{0}\":{1}", k, Encode(o[k]) ) );
        	}
        	return '{' + Util.Join( items ) + '}';
        }

        static public string PrepareValueForJSON(object value)
        {
            if (value == null) return null;
            if (value is string ||
                value is DateTime ||
                value is Enum ||
                value is AbstractRecord)
            {
                if (value is DateTime)
                    value = ((DateTime)value).ToShortDateString();
                return "'" + Util.FormatForClient(value.ToString()) + "'";
            }
            else
                return value.ToString();
        }
    }
}
