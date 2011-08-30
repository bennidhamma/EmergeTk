using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using System.Text;
using System.Collections;


namespace EmergeTk.WebServices
{
	public static class RecordSerializer
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(RecordSerializer));
		
		public readonly static string[] IdFieldSet = new string[] { "id" };
		public readonly static string[] WildcardFieldSet = new string[] { "*" };
		private static Regex simpleJsonRegex = new Regex("\\w+",RegexOptions.Compiled);
		private static Dictionary<string,object[]> jsonParameters = new Dictionary<string, object[]>();
		
		private static object[] GetWildcardFields(Type type)
		{
			List<object> fields = new List<object>();
			foreach( ColumnInfo ci in ColumnInfoManager.RequestColumns(type))
			{
				fields.Add( Util.PascalToCamel(ci.Name) );
			}
			return fields.ToArray();
		}
		
		private static object[] SetupFields(string fields, Type rType)
		{
			//strip out spaces
			if (fields == null) fields = "id";
			fields = fields.Replace(" ", "");
			object[] fieldArray;
			if( fields.StartsWith("[") )
			{
				if( jsonParameters.ContainsKey(fields) )
					fieldArray = jsonParameters[fields];
				else
				{
					string simpleJsonString = fields;
					if( !fields.Contains('"') )
						simpleJsonString = simpleJsonRegex.Replace(fields,"\"$0\"");
					//log.Debug(simpleJsonString);
					fieldArray = JSON.Default.JSONToArray(simpleJsonString);
					jsonParameters[fields] = fieldArray;
				}
			}
			else if( fields == "*" )
				fieldArray = GetWildcardFields(rType);
			else
				fieldArray = fields.Split(',');
			return fieldArray;
		}
		
		public static void Serialize(AbstractRecord record, string fields, IMessageWriter writer)
		{		
			if( record != null )
				Serialize(record,SetupFields(fields, record.GetType()), writer);
		}
		
		public static void Serialize(AbstractRecord record, string explicitName, string fields, IMessageWriter writer)
		{		
			if( record != null )
				Serialize(record, explicitName, SetupFields(fields, record.GetType()), writer);
		}

        public static void Serialize(AbstractRecord record, object[] fields, IMessageWriter writer)
        {
            Serialize(record, String.Empty, fields, writer);
        }
		
		public static void Serialize(AbstractRecord record, String explicitName, object[] fields, IMessageWriter writer )
		{
            if (record == null)
                return;
			Type rType = record.GetType();
			IRestServiceManager serviceManager = WebServiceManager.Manager.GetRestServiceManager(rType);
            if (serviceManager == null)
                return;
			if( WebServiceManager.DoAuth() )
				serviceManager.Authorize( RestOperation.Get, null, record );

            // the original MessageNode based code sets the name of the object here.  So, it seems counter-intuitive to me,
            // but we're going to try to simulate the same thing with an 
            // OpenProperty()/OpenObject()/CloseObject()/CloseProperty() bracketing.

            if (String.IsNullOrEmpty(explicitName))
                writer.OpenProperty(WebServiceManager.Manager.GetRestTypeDescription(rType).ModelName);
            else
                writer.OpenProperty(explicitName);

            writer.OpenObject();

            writer.WriteProperty("id", record.Id);
			if (record is IVersioned)
				writer.WriteProperty ("version", record.Version);
			
			if (record is IDerived || fields.Contains ("type"))
			{
				writer.WriteProperty ("type", record.DbSafeModelName);
			}
			
			foreach( object o in fields )
			{
				if( o is Dictionary<string,object> )
				{
					Dictionary<string,object> values = 	(Dictionary<string,object>)o;
					foreach( string key in values.Keys )
					{
						object val = record[Util.CamelToPascal(key)];
						if (val is AbstractRecord)
                        {
                            Serialize(val as AbstractRecord, key, values[key] as object[], writer);
                        }
                        else if (val is IRecordList)
                        {
                            Serialize(val as IRecordList, key, values[key] as object[], writer);
                        }
                        else if (val == null)
                        {
                            writer.WriteProperty(key, (String)null);
                        }
					}
					continue;
				}
				string f = (string)o;
				if( WebServiceManager.DoAuth() && ! serviceManager.AuthorizeField(RestOperation.Get,record,f) )
				   continue;
				string uField = Util.CamelToPascal(f);
				string lField = Util.PascalToCamel(f);
				
				if( lField.EndsWith("__count") )
				{
					string propName = uField.Substring(0,f.Length-"__count".Length);
					List<int> ids = record.LoadChildrenIds(record.GetFieldInfoFromName(propName));
                    if (ids != null && ids.Count > 0)
                        writer.WriteProperty(lField, ids.Count);
                    else
                        writer.WriteProperty(lField, (record[propName] as IRecordList).Count);
					continue;
				}
				ColumnInfo fi = record.GetFieldInfoFromName(uField);
				if( fi == null )
					continue;
				if( fi.IsList )
				{

					List<int> ids = record.LoadChildrenIds(fi);
                    if (ids != null && ids.Count > 0)
                    {
                        SerializeIntsList(ids, lField, null, null, fi.ListRecordType, writer);
                    }
                    else
                    {
                        object recordList = record[uField];
                        if (recordList == null)
                        {
                            writer.OpenProperty(lField);
                            writer.WriteScalar((string) null);
                            writer.CloseProperty();
                            continue;
                        }
                        Serialize((recordList as IRecordList).GetEnumerable(), lField, (object[])null, fi.ListRecordType, writer);
                    }
				}
                else if (fi.IsRecord)
                {
                    // we're just trying to get the ID.  If we *can* get this field
                    // without having to load from another table, then do so.
                    // Add IsPropertyLoaded to AbstractRecord. If it's not loaded, execute this code.
                    if (!record.PropertyLoaded(uField) && record.OriginalValues != null && record.OriginalValues.ContainsKey(uField) && record.OriginalValues[uField] != null )
                    {
                        StreamLineWriteProperty(fi.Type, writer, record.OriginalValues, lField, uField);
                    }
                    else
                    {
                        // OK, so we have to do an AbstractRecord.Load()
                        object rec = record[uField];
                        writer.OpenProperty(lField);
                        if (rec != null)
                            writer.WriteScalar(((AbstractRecord)rec).Id);
                        else
                            writer.WriteScalar(null);
                        writer.CloseProperty();
                    }
                }
                else
                {
                    StreamLineWriteProperty(fi.Type, writer, record, lField, uField);
                }
			}
            writer.CloseObject();
            writer.CloseProperty();
 		}

        public static void StreamLineWriteProperty(Type t, IMessageWriter writer, AbstractRecord record, String lField, String uField)
        {
            if (t == typeof(int))
            {
                writer.WriteProperty(lField, (int)record[uField]);
            }
            else if (t == typeof(String))
            {
                writer.WriteProperty(lField, (String)record[uField]);
            }
            else 
            {
                writer.OpenProperty(lField);
                if (t == typeof(bool))
                    writer.WriteScalar((bool)record[uField]);
                else if (t == typeof(double))
                    writer.WriteScalar((double)record[uField]);
                else if (t == typeof(float))
                    writer.WriteScalar((float)record[uField]);
                else if (t == typeof(decimal))
                    writer.WriteScalar((decimal)record[uField]);
                else if (t == typeof(DateTime))
                    writer.WriteScalar((DateTime)record[uField]);
                else
                    writer.WriteScalar(record[uField]);

                writer.CloseProperty();
            }
        }

        public static void StreamLineWriteProperty(Type t, IMessageWriter writer, Dictionary<String, object> originalValues, String lField, String uField)
        {
            if (t == typeof(int))
            {
                writer.WriteProperty(lField, (int)originalValues[uField]);
            }
            else if (t == typeof(String))
            {
                writer.WriteProperty(lField, (String)originalValues[uField]);
            }
            else
            {
                writer.OpenProperty(lField);
                writer.WriteScalar(originalValues[uField]);
                writer.CloseProperty();
            }
        }
		
		public static void Serialize (IRecordList list,string fields, IMessageWriter writer)
		{	
			if( list != null )
                Serialize<AbstractRecord>(list.GetEnumerable(),SetupFields(fields,list.RecordType),list.RecordType, writer);
		}
        
        public static void Serialize(IRecordList list, String listName, string fields, IMessageWriter writer)
        {
            if (list != null)
                Serialize<AbstractRecord>(list.GetEnumerable(), listName, SetupFields(fields, list.RecordType), list.RecordType, writer);
        }

        public static void Serialize(IRecordList list, String listName, object[] fields, IMessageWriter writer)
        {
            if (list != null)
                Serialize(list.GetEnumerable(), listName, fields, list.RecordType, writer);
        }
		
		public static void Serialize (IRecordList list,object[] fields, IMessageWriter writer)
		{
			if( list != null )
				Serialize<AbstractRecord>(list.GetEnumerable(),fields,list.RecordType, writer);
		}
		
		public static void Serialize<T> (IEnumerable<T> items, string fields, Type recordType, IMessageWriter writer) where T : AbstractRecord
		{
			Serialize<T>(items, SetupFields(fields,recordType),recordType, writer );
		}

		public static void Serialize<T> (IEnumerable<T> items, string fields, IMessageWriter writer) where T : AbstractRecord
		{
			Serialize<T>(items, SetupFields(fields,typeof(T)), typeof(T), writer );
		}

        public static void Serialize<T>(IEnumerable<T> items, string listName, string fields, Type recordType, IMessageWriter writer) where T : AbstractRecord
        {
            Serialize<T>(items, listName, SetupFields(fields, recordType), recordType, writer);
        }

        public static void
        Serialize<T>(
            IEnumerable<T> items,
            String listName,
            object[] fields,
            Type recordType,
            IMessageWriter writer) where T : AbstractRecord
        {
            RestTypeDescription att = WebServiceManager.Manager.GetRestTypeDescription(recordType);
            if (att.RestType == null || items == null)
			{
				log.Warn("No attribute for type " + recordType);
                return;
			}

            if (String.IsNullOrEmpty(listName))
                writer.OpenProperty(att.ModelPluralName);
            else
                writer.OpenProperty(listName);

            writer.OpenList(att.ModelName);

            if (fields == null)
            {
                foreach (AbstractRecord r in items)
                {
                    writer.WriteScalar(r.Id);
                }
            }
            else
            {
                foreach (AbstractRecord r in items)
                {
                    if (r != null)
                    {
						//log.Debug("Serializing " + r);
                        Serialize(r, fields, writer);
                    }
                }
            }
            writer.CloseList();
            writer.CloseProperty();

        }
		
		public static void Serialize<T> (IEnumerable<T> items, object[] fields, Type recordType, IMessageWriter writer) where T : AbstractRecord
		{
            Serialize<T>(items, String.Empty, fields, recordType, writer);
		}
		
		public static void SerializeIntsList (IEnumerable<int> items, string name, string fields, string sortBy, Type recordType, IMessageWriter writer)
		{
			TypeLoader.InvokeGenericMethod(typeof(RecordSerializer),"SerializeIntsListT",new Type[]{recordType},null,new object[]{items,name,fields,sortBy,recordType, writer});
		}

        public static Dictionary<int, String> fieldHashes = new Dictionary<int, String>();

        public static IRecordList<T> FastLoadScalarList<T>(IEnumerable<int> items, String sortBy, String fields) where T : AbstractRecord, new()
        {
            int hashFields = fields.GetHashCode();
            String sqlSelCols = null;

            if (!fieldHashes.ContainsKey(hashFields))
            {
                // if the fields are scalar, sqlSelCols will come out non-null
                // if one of the fields are non-scalar, sqlSelCols will be null,
                // and we store it either way.   
                AllFieldsAreScalar<T>(fields, out sqlSelCols);
                lock (fieldHashes)
                {
                    // store it, null or non-null.
                    fieldHashes[hashFields] = sqlSelCols;
                }
            }
            else  
            {
                // we've seen this hash before, return out the
                // selection column string (or NULL).
                sqlSelCols = fieldHashes[hashFields];
            }

            if (sqlSelCols == null)
                return null;

            return  DataProvider.DefaultProvider.Load<T>(
                                            String.Format("ROWID IN ({0})", Util.JoinToString<int>(items, ",")),
                                            sortBy,
                                            sqlSelCols);
        }
		
		public static void
        SerializeIntsListT<T> (
            IEnumerable<int> items, 
			string name,
            string fields, 
            string sortBy, 
            Type recordType, 
            IMessageWriter writer) where T : AbstractRecord, new()
		{
            RestTypeDescription att = WebServiceManager.Manager.GetRestTypeDescription(recordType);
            if (att.RestType == null)
            {
                log.WarnFormat("Could not find RestServiceAttribute for type {0}", recordType);
                return;
            }


			if( fields == null )
			{
                writer.OpenProperty(name ?? att.ModelPluralName);
                writer.OpenList(att.ModelName);

				foreach(int id in items )
				{
                    writer.WriteScalar(id);
				}
                writer.CloseList();
                writer.CloseProperty();
			}
			else
			{
                IRecordList<T> records = FastLoadScalarList<T>(items, sortBy, fields);
                if (records == null)          
                {
                    records = new RecordList<T>();
                    foreach (int id in items)
                    {
                        AbstractRecord r = AbstractRecord.Load<T>(id);
                        records.Add(r);
                    }
                    if (!String.IsNullOrEmpty(sortBy))
                    {
                        SortDirection sortDir = SortDirection.Ascending;
                        String[] tokens = sortBy.Split(' ');

                        if (tokens.Length > 1)
                        {
                            if (tokens[1] == "desc")
                                sortDir = SortDirection.Descending;
                        }
                        string uField = Util.CamelToPascal(tokens[0]);
                        records.Sort(new SortInfo(uField, sortDir));
                    }
                }
                Serialize(records, name, fields, recordType, writer);
            }
		}

        private static bool AllFieldsAreScalar<T>(String fields, out String fieldString) where T : AbstractRecord, new()
        {
            ColumnInfo[] cols = ColumnInfoManager.RequestColumns<T>();
            StringBuilder sb = new StringBuilder();
            fieldString = null;

            foreach (String field in fields.Split(','))
            {
                try
                {
                    if (field == "id")
                        continue;


                    ColumnInfo col = cols.FirstOrDefault(ci => ci.Name == Util.CamelToPascal(field));
                    if (col == null || col.IsList)
                        return false; // incorrect field name or there's a list.

                    sb.AppendFormat("{0},", field);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error getting columnInfo for field name = {0}, type = {1}", field, typeof (T).Name), ex);
                }
            }

            if (sb.Length > 0)
            {
                fieldString = sb.ToString().Trim(',');
                return true;
            }

            return false;
        }
		
		public static AbstractRecord DeserializeRecordType (Type t, MessageNode node, DeserializationContext context)
		{
			return (AbstractRecord) TypeLoader.InvokeGenericMethod (typeof(RecordSerializer), "DeserializeRecord", new Type[] {t},
				null, new object[] {node, context});
        }
				
		public static T DeserializeRecord<T>(MessageNode node, DeserializationContext context) where T : AbstractRecord, new()
		{
			IRestServiceManager serviceManager = WebServiceManager.Manager.GetRestServiceManager(typeof(T));
			if( serviceManager == null )
				throw new Exception("No IRestServiceManager defined.  Cannot deserialize objects of type " + typeof(T) );
			//TODO: we need to provide feedback on errors. i.e. "expecting integer for child id, but found 'name' instead."			
			T record = null;
			RestOperation op = RestOperation.Post;
			if( node.ContainsKey( "id" ) )
			{
				//log.Debug("loading with id", node["id"]);
				record = AbstractRecord.Load<T>(node["id"]);
				if( WebServiceManager.DoAuth() )
					serviceManager.Authorize(RestOperation.Put,node,record);
				op = RestOperation.Put;
				if (record is IVersioned)
				{
					if (!node.ContainsKey ("version"))
						throw new ArgumentNullException ("version");
					
					if (record.Version != Convert.ToInt32 (node["version"]))
					{
						throw new VersionOutOfDateException (string.Format ("Version mismatch - actual: {0}.  supplied: {1}", record.Version, 
							node["version"]));
					}
				}
				if( record == null )
				{
					throw new RecordNotFoundException(string.Format("Record: {0}, {1} does not exist.", typeof(T), node["id"] ) );
				}
				//now create a writable copy to avoid modification collisions / corrupting good data with a bad operation.
				record = AbstractRecord.CreateFromRecord<T>(record);
			}
			else
			{
				if( WebServiceManager.DoAuth() )
					serviceManager.Authorize(RestOperation.Post,node,null);
				record = new T();
			}
			foreach( string k in node.GetKeys() )
			{
				if( k == "id" || k == "version")
				{
					continue;
				}
				if( (WebServiceManager.DoAuth() && ! serviceManager.AuthorizeField(op,record,k) ) )
				{
					log.WarnFormat("Column {0} failed authorization.", k);
					continue;
				}
				object val = node[k];

				//log.DebugFormat("deserializing key: {0} value: {1} ", k, val );
				string recordFieldName = Util.CamelToPascal(k);
				ColumnInfo field = ColumnInfoManager.RequestColumn<T>(recordFieldName);
				if( field == null )
					throw new InvalidOperationException("Invalid field specified: " + recordFieldName);

                if( field.IsList )
				{
                    if (val != null)
                    {
                        //need to deserialize a list.
                        //TODO: we may be able to avoid generic methods here, if we just use Activator.CreateInstance					
                        MessageList list = (MessageList)val;
                        IRecordList newList = (IRecordList)TypeLoader.InvokeGenericMethod
                            (typeof(RecordSerializer), "DeserializeList", new Type[] { field.Type.GetGenericArguments()[0] }, null, new object[] { list, context });
                        IRecordList oldList = (IRecordList)record[recordFieldName];
                        if (oldList != null)
                            newList.RecordSnapshot = oldList.RecordSnapshot;
                        record[recordFieldName] = newList;
                        if (context.Lists == null)
                            context.Lists = new List<RecordPropertyList>();
                        context.Lists.Add(new RecordPropertyList(record, recordFieldName));
                    }
                    else
                    {
                        record[recordFieldName] = null;
                    }
				}
				else if( field.IsRecord )
				{
                    if (val == null)
                    {
                        record[recordFieldName] = null;
                    }
					else if( val is string )
					{
						Type loadType = field.Type;
						int id = int.Parse ((string)val);
						if (AbstractRecord.IsDerived (field.Type))
						{
							loadType = DataProvider.DefaultProvider.GetTypeForId (id);
						}
						record[recordFieldName] = AbstractRecord.Load(loadType, id);
					}
					else if( val is MessageNode )
					{
						MessageNode propNode = (MessageNode)val;
						Type loadType = field.Type;
						if (AbstractRecord.IsDerived (field.Type))
						{
							loadType = TypeLoader.GetType (propNode["type"].ToString ());
						}					
						record[recordFieldName] = TypeLoader.InvokeGenericMethod(typeof(RecordSerializer),"DeserializeRecord", new Type[] { loadType }, null, new object[]{propNode,context});
					}
				}
				else if( field.Type == typeof( Dictionary<string,string> ) )
				{
                    if (val != null)
                    {
                        MessageNode inNode = (MessageNode)node[k];
                        Dictionary<string, string> stringDict = new Dictionary<string, string>();
                        foreach (string key in inNode.GetKeys())
                            stringDict.Add(key, Convert.ToString(inNode[key]));
                        record[recordFieldName] = stringDict;
                    }
                    else
                    {
                        record[recordFieldName] = null;
                    }
				}
				else if (field.DataType == DataType.Json)
				{
					//log.Debug ("JSON serialize", val.GetType (),  val);
					if (val != null)
					{
						var deser = JSON.DeserializeObject(field.Type, (string)val);
						record[recordFieldName] = deser;
					}
					else if (val is MessageList)
					{
						MessageList ml = (MessageList)val;
						IList target = (IList)Activator.CreateInstance (field.Type);
						record[recordFieldName] = target;
						foreach (var item in ml)
						{
							target.Add (item);
						}
					}
					else
					{
						record[recordFieldName] = null;
					}

				}
				else //scalar. 
				{
                    if (val == null && field.Type.IsValueType)
                        continue;

					record[recordFieldName] = val;
				}
				// check for field authorization (after value copied into record)
				if (WebServiceManager.DoAuth())
					serviceManager.AuthorizeField(op, record, k);
			}
			if( context.Records == null )
				context.Records = new List<AbstractRecord>();
			context.Records.Add(record);
			return record;
		}
		
		private static bool DoAuth()
		{
			return System.Web.HttpContext.Current != null && ! User.IsRoot;
		}
		
		public static IRecordList DeserializeNonGenericList(Type recordType, MessageList list, DeserializationContext context)
		{
			return (IRecordList)TypeLoader.InvokeGenericMethod(typeof(RecordSerializer),"DeserializeList",new Type[]{recordType},null,new object[]{list,context});
		}
		
		public static IRecordList<T> DeserializeList<T>(MessageList list, DeserializationContext context) where T : AbstractRecord, new()
		{
			//TODO: we need to provide feedback on errors. i.e. "expecting integer for child id, but found 'name' instead."
			//log.Debug("calling deserialize list", list );
			RecordList<T> records = new RecordList<T>();
			foreach( object item in list )
			{
				int id = 0;
				if( item is string ) //scalars must be ints.
				{
					id = int.Parse((string)item);
					T c = AbstractRecord.Load<T>(id);
					if( c != null )
						records.Add(c);
				}
				else if( item is MessageNode )
				{					
					records.Add( DeserializeRecord<T>((MessageNode)item, context) );	
				}
			}
			return records;
		}
	}
	
	public struct RecordPropertyList
	{
		public AbstractRecord Record;
		public string Property;
						
		public RecordPropertyList(AbstractRecord record, string property)
		{
			this.Record = record;
			this.Property = property;
		}
	}
	
	public class DeserializationContext
	{
		public List<AbstractRecord> Records;
		public List<RecordPropertyList> Lists;
		
		public void SaveChanges()
		{
			foreach( AbstractRecord r in Records )
			{
				r.Save();	
			}
			
			foreach( RecordPropertyList rpl in Lists )
			{
				rpl.Record.SaveRelations(rpl.Property);	
			}
		}
	}
}
