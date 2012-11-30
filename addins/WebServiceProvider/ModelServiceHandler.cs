using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using EmergeTk.Model;
using EmergeTk.Model.Search;
using SimpleJson;

namespace EmergeTk.WebServices
{
	[Flags]
	public enum RestOperation
	{
		Get = 1,
		Post = 2,
		Put = 4,
		Delete = 8,
		Copy = 16
	}


	/*
	 * If this were generic, could we create an instance for each RESTful type?
	 * Then we could store in the map the endpoints, i.e. /api/model/vote directs to
	 * an instance of ModelServiceHandler<Vote>.
	 */
	
	[WebService("/api/model/")]
	public class ModelServiceHandler<T> : IMessageServiceManager where T : AbstractRecord, new()
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ModelServiceHandler<T>));
		
		public IRestServiceManager ServiceManager { get; set; }
		public RestTypeDescription RestDescription { get; set; }
		
		[MessageServiceEndPoint(@"(\d+)/(\w+)",Verb=RestOperation.Get)]
		[MessageDescription("Get a property of a {ModelName}.  Can be a scalar value, a record, or a list of records.")]
		public void GetRecordProperty(MessageEndPointArguments arguments)
		{
			T record = AbstractRecord.Load<T>(arguments.Matches[0].Groups[1].Value);
			string property = arguments.Matches[0].Groups[2].Value;
			string uProperty = Util.CamelToPascal(property);
			ColumnInfo ci = ColumnInfoManager.RequestColumn<T>(uProperty);
			if( ci == null )
				throw new Exception("The requested property is unavailable for the requested type.");
			log.DebugFormat("Got ci {0} for pro {1}", ci, uProperty);

			if( ci.IsList )
			{
				//TODO: could we sort in load children ids sql?
				List<int> ids = record.LoadChildrenIds(ci);
				IRecordList list = null;
				if(ids == null || ids.Count == 0 )
				{
					list = (IRecordList)record[ci.Name];
					if( arguments.QueryString["sortBy"] != null )
					{
						SortInfo sortInfo = GetSortInfo(arguments.QueryString["sortBy"]);
						if( sortInfo != null )
						{
							list.Sort(sortInfo);
						}
					}
					BuildReturnList("response",property, list.Count, arguments.QueryString,list, arguments.Response.Writer);
				}
                else
                {
                    BuildReturnList("response", property, ids.Count, arguments.QueryString, ids, ci.ListRecordType, arguments.Response.Writer);
                }				
			}
			else if( ci.IsRecord )
			{
				object val = record[Util.CamelToPascal(property)];
				RecordSerializer.Serialize( val as AbstractRecord, arguments.QueryString["fields"], arguments.Response.Writer );
			}
			else
			{
				object val = record[Util.CamelToPascal(property)];
                IMessageWriter writer = arguments.Response.Writer;
                writer.OpenObject();
                writer.OpenProperty(property);
                writer.WriteScalar(val);
                writer.CloseProperty();
                writer.CloseObject();
			}
            arguments.Response.Writer.Flush();
		}
		
		[MessageServiceEndPoint(@"list/(\w+)/([^/]+)",Verb=RestOperation.Get)]
		[MessageDescription("Get a list of of {ModelPluralName} where (\\w+) is equal to (.+) .")]
		public void GetRecordListByProperty(MessageEndPointArguments arguments)
		{
            IMessageWriter writer = arguments.Response.Writer;
			string property = arguments.Matches[0].Groups[1].Value;
			string val = arguments.Matches[0].Groups[2].Value;
			IRecordList<T> records = DataProvider.LoadList<T>(new FilterInfo(property,val));
			BuildReturnList
				("response",
				 this.RestDescription.ModelPluralName,
				 records.Count,
				 arguments.QueryString,
				 records,
				 arguments.Response.Writer);
			writer.Flush();
		}
		
		[MessageServiceEndPoint(@"(\w+)/([^/]+)",Verb=RestOperation.Get)]
		[MessageDescription("Get a {ModelName} or list of of {ModelPluralName} where (\\w+) is equal to (.+) .")]
		public void GetRecordByProperty(MessageEndPointArguments arguments)
		{
            IMessageWriter writer = arguments.Response.Writer;
			string property = arguments.Matches[0].Groups[1].Value;
			string val = arguments.Matches[0].Groups[2].Value;
			T record = AbstractRecord.Load<T>(property,val);
			RecordSerializer.Serialize(record, arguments.QueryString["fields"], arguments.Response.Writer );
            writer.Flush();
		}
		
		//if this is the first method defined, use this: (?<=^|\,)(\d+)(?=\,|$) instead to avoid conflicts with GetRecordProperty
		[MessageServiceEndPoint("\\d+",Verb=RestOperation.Get)]
		[MessageDescription("Get a specific {ModelName} or group of {ModelPluralName} (comma delimited)")]
		public void GetRecordList(MessageEndPointArguments arguments)
		{
            IMessageWriter writer = arguments.Response.Writer;
			if( arguments.Matches.Count == 1 )
			{
				T record = AbstractRecord.Load<T>(arguments.Matches[0].Captures[0].Value);
				RecordSerializer.Serialize(record, arguments.QueryString["fields"], writer );
			}
			else
			{
				RecordList<T> records = new RecordList<T>();
				foreach( Match m in arguments.Matches )
				{					
					records.Add( AbstractRecord.Load<T>(m.Groups[0].Value) );
				}
				SortInfo sortInfo = GetSortInfo(arguments.QueryString["sortBy"]);
				if( sortInfo != null )
				{
					records.Sort(sortInfo);
				}
				BuildReturnList("response",this.RestDescription.ModelPluralName, records.Count, arguments.QueryString,records, arguments.Response.Writer);
			}
            writer.Flush();
		}


        [MessageServiceEndPoint("query", Verb = RestOperation.Get)]
        [MessageDescription(
@"Query the data source for a list of {ModelPluralName}, using GET to specify the search criteria.  
QueryString args:
* '''predicates''' JSON array of GalleryPredicates
* '''sortBy''' a field to sort against - append desc or asc to control sort order
")]
        public void GetDataProviderSearch(MessageEndPointArguments arguments)
        {
			JsonArray predicates = null;
			if (arguments.QueryString["predicates"] != null)
				predicates = (JsonArray)JSON.DeserializeObject(arguments.QueryString["predicates"]);
			IRecordList<ModelPredicate> preds = RecordSerializer.DeserializeList<ModelPredicate>(predicates, new DeserializationContext());
            IRecordList<T> records = DataProvider.LoadList<T>(preds.Select(pred => (FilterInfo) pred).ToArray());
            SortInfo sortInfo = GetSortInfo(arguments.QueryString["sortBy"]);
            if (sortInfo != null)
                records.Sort(sortInfo);

            BuildReturnList("response", this.RestDescription.ModelPluralName, records.Count, arguments.QueryString, records, arguments.Response.Writer);
            arguments.Response.Writer.Flush();
        }


		[MessageServiceEndPoint("search", Verb = RestOperation.Get)]
		[MessageDescription(
@"Search via SearchProvider for a list of {ModelPluralName}.  
QueryString args: 
* '''q:''' the search query.
* '''start:''' the starting position to return values from (defaults to 0)
* '''count:''' the # of records to return (defaults to 10)
* '''sortBy''' a field to sort against - append desc or asc to control sort order.
* '''predicates''' JSON array of GalleryPredicates
")]
		public void GetSolrSearch(MessageEndPointArguments arguments)
		{
			JsonArray predicates = null;
			IRecordList<ModelPredicate> preds = null;
			if (arguments.QueryString["predicates"] != null)
			{
				predicates = (JsonArray)JSON.DeserializeObject(arguments.QueryString["predicates"]);
				preds = RecordSerializer.DeserializeList<ModelPredicate>(predicates, new DeserializationContext());
			}
			SearchSolr(arguments.QueryString, arguments.Response.Writer, preds);
		}

		public void SearchSolr(NameValueCollection args, IMessageWriter writer, IRecordList<ModelPredicate> preds)
		{
			string query = args["q"];
            int requestCount = args["count"] != null ? int.Parse(args["count"]) : 10;
            int start = args["start"] != null ? int.Parse(args["start"]) : 0;

            SortInfo sortInfo = this.GetSortInfo(args["sortBy"]);
			SortInfo dataProviderSort =  this.GetSortInfo(args["dataProviderSortBy"]);
			FilterSet filterQueries = BuildCachedFilterSetT<T>(preds);

            // build response object
            writer.OpenRoot("response");

            Type type = typeof(T);
            ISearchOptions options = IndexManager.Instance.GenerateOptionsObject();
            options.Type = type.ToString();
            options.Facets = GetFacets(args);
            options.Rows = requestCount;
			if (sortInfo != null)
			{
				options.Sorts = new List<SortInfo> { sortInfo };
			}
            options.Start = start;

            // set up the query handler to use.
            if (!string.IsNullOrEmpty(args["qt"]) )
				options.ExtraParams["qt"] = args["qt"];

			ISearchProviderQueryResults<RecordKey> results = IndexManager.Instance.SearchInt(query, filterQueries, options);
			List<int> ids = results.Results.Select(rk => rk.Id).ToList();
			IRecordList<T> recordsFound = DataProvider.LoadList<T>(new FilterInfo("ROWID", ids, FilterOperation.In), dataProviderSort);			

            try
            {
                RecordSerializer.Serialize(recordsFound, args["fields"], type, writer);
            }
            catch (Exception ex)
            {
                log.Error("Error serializing primary results", ex);
                throw ex;
            }

            writer.WriteProperty("total", results.NumFound);
            writer.WriteProperty("count", recordsFound.Count);
            writer.WriteProperty("start", start);

            if (results.Facets != null && results.Facets.Count > 0)
            {
                writer.OpenProperty("facets");
                writer.OpenObject();
                foreach (KeyValuePair<String, ICollection<KeyValuePair<String, int>>> kvp in results.Facets)
                {
                    String facetValueName = kvp.Key;
                    ICollection<KeyValuePair<String, int>> facetFieldValues = kvp.Value;

                    writer.OpenProperty(facetValueName);
                    writer.OpenList(facetValueName + "Value");

                    foreach (KeyValuePair<String, int> facetFieldValue in facetFieldValues)
                    {
                        writer.OpenProperty("facet");
                        writer.OpenObject();
                        writer.WriteProperty("value", facetFieldValue.Key);
                        writer.WriteProperty("count", facetFieldValue.Value);
                        writer.CloseObject();
                        writer.CloseProperty();
                    }

                    writer.CloseList();
                    writer.CloseProperty();
                }
                writer.CloseObject();
                writer.CloseProperty();
            }
            if (results.FacetQueries != null && results.FacetQueries.Count > 0)
            {
                writer.OpenProperty("facetQueries");
                writer.OpenObject();

                foreach (KeyValuePair<string, int> kvp in results.FacetQueries)
                {
                    writer.WriteProperty(kvp.Key, kvp.Value);
                }

                writer.CloseObject();
                writer.CloseProperty();
            }

            writer.CloseRoot();
       	}
		
		private IFacets GetFacets(NameValueCollection args)
        {
            String facetsbool = args["facet"];
            bool facets = false;
            if (!bool.TryParse(facetsbool, out facets) || facets == false)
                return null;

            IFacets facetsItf = IndexManager.Instance.GenerateFacetsObject();
            String facetFields = args["facetFields"];
            if (!String.IsNullOrEmpty(facetFields))
            {
                String[] tokens = facetFields.Split(',');
                int count = tokens.GetLength(0);
                for (int i = 0; i < count; i++)
                {
                    facetsItf.Fields.Add(tokens[i]);
                }
            }
            facetsItf.Limit = GetIntArg(args, "facetLimit", "DefaultFacetLimit", 10);
            facetsItf.MinCount = GetIntArg(args, "facetMinCount", "DefaultFacetMinCount", 1);
            return facetsItf;
        }
		
		private int GetIntArg(NameValueCollection args, String argName, String settingName, int settingDefault)
        {
            int valueInt;
            String valueString = args[argName];
            if (!Int32.TryParse(valueString, out valueInt))
            {
                valueInt = Setting.GetValueT<int>(settingName, settingDefault);
            }
            return valueInt;
        }

        private bool GetBoolArg(NameValueCollection args, String argName, String settingName, bool settingDefault)
        {
            bool valueBool;
            String valueString = args[argName];
            if (!bool.TryParse(valueString, out valueBool))
            {
                valueBool = Setting.GetValueT<bool>(settingName, settingDefault);
            }
            return valueBool;
        }

		private FilterSet BuildCachedFilterSetT<T>(IRecordList<ModelPredicate> preds) where T : AbstractRecord, new()
		{
			FilterSet cachedFilterSet = new FilterSet(FilterJoinOperator.And);
			cachedFilterSet.Rules.Add(new FilterInfo("RecordType", typeof(T).FullName));
			if (preds != null)
			{
				foreach (var pred in preds)
				{
					cachedFilterSet.Rules.Add((FilterInfo)pred);
				}
			}
			return cachedFilterSet;
		}
		
		[MessageServiceEndPoint("",Verb=RestOperation.Get)]
		public void Get(MessageEndPointArguments arguments)
		{
			IRecordList<T> records = DataProvider.LoadList<T>();
			IMessageWriter writer = arguments.Response.Writer;
			writer.OpenRoot("response");            
			RecordSerializer.Serialize((IEnumerable<T>)records, arguments.QueryString["fields"], writer);
            writer.CloseRoot();
		}

		[MessageServiceEndPoint("\\d+",Verb=RestOperation.Put)]
		[MessageDescription("Modify an existing {ModelName}.")]
		public void Put(MessageEndPointArguments arguments)
		{
			string id = arguments.Matches[0].Groups[0].Value;
			if( arguments.InMessage.ContainsKey("id") && (string)arguments.InMessage["id"] != id)
			{
				log.WarnFormat("Id on url: [{0}] Id in message: [{1}]", id, arguments.InMessage["id"] );
				throw new InvalidOperationException("Request body cannot contain a different id from id on url.");
			}
			arguments.InMessage["id"] = id;
			//TODO: what if they don't have a valid id, or if it doesn't match an existing object?
			DeserializationContext context = new DeserializationContext(){Records = new List<AbstractRecord>(), Lists = new List<RecordPropertyList>()};
			T record = RecordSerializer.DeserializeRecord<T>(arguments.InMessage, context);
			record.ValidateAndThrow();
			context.SaveChanges();
			string fields = "version";
			if( arguments.QueryString != null && !string.IsNullOrEmpty(arguments.QueryString["fields"]) )
			{
				fields = arguments.QueryString["fields"];
			}
            IMessageWriter writer = arguments.Response.Writer;
			RecordSerializer.Serialize(record, fields, writer);
            writer.Flush();
		}

		[MessageServiceEndPoint("(\\d+)/(\\w+)",Verb=RestOperation.Post)]
		[MessageDescription("add records into an existing {ModelName}'s property list.")]
		public void PostToPropertyList(MessageEndPointArguments arguments)
		{
			//we are authorizing a modification (PUT) on record, that POSTs new associations.
			T record = AbstractRecord.Load<T>(arguments.Matches[0].Groups[1].Value);
			string lProperty = arguments.Matches[0].Groups[2].Value;
			string uProperty = Util.CamelToPascal(lProperty);
			if (WebServiceManager.DoAuth())
				ServiceManager.AuthorizeField(RestOperation.Put, record, lProperty);
			IRecordList recordPropertyList = (IRecordList)record[uProperty];
			DeserializationContext context = new DeserializationContext(){Records = new List<AbstractRecord>(), Lists = new List<RecordPropertyList>()};
			IRecordList newItems = RecordSerializer.DeserializeNonGenericList( recordPropertyList.RecordType, (JsonArray)arguments.InMessage[lProperty], context );			
#if false
			List<ValidationError> errors = null;
			foreach( AbstractRecord r in newItems )
			{
				errors = r.Validate( lProperty + "." + r.Id + ":", errors);
			}
			
			if( errors != null )
			{
				throw new ValidationException( "Multi-save validation errors", errors);
			}
#endif
			foreach( AbstractRecord r in newItems )
			{
				r.Parent = record;
			}
			context.SaveChanges();
			foreach( AbstractRecord r in newItems )
			{
				if( ! recordPropertyList.Contains( r ) )
					recordPropertyList.Add( r );
			}
			record.SaveRelations(uProperty);
			
            BuildReturnList("details", "details", recordPropertyList.Count, arguments.QueryString, newItems, arguments.Response.Writer);
            arguments.Response.Writer.Flush();
		}
		
		[MessageServiceEndPoint("saveAll", Verb=RestOperation.Post)]
		public void SaveAll(MessageEndPointArguments arguments)
		{
			DataProvider.LoadList<T>().Save();
		}
		
		[MessageServiceEndPoint("",Verb=RestOperation.Post)]
		[MessageDescription("Create a new {ModelName}.")]
		public void Post(MessageEndPointArguments arguments)
		{
			if( arguments.InMessage.ContainsKey("id") )
			{
				throw new UnauthorizedAccessException("Cannot POST with an id.");
			}
			DeserializationContext context = new DeserializationContext(){Records = new List<AbstractRecord>(), Lists = new List<RecordPropertyList>()};
			T record = RecordSerializer.DeserializeRecord<T>(arguments.InMessage,context);
			record.ValidateAndThrow();
			context.SaveChanges();
			if( HttpContext.Current != null )
			{
				string location = HttpContext.Current.Request.Url.AbsoluteUri;
				if( location.Contains("?") )
				   location = location.Substring(0,location.IndexOf("?"));
				location += record.Id.ToString();
				HttpContext.Current.Response.AppendHeader("Location",location);
			}

			string fields = "id";
			if( arguments.QueryString != null && !string.IsNullOrEmpty(arguments.QueryString["fields"]) )
			{
				fields = arguments.QueryString["fields"];
			}
            IMessageWriter writer = arguments.Response.Writer;
			RecordSerializer.Serialize(record,fields, writer);
            writer.Flush();
			arguments.Response.StatusCode = 201;
			arguments.Response.StatusDescription = "Created";
		}
		
		[MessageServiceEndPoint("(\\d+)/(\\w+)",Verb=RestOperation.Delete)]
		[MessageDescription("deletes records from an existing {ModelName}'s property list.")]
		public void DeleteFromPropertyList(MessageEndPointArguments arguments)
		{
			//we are authorizing a modification (PUT) on record, that DELETEs existing associations.
			T record = AbstractRecord.Load<T>(arguments.Matches[0].Groups[1].Value);
			string lProperty = arguments.Matches[0].Groups[2].Value;
			string uProperty = Util.CamelToPascal(lProperty);
			if (WebServiceManager.DoAuth())
				ServiceManager.AuthorizeField(RestOperation.Put, record, lProperty);
			IRecordList recordPropertyList = (IRecordList)record[uProperty];
			DeserializationContext context = new DeserializationContext();  //create an empty instance to pass along.
			IRecordList itemsToDelete = RecordSerializer.DeserializeNonGenericList( recordPropertyList.RecordType, (JsonArray)arguments.InMessage[lProperty], context );
			foreach( AbstractRecord r in itemsToDelete )
			{
				log.DebugFormat("removing {0} from {1}", r, record);
				recordPropertyList.Remove( r );
			}
			record.SaveRelations(uProperty);
            IMessageWriter writer = arguments.Response.Writer;
			writer.OpenObject();
            writer.WriteProperty("total", recordPropertyList.Count);
            writer.CloseObject();
            writer.Flush();
		}
		
		[MessageServiceEndPoint("\\d+",Verb=RestOperation.Delete)]
		[MessageDescription("Delete a {ModelName}.")]
		public void Delete(MessageEndPointArguments arguments)
		{
			T record = AbstractRecord.Load<T>(arguments.Matches[0].Captures[0].Value);
			if( record != null )
			{
				log.Debug("deleting object ", record, arguments.Matches[0].Captures[0].Value);
                if (WebServiceManager.DoAuth())
				    ServiceManager.Authorize(RestOperation.Delete, arguments.InMessage, record);
				record.Delete();
			}			
			arguments.Response.StatusCode = 204;
			arguments.Response.StatusDescription = "No Content";
		}
		
		[MessageServiceEndPoint("\\d+",Verb=RestOperation.Copy)]
		[MessageDescription("Copy the requested object.  The new object is saved and fields can be requested using the fields arugment.")]
		public void Copy(MessageEndPointArguments arguments)
		{
			T record = null;
            Response response = arguments.Response;

			string id = arguments.Matches[0].Groups[0].Value;
			if( arguments.InMessage != null && (string)arguments.InMessage["id"] != id )
			{
				log.WarnFormat("Id on url: [{0}] Id in message: [{1}]", id, arguments.InMessage["id"] );
				throw new InvalidOperationException("Cannot COPY with a different id in the request body than was passed on the URL.");
			}
			if( arguments.InMessage == null )
				record = AbstractRecord.Load<T>(id);
			else
			{	
				arguments.InMessage["id"] = id;
				record = RecordSerializer.DeserializeRecord<T>(arguments.InMessage, new DeserializationContext());
			}
			record = (T)record.Clone();
			log.Debug("Before record save, after clone: ", record, record.Id);
			record.Save();
			log.Debug("Before record save, after clone: ", record, record.Id);
			string fields = "id";
			if( arguments.QueryString != null && !string.IsNullOrEmpty(arguments.QueryString["fields"]) )
			{
				fields = arguments.QueryString["fields"];
			}
            RecordSerializer.Serialize(record, fields, response.Writer);
            response.Writer.Flush();
		}
		
		private SortInfo GetSortInfo (string sort)
		{
			SortInfo sortInfo = null;
			if (sort != null) {
				sort = Util.CamelToPascal(sort);
				SortDirection dir = SortDirection.Ascending;
				if (sort.Contains (' ')) {
					dir = sort.EndsWith ("desc") ? SortDirection.Descending : SortDirection.Ascending;
					sort = sort.Substring (0, sort.IndexOf (" "));
				}
				sortInfo = new SortInfo (sort, dir);
			}
			return sortInfo;
		}
		
		private void BuildReturnList(string nodeName, string listName, int totalCount, NameValueCollection args, IRecordList records, IMessageWriter writer)
		{
			int requestCount = args["count"] != null ? int.Parse(args["count"]) : -1;
			int start = args["start"] != null ? int.Parse(args["start"]) : -1;
			BuildReturnList(nodeName,listName,args,records,start,requestCount, totalCount, writer);
		}
		
		private void BuildReturnList(string rootName, string listName, NameValueCollection args, IRecordList records,
		                                    int start, int requestCount, int totalCount, IMessageWriter writer)
		{
			log.DebugFormat("building return list: rootName: {4}, records: {0} with count: {1}, start #: {2}, requestCount: {3}", records, records.Count,
			                start, requestCount, rootName);

            writer.OpenRoot(rootName);
            writer.WriteProperty("total", totalCount);
			//validate all records.  if any fail, UnauthorizedAccessException will abort the request.

			if( start != -1 && requestCount != -1 )
			{
				RecordSerializer.Serialize( GetPageSet(records,start,requestCount), listName, args["fields"], records.RecordType, writer );				
			}
			else
			{
				RecordSerializer.Serialize( records, listName, args["fields"], writer );
			}
            writer.CloseRoot();
		}

		
		private void BuildReturnList(string nodeName, string listName, int totalCount, NameValueCollection args, List<int> ids, Type recordType, IMessageWriter writer)
		{
			int requestCount = args["count"] != null ? int.Parse(args["count"]) : -1;
			int start = args["start"] != null ? int.Parse(args["start"]) : -1;
            String sortBy = args["sortBy"];
			BuildReturnList(nodeName, listName, args, ids, start, requestCount, totalCount, sortBy, recordType, writer);
		}
		
		private void BuildReturnList(string nodeName, string listName, NameValueCollection args, List<int> ids,
		                                    int start, int requestCount, int totalCount, String sortBy, Type recordType, IMessageWriter writer)
		{
			log.DebugFormat("building return list: nodeName: {4}, records: {0} with count: {1}, start #: {2}, requestCount: {3}", ids, ids.Count,
			                start, requestCount, nodeName);

            writer.OpenRoot(nodeName);
            writer.WriteProperty("total", totalCount);
			//validate all records.  if any fail, UnauthorizedAccessException will abort the request.

			if( start != -1 && requestCount != -1 )
			{
                writer.WriteProperty("start", start);
				start = Math.Min (start, ids.Count - 1);
				requestCount = Math.Min (requestCount, ids.Count - start);
				RecordSerializer.SerializeIntsList( ids.GetRange(start,requestCount).AsEnumerable(), null, args["fields"], sortBy, recordType, writer);				
			}
			else
			{
				RecordSerializer.SerializeIntsList( ids.AsEnumerable(), null, args["fields"], sortBy, recordType, writer );
			}
            writer.CloseRoot();
		}
				
		private IEnumerable<AbstractRecord> GetPageSet(IRecordList records, int start, int requestCount )
		{
			int end = records.Count > start + requestCount ? start + requestCount : records.Count;
			for( int i = start; i < end; i++ )
				yield return records[i];
		}
		
		#region IMessageServiceManager implementation
		public void Authorize (RestOperation operation, string method, JsonObject message)
		{
			//RestOperation operation, MessageNode recordNode, AbstractRecord record
    	    ServiceManager.Authorize(operation, message, null);
		}
		
		public string GenerateHelpText ()
		{
			return ServiceManager.GetHelpText();
		}
		
		public void GenerateExampleRequestNode (string message, IMessageWriter writer)
		{
			if( message == "Put" || message == "Post" )
			{
				RecordSerializer.Serialize( ServiceManager.GenerateExampleRecord(), ServiceManager.GenerateExampleFields( message ), writer );
#if false
                // uh...huh?   not sure how to do this in the IMessageWriter world. 
				if( response != null && response.ContainsKey("id") )
					response.Remove("id");
#endif
			}
		}
		
		public void GenerateExampleResponseNode (string message, IMessageWriter writer)
		{
			if( message.StartsWith( "Get" ) )
				RecordSerializer.Serialize( ServiceManager.GenerateExampleRecord(), ServiceManager.GenerateExampleFields( message ) , writer);
			else if( message == "Post" )
			{
				//TODO: sometimes we might want to return values that are created on the object on teh server
                writer.OpenRoot("response");
                writer.WriteProperty("createdId", 123);
			}
		}
		#endregion

	}
}
