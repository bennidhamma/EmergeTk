
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using EmergeTk.Model;
using SolrNet;
using SolrNet.Commands;
using SolrNet.Commands.Parameters;
using SolrNet.Impl;
using SolrNet.Impl.FieldSerializers;
using System.Xml;
using System.Threading;
using SolrNet.Impl.QuerySerializers;
using SolrNet.Impl.FacetQuerySerializers;

namespace EmergeTk.Model.Search
{
	public class SolrSearchProvider : ISearchServiceProvider
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(SolrSearchProvider));
        private static readonly EmergeTkLog queryLog = EmergeTkLogManager.GetLogger("SolrQueries");

        [ThreadStaticAttribute]
        private static StopWatch solrWatch;  // keep track of time spent in HTTP call

        [ThreadStaticAttribute]
        private static StopWatch parseWatch;  // track time it takes us to parse.

        [ThreadStaticAttribute]
        private static StopWatch providerWatch;  // track time it takes us to do ancillary SOLR provider operations.


        public static StopWatch SolrWatch
        {
            get
            {
                if (solrWatch == null)
                {
                    solrWatch = new StopWatch("SOLR Calls", "SOLR HTTP Call Time");
                }
                return solrWatch;
            }
        }

        public static StopWatch ProviderWatch
        {
            get
            {
                if (providerWatch == null)
                {
                    providerWatch = new StopWatch("SOLR Provider", "SOLR Provider Ancillary operations time");
                }
                return providerWatch;
            }
        }

        public static StopWatch ParseWatch
        {
            get
            {
                if (parseWatch == null)
                {
                    parseWatch = new StopWatch("Parse SOLR Responses", "SOLR Response Parse Time");
                }
                return parseWatch;
            }
        }
		
		string connectionString;

		bool commitEnabled = true;
        SearchSerializerTestHook testHook = null;

		public bool CommitEnabled
		{
			get
			{
				return commitEnabled;
			}
			set
			{
				commitEnabled = value;
			}
		}

        public SearchSerializerTestHook TestHook
        {
            set
            {
                testHook = value;
            }
        }

		public SolrSearchProvider()
		{
			connectionString = Setting.GetValueT<String>("SolrConnectionString","http://localhost:8983/solr");
		}

        private SolrConnection GetConnection()
        {
            SolrConnection conn = new SolrConnection(connectionString);
            int solrTimeOut = Setting.GetValueT<int>("SolrConnectionTimeout", -1);
            conn.Timeout = solrTimeOut;
            return conn;
        }
		
		object throttleLock = new object ();
		
		private void ExecuteCommand( ISolrCommand cmd )
		{
			lock (throttleLock)
			{
				log.Debug("executing command ", cmd );
	            ISolrConnection conn = GetConnection();
				cmd.Execute(conn);
				log.Debug("done executing command, comitting.");
				Commit();
				log.Debug("committed");
				Thread.Sleep (30);
			}
		}

		public void Commit()
		{
			if (commitEnabled)
			{
				lock (throttleLock)
				{
					CommitCommand cmd = new CommitCommand();
	                SolrConnection conn = GetConnection();
					cmd.Execute(conn);
					Thread.Sleep (30);
				}
			}
		}
		
		#region ISearchServiceProvider implementation
		public void GenerateIndex (IRecordList elements)
        {
            if (testHook == null)
            {
                AddCommand<AbstractRecord> cmd = new AddCommand<AbstractRecord>(elements.ToArray().Select(r=> new KeyValuePair<AbstractRecord, double?>(r, null)), new EmergeTkSolrSerializer<AbstractRecord>());
                ExecuteCommand(cmd);
            }
            else
            {
                EmergeTkSolrSerializer<AbstractRecord> serializer = new EmergeTkSolrSerializer<AbstractRecord>();
                foreach (AbstractRecord r in elements)
                {
                    testHook(serializer.Serialize(r, null));
                }
            }
		}
		
		public void GenerateIndex (AbstractRecord r)
		{
			AddCommand<AbstractRecord> cmd = new AddCommand<AbstractRecord>(new KeyValuePair<AbstractRecord, double?>[]{new KeyValuePair<AbstractRecord, double?>(r, null)}, new EmergeTkSolrSerializer<AbstractRecord>());
			ExecuteCommand(cmd);
		}
		
		public void Delete (AbstractRecord r)
		{
			DeleteCommand cmd = new DeleteCommand(new DeleteByIdAndOrQueryParam(new string[] { r.GetType().FullName + "." + r.Id }, null, null));
			ExecuteCommand(cmd);
		}
		
		public void Delete (IRecordList elements)
		{
			List<string> deleteList = new List<string>();
			foreach( AbstractRecord r in elements )
			{
				deleteList.Add( r.GetType().FullName + "." + r.Id );
			}
			DeleteCommand cmd = new DeleteCommand(new DeleteByIdAndOrQueryParam(deleteList.ToArray(), null, null));
			ExecuteCommand(cmd);
		}
		
		public void DeleteAll()
		{
			DeleteByQuery("*:*");
		}
		
		public void DeleteAllOfType<T>()
		{
			DeleteByQuery("RecordType:" + typeof(T).FullName );
		}
		
		public void DeleteByQuery(string query)
		{
			DeleteCommand cmd = new DeleteCommand(new DeleteByIdAndOrQueryParam(null, new SolrQuery(query), new DefaultQuerySerializer(new DefaultFieldSerializer())));
			ExecuteCommand(cmd);
		}
		
		public IRecordList Search (string field, string key)
		{			
			return Search(string.Format("{0}:{1}", field, key));
		}

		private SolrQueryExecuter<T> GetExecuterT<T>(ISolrQueryResultParser<T> parser)
		{
			SolrConnection conn = GetConnection();
			ISolrFieldSerializer fieldSerializer = new DefaultFieldSerializer();
			ISolrQuerySerializer querySerializer = new DefaultQuerySerializer(fieldSerializer);
			ISolrFacetQuerySerializer facetQuerySerializer = new DefaultFacetQuerySerializer(querySerializer, fieldSerializer);
			return new SolrQueryExecuter<T>(parser, conn, querySerializer, facetQuerySerializer);
		}

		public IRecordList Search( string query )
		{
			SolrQuery q = new SolrQuery( query );
			SolrQueryExecuter<RecordKey> queryExec = GetExecuterT<RecordKey>(new EmergeTkFastParser());
			ISolrQueryResults<RecordKey> results = queryExec.Execute( q, new QueryOptions() );
			RecordList list = new RecordList();
			foreach( RecordKey k in results )
			{
				AbstractRecord r = k.ToRecord();
				if( r != null )
					list.Add( r );
				else
				{
					log.Warn("Did not load record key ", k);
				}
				//log.Debug("Adding record to search results", r);
			}
			
			return list;	
		}
		
		public void Optimize()
		{
			OptimizeCommand oc = new OptimizeCommand();
			oc.Execute(GetConnection());
		}
		
		public IRecordList<T> Search<T> (string field, string key, System.Collections.Generic.List<string> types) where T : AbstractRecord, new()
		{
			RecordList<T> result = new RecordList<T>();
			foreach(T r in Search<T>(string.Format("{0}:{1}", field, key)) )
			{
				if( types.Contains( r.GetType().FullName ) )
				{
					result.Add( r );
				}
			}
			return result;
		}
		
		public IRecordList<T> Search<T> (string field, string key) where T : AbstractRecord, new()
		{
			return Search<T>(string.Format("{0}:{1} RecordType:{2}", field, key,  typeof(T).FullName));
		}
		
		public IRecordList<T> Search<T> (string query) where T : AbstractRecord, new()
		{
			SolrQuery q = new SolrQuery( query + " RecordType:" + typeof(T).FullName);
			SolrQueryExecuter<T> queryExec = GetExecuterT<T>(new EmergeTkParser<T>());
			ISolrQueryResults<T> results = queryExec.Execute( q, new QueryOptions() );
			RecordList<T> list = new RecordList<T>();
			foreach( T t in results )
			{
				list.Add( t );
				//log.Debug("adding result", t );
			}
			
			return list;
		}

		public IRecordList<T> Search<T>( string query, SortInfo sort, int start, int count, out int numFound ) where T : AbstractRecord, new()
		{
			log.Info("Executing solr query: ", query );
			QueryOptions options = new QueryOptions();

			options.Start = start;

			options.Rows = count;

			if( sort != null )
			{
				SortOrder so = new SortOrder( sort.ColumnName, sort.Direction == SortDirection.Ascending ? Order.ASC : Order.DESC );
				options.AddOrder( so );				
			}

			SolrConnection conn = GetConnection();
			SolrQuery q = new SolrQuery( query + " RecordType:" +  typeof(T).FullName );
			SolrQueryExecuter<T> queryExec = GetExecuterT<T>(new EmergeTkParser<T>());
			ISolrQueryResults<T> results = queryExec.Execute( q, options );
			RecordList<T> list = new RecordList<T>();
			foreach( T t in results )
			{
				list.Add( t );
				log.Debug("adding result", t );
			}
			log.DebugFormat("SOLR numFound: {0} # Records: {1} ", results.NumFound, list.Count );
			numFound = results.NumFound;

			return list;
		}
 
				
		/// <summary>
		/// SearchInt - simple interface for returning list of recordkeys.
		/// </summary>
		/// <param name="field">
		/// A <see cref="System.String"/> default field to search. - ignored by this 
		/// implementation.
		/// </param>
		/// <param name="query">
		/// A <see cref="System.String"/> the query to search solr for.
		/// </param>
		/// <param name="type">
		/// A <see cref="System.String"/> filter to objects of this type.
		/// </param>
		/// <param name="pageNumber">
		/// A <see cref="System.Int32"/> a page number to start, -1 to ignore.
		/// </param>
		/// <param name="pageSize">
		/// A <see cref="System.Int32"/> number of records to return for the given page. -1 to ignore.
		/// </param>
		/// <param name="sort">
		/// A <see cref="SortInfo"/> specify sorting criteria.  optional.
		/// </param>
		/// <returns>
		/// A <see cref="List"/> A list of recordkeys describing the resultset.
		/// </returns>
		public List<RecordKey> SearchInt (string field, string query, string type, SortInfo[] sorts, int start, int count, out int numFound )
		{
            QueryOptions options = SimpleQueryOptions(sorts, start, count, null);
            return SearchInt(query, type, options, out numFound);
		}

        /// <summary>
        /// Generates log-suitable string from SOLR query.
        /// </summary>
        /// <param name="query">SolrQuery string</param>
        /// <param name="options">SolrNet QueryOptions object</param>
        /// <returns></returns>
        private void SolrQLogString(String query, QueryOptions options)
        {
            if (options.FilterQueries == null || options.FilterQueries.Count == 0)
            {
                queryLog.InfoFormat("Issuing SOLR query = {0}", query);
                return;
            }

            StopWatch watch = new StopWatch("SolrQLogString", queryLog);
            watch.Start();
            StringBuilder sb = new StringBuilder(query + "&");

		
            IEnumerable<String> fqs = options.FilterQueries.Select(q => String.Format("fq={0}", ((SolrQuery)q).Query));
            sb.Append(String.Join("&", fqs.ToArray()));
            watch.Stop();
            queryLog.InfoFormat("Issuing SOLR query = {0}", sb.ToString());
        }

        /// <summary>
        /// shared legacy function for simple SearchInt() functionality.
        /// </summary>
        /// <param name="query">query in string form</param>
        /// <param name="type">type of record to return</param>
        /// <param name="options">SolrNet query options object</param>
        /// <param name="numFound">count of entire number of records present</param>
        /// <returns>List of RecordKey's, maxcount will be the count in the options 
        /// parameter
        /// </returns>
   		private List<RecordKey> SearchInt(string query, String type, QueryOptions options, out int numFound )
        {
            SolrSearchProvider.ProviderWatch.Start();
            StopWatch watch = new StopWatch("SolrSearchProvider.SearchInt", queryLog);
            watch.Start();
            SolrQLogString(query, options);
            if (type != null)
                query += " RecordType:" + type;

            SolrConnection conn = GetConnection();
            SolrQuery q = new SolrQuery(query);
			SolrQueryExecuter<RecordKey> queryExec = GetExecuterT<RecordKey>(new EmergeTkFastParser());
            SolrSearchProvider.ProviderWatch.Stop();

            SolrSearchProvider.SolrWatch.Start();  // parser will stop this watch. 
            ISolrQueryResults<RecordKey> results = queryExec.Execute(q, options);

            SolrSearchProvider.ProviderWatch.Start();
            numFound = results.NumFound;
            log.InfoFormat("SOLR returned {0} total result(s), count {1}", results.NumFound, results.Count);
            List<RecordKey> list = new List<RecordKey>();
            foreach (RecordKey rk in results)
            {
                //log.Debug("Adding result", rk );
                list.Add(rk);
            }
            watch.Lap("Completed SOLR call");
            watch.Stop();
            SolrSearchProvider.ProviderWatch.Stop();
            return list;
        }

        /// <summary>
        /// private function returns queryoptions object for simple case, where we just 
        /// need a sort, start, and count.
        /// </summary>
        /// <param name="sorts">array of sort criteria</param>
        /// <param name="start">offset into results to return</param>
        /// <param name="count">count of results to return</param>
        /// <returns></returns>
        private QueryOptions SimpleQueryOptions(SortInfo[] sorts, int start, int count, FilterSet cachedQueries)
        {
            SolrSearchProvider.ProviderWatch.Start();
            QueryOptions options = new QueryOptions();
            if (sorts != null)
            {
                foreach (SortInfo sort in sorts)
                {
                    SortOrder so = new SortOrder(sort.ColumnName, sort.Direction == SortDirection.Ascending ? Order.ASC : Order.DESC);
                    options.AddOrder(so);
                }
            }
            if (start != -1)
                options.Start = start;

            if (count != -1)
                options.Rows = count;

            AddCachedQueries(options, cachedQueries);
            SolrSearchProvider.ProviderWatch.Stop();
            return options;
        }

        /// <summary>
        /// Function that the emergeTk gallery calls to do searching - returns a list of
        /// recordkeys only; then the entire objects are reconstituted from the data 
        /// layer.
        /// </summary>
        /// <param name="field">
        /// A <see cref="System.String"/> default field to search.
        /// </param>
        /// <param name="mainQuery">
        /// A <see cref="Chaos.Model.FilterSet"/> the dynamic part of the query to 
        /// search for.
        /// </param>
        /// <param name="cachedQuery">
        /// A <see cref="Chaos.Model.FilterSet"/> set of predicates to be implemented as
        /// filter queries.  These are things we believe SOLR can effectively cache.
        /// </param>
        /// <param name="type">
        /// A <see cref="System.String"/> filter to objects of this type.
        /// </param>
        /// <param name="pageNumber">
        /// A <see cref="System.Int32"/> a page number to start, -1 to ignore.
        /// </param>
        /// <param name="pageSize">
        /// A <see cref="System.Int32"/> number of records to return for the given page. -1 to ignore.
        /// </param>
        /// <param name="sort">
        /// A <see cref="SortInfo"/> specify sorting criteria.  optional.
        /// </param>
        /// <returns>
        /// A <see cref="List"/> A list of recordkeys describing the resultset.
        /// </returns>
        public List<RecordKey> SearchInt(string field, FilterSet mainQuery, FilterSet cachedQuery, string type, SortInfo[] sorts, int start, int count, out int numFound)
        {
            String query = GetFilterFormatter().BuildQuery(mainQuery);
            QueryOptions options = SimpleQueryOptions(sorts, start, count, cachedQuery);
            return SearchInt(query, type, options, out numFound);
        }
		
		public ISearchFilterFormatter GetFilterFormatter()
		{
			return new SolrFilterFormatter();
		}

		public ISearchProviderQueryResults<T> Search<T>(String query, FilterSet cachedQuery, ISearchOptions options) where T : AbstractRecord, new()           
		{
            SolrSearchProvider.ProviderWatch.Start();
            StopWatch watch = new StopWatch("SolrSearchProvider.Search<T>", queryLog);
            watch.Start();
            QueryOptions queryOptions = PrepareQueryOptions(ref query, options, cachedQuery);
            SolrQLogString(query, queryOptions);
            SolrConnection conn = GetConnection();
			SolrQuery q = new SolrQuery(query);
			SolrQueryExecuter<T> queryExec = GetExecuterT<T>(new EmergeTkParser<T>(options));

            SolrSearchProvider.ProviderWatch.Stop();
            SolrSearchProvider.SolrWatch.Start();
			ISolrQueryResults<T> resultsSolrNet = queryExec.Execute(q, queryOptions);
            watch.Lap(String.Format("SOLR returned results - numFound = {0}", resultsSolrNet.NumFound));

            SolrSearchProvider.ProviderWatch.Start();
			SolrSearchProviderQueryResults<T> results = new SolrSearchProviderQueryResults<T>();
			results.Results = resultsSolrNet;
			results.Facets = resultsSolrNet.FacetFields;
            results.FacetQueries = resultsSolrNet.FacetQueries;
			results.NumFound = resultsSolrNet.NumFound;

            if (options.MoreLikeThis != null)
            {
                Dictionary<T, int> moreLikeThisOrder = new Dictionary<T, int>();
                foreach (KeyValuePair<String, IList<T>> kvp in resultsSolrNet.SimilarResults)
                {
                    // we don't care about kvp.Key - it's the original result doc; we're throwing
                    // away that relationship for now.
                    IList<T> list = kvp.Value;
                    foreach (T t in list)
                    {
                        if (moreLikeThisOrder.ContainsKey(t))
                            moreLikeThisOrder[t]++;
                        else // never been seen before, add it.
                            moreLikeThisOrder[t] = 1;
                    }
                }
                results.MoreLikeThisOrder = moreLikeThisOrder;
            }
            watch.Lap("Completed SOLR call");
            watch.Stop();
            SolrSearchProvider.ProviderWatch.Stop();
			return results;
		}

        private IEnumerable<int> GetRecordIds<T>(Dictionary<T, int> dict, Func<T, int> selector) 
        {
            foreach (KeyValuePair<T, int> kvp in dict)
                yield return selector(kvp.Key);
        }

        
		public ISearchProviderQueryResults<RecordKey> SearchInt(String query, FilterSet cachedQuery, ISearchOptions options)
		{
            SolrSearchProvider.ProviderWatch.Start();
            StopWatch watch = new StopWatch("SolrSearchProvider.SearchInt<RecordKey>", queryLog);
            watch.Start();

        	QueryOptions queryOptions = PrepareQueryOptions(ref query, options, cachedQuery);
            SolrQLogString(query, queryOptions);
            SolrConnection conn = GetConnection();

			SolrQuery q = new SolrQuery(query);
			SolrQueryExecuter<RecordKey> queryExec = GetExecuterT<RecordKey>(new EmergeTkFastParser(options));

            SolrSearchProvider.ProviderWatch.Stop();
            SolrSearchProvider.SolrWatch.Start();
			ISolrQueryResults<RecordKey> resultsSolrNet = queryExec.Execute(q, queryOptions);
            SolrSearchProvider.ProviderWatch.Start();

			log.Info("SOLR numFound: ", resultsSolrNet.NumFound);
			SolrSearchProviderQueryResults<RecordKey> results = new SolrSearchProviderQueryResults<RecordKey>();
			results.Results = resultsSolrNet;
			results.Facets = resultsSolrNet.FacetFields;
            results.FacetQueries = resultsSolrNet.FacetQueries;
			results.NumFound = resultsSolrNet.NumFound;

            if (options.MoreLikeThis != null)
            {
                Dictionary<RecordKey, int> moreLikeThisOrder = new Dictionary<RecordKey, int>();
                foreach (KeyValuePair<string, IList<RecordKey>> kvp in resultsSolrNet.SimilarResults)
                {
                    // we don't care about kvp.Key - it's the original result doc; we're throwing
                    // away that relationship for now.
                    IList<RecordKey> list = kvp.Value;
                    foreach (RecordKey t in list)
                    {
                        if (moreLikeThisOrder.ContainsKey(t))
                            moreLikeThisOrder[t]++;
                        else // never been seen before, add it.
                            moreLikeThisOrder[t] = 1;
                    }
                }
                results.MoreLikeThisOrder = moreLikeThisOrder;
            }
            watch.Lap("Completed SOLR call");
            watch.Stop();
            SolrSearchProvider.ProviderWatch.Stop();
			return results;
		}


		public ISearchOptions GenerateOptionsObject()
		{
			return new SolrSearchOptions();
		}

		public IFacets GenerateFacetsObject()
		{
			return new SolrFacets();
		}

		public IMoreLikeThis GenerateMoreLikeThisObject()
		{
			return new SolrMoreLikeThis();
		}

        private void SetSorts(QueryOptions queryOptions, IEnumerable<SortInfo> sorts, bool random)
        {
            if (sorts != null)
            {
                SortOrder[] sortOrders = sorts.Select(sort => new SortOrder(sort.ColumnName, sort.Direction == SortDirection.Ascending ? Order.ASC : Order.DESC)).ToArray();
                queryOptions.AddOrder(sortOrders);
            }
            if (random)
            {
                // add the random sort order as the last sort; this is so that all the banners from same place don't
                // group together.  RandomRecordId is a random sorting of RecordId (ROWID on 
                // Banners table).

                SortOrder soRandom = new SortOrder("RandomRecordId");
                queryOptions.AddOrder(soRandom);
            }
        }

		private QueryOptions PrepareQueryOptions(ref String query, ISearchOptions options, FilterSet cachedQueries)
		{
			QueryOptions queryOptions = new QueryOptions();
			queryOptions.Rows = options.Rows;
			queryOptions.Start = options.Start;

            SetSorts(queryOptions, options.Sorts, options.RandomSort);

			// get the facets, if they exist.
			if (options.Facets != null && options.Facets.Fields.Count > 0)
			{
				SolrFacetFieldQuery[] facets = new SolrFacetFieldQuery[options.Facets.Fields.Count];
				int i = 0;
				foreach (String facet in options.Facets.Fields)
				{
					facets[i] = new SolrFacetFieldQuery(facet);
					i++;
				}
				queryOptions.AddFacets(facets);
                queryOptions.Facet.MinCount = options.Facets.MinCount;
                queryOptions.Facet.Sort = true;
				queryOptions.Facet.Limit = options.Facets.Limit;
			}
			if (options.MoreLikeThis != null && options.MoreLikeThis.Fields.Count > 0)
			{
				MoreLikeThisParameters moreLikeThisParms = new MoreLikeThisParameters(options.MoreLikeThis.Fields);
                moreLikeThisParms.Count = options.MoreLikeThis.ChildCount;
				moreLikeThisParms.MinDocFreq = options.MoreLikeThis.MinDocFreq;
				moreLikeThisParms.MinTermFreq = options.MoreLikeThis.MinTermFreq;
				moreLikeThisParms.Boost = options.MoreLikeThis.Boost;
				queryOptions.MoreLikeThis = moreLikeThisParms;
			}
            AddCachedQueries(queryOptions, cachedQueries);

            if (String.IsNullOrEmpty(query))
            {
                // if the query string is empty, then we need to force it to the standard handler.
                if (options.ExtraParams == null)
                    options.ExtraParams = new Dictionary<string, string> { { "qt", "standard" } };
                else
                    options.ExtraParams["qt"] = "standard";

                // use standard handler syntax for all documents returned. 
                query = "*:*";
            }

            if (options.ExtraParams != null)
                queryOptions.ExtraParams = options.ExtraParams;

			return queryOptions;
		}

        private void AddCachedQueries(QueryOptions queryOptions, FilterSet cachedQueries)
        {
            if (cachedQueries != null)
            {
                ISearchFilterFormatter formatter = GetFilterFormatter();
                foreach (IFilterRule rule in cachedQueries.Rules)
                {
                    String queryPlusTag = String.Format("{{!tag={0}}}{1}", ((FilterInfo)rule).ColumnName, formatter.BuildQuery(rule));
                    queryOptions.FilterQueries.Add(new SolrQuery(queryPlusTag));
                }
            }
        }
		#endregion
	}

	public class EmergeTkParser<T> : ISolrQueryResultParser<T> where T : AbstractRecord, new()
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(EmergeTkParser<T>));
        private ISearchOptions options = null;

        public EmergeTkParser()
        {
        }

        public EmergeTkParser(ISearchOptions options)
        {
            this.options = options;
        }
		
		public ISolrQueryResults<T> Parse(string r)
		{
            // done with HTTP call, stop the SOLR watch.
            SolrSearchProvider.SolrWatch.Stop();
            SolrSearchProvider.ParseWatch.Start();
            StopWatch watch = new StopWatch("EmergeTkParser<T>.Parse");
            watch.Start();
		
			Type type = typeof(T);
			
			SolrQueryResults<T> results = new SolrQueryResults<T>();
			var xml = new XmlDocument();
            xml.LoadXml(r);
            watch.Lap("SOLR results parsed");
			results.NumFound = int.Parse(xml.SelectSingleNode("/response/result/@numFound").Value);
			XmlNodeList docs = xml.SelectNodes("/response/result/doc");
            Type hitType = null;
			List<int> ids = new List<int>();
			foreach(XmlNode docNode in docs )
			{
				XmlNode idNode = docNode.SelectSingleNode("int[@name='RecordId']");
				XmlNode typeNode = docNode.SelectSingleNode("str[@name='RecordType']");
				if( null == idNode || null == typeNode )
				{
					log.Warn("could not load result", docNode.OuterXml);
				}
				
				hitType = TypeLoader.GetType(typeNode.InnerText);
				int id = int.Parse( idNode.InnerText );	
				//log.DebugFormat("looking for hit type: {0} id {1}", typeNode.InnerText, id );
				
				if( hitType == null || ! type.IsAssignableFrom(hitType) )
				{
					log.Warn("could not load result", id, typeNode.InnerText);
					continue;
				}

				ids.Add(id);
			}
	        SolrSearchProvider.ParseWatch.Stop();
            IRecordList<T> records = DataProvider.DefaultProvider.Load<T>(ids);
            SolrSearchProvider.ParseWatch.Start();				
			foreach( T t in records )
				results.Add(t);
            watch.Lap("completed building primary SOLR results, including AbstractRecord.Load calls");
            if (hitType != null)
            {
                if (options != null)
                {
                    if (options.Facets != null)
                    {
                        FacetAndMoreLikeThisLoader.LoadFacets<T>(results, xml);
                        watch.Lap("Completed loading facets");
                    }
                    if (options.MoreLikeThis != null)
                    {
                        results.SimilarResults = FacetAndMoreLikeThisLoader.GenerateMoreLikeThis<T>
                            (xml,
                            (t, i) => {
                                SolrSearchProvider.ParseWatch.Stop(); 
                                T record = (T)AbstractRecord.Load(t, i);
                                SolrSearchProvider.ParseWatch.Start();
                                return record;
                            });

                        watch.Lap("Completed loading MoreLikeThis results");
                    }
                }
            }
            watch.Stop();
            SolrSearchProvider.ParseWatch.Stop();
			return results;
		}
	}


	
	public class EmergeTkFastParser : ISolrQueryResultParser<RecordKey> 
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(EmergeTkFastParser));
        private ISearchOptions options = null;

        public EmergeTkFastParser()
        {
        }

        public EmergeTkFastParser(ISearchOptions options)
        {
            this.options = options;
        }
		
		public ISolrQueryResults<RecordKey> Parse(string r)
		{
            // HTTP request completed; stop the SOLR watch.
            SolrSearchProvider.SolrWatch.Stop();
            StopWatch watch = new StopWatch("EmergeTkFastParser.Parse");
            SolrSearchProvider.ParseWatch.Start();
            watch.Start();
			SolrQueryResults<RecordKey> results = new SolrQueryResults<RecordKey>();
			
			var xml = new XmlDocument();
            xml.LoadXml(r);
            watch.Lap("Done loading SOLR XML results");
			results.NumFound = int.Parse(xml.SelectSingleNode("/response/result/@numFound").Value);
			XmlNodeList docs = xml.SelectNodes("/response/result/doc");

			foreach(XmlNode docNode in docs )
			{
				XmlNode idNode = docNode.SelectSingleNode("int[@name='RecordId']");
				XmlNode typeNode = docNode.SelectSingleNode("str[@name='RecordType']");
				if( null == idNode || null == typeNode )
				{
					log.Warn("could not load result", docNode.OuterXml);
					continue;
				}
				
				int id = int.Parse( idNode.InnerText );
				
				RecordKey rk = new RecordKey();
				rk.Id = id;
				rk.Type = typeNode.InnerText;
				
				results.Add( rk );
			}
            watch.Lap("Completed parsing out primary SOLR results");

            if (options != null)
            {
                if (options.Facets != null)
                {
                    FacetAndMoreLikeThisLoader.LoadFacets<RecordKey>(results, xml);
                    watch.Lap("Completed parsing out SOLR facets info");
                }

                if (options.MoreLikeThis != null)
                {
                    results.SimilarResults = FacetAndMoreLikeThisLoader.GenerateMoreLikeThis<RecordKey>
                        (xml,
                        (t, i) => new RecordKey(t.FullName, i));

                    watch.Lap("Completed parsing out MoreLikeThis information");
                }
            }
            watch.Stop();
            SolrSearchProvider.ParseWatch.Stop();
            return results;
		}
	}


    public class FacetAndMoreLikeThisLoader
    {
        private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(FacetAndMoreLikeThisLoader));

        static private void AddFacetFieldItems(XmlDocument xml, String xPath, Dictionary<String, ICollection<KeyValuePair<String, int>>> facetFields)
        {
            XmlNodeList lsts = xml.SelectNodes(xPath);
            foreach (XmlNode lst in lsts)
            {
                String facetName = lst.Attributes.GetNamedItem("name").Value;
                Dictionary<String, int> facetField = new Dictionary<string, int>();

                XmlNodeList values = lst.SelectNodes("int");
                foreach (XmlNode facetItemXmlNode in values)
                {
                    XmlNode nameItemAttr = facetItemXmlNode.Attributes.GetNamedItem("name");
                    String itemName = (nameItemAttr == null) ? "Missing" : nameItemAttr.Value;
                    facetField.Add(itemName, Convert.ToInt32(facetItemXmlNode.InnerText));
                }
                facetFields.Add(facetName, facetField);
            }
        }

        static public void LoadFacets<T>(SolrQueryResults<T> results, XmlDocument xml)
        {
            Dictionary<String, ICollection<KeyValuePair<String, int>>> facetFields = new Dictionary<string, ICollection<KeyValuePair<string, int>>>(StringComparer.CurrentCultureIgnoreCase);

            // get the facet fields
            AddFacetFieldItems(xml, "/response/lst[@name=\"facet_counts\"]/lst[@name=\"facet_fields\"]/lst", facetFields);
            // now get the facet dates
            AddFacetFieldItems(xml, "/response/lst[@name=\"facet_counts\"]/lst[@name=\"facet_dates\"]/lst", facetFields);

            results.FacetFields = facetFields;

            // get the facet queries
            XmlNodeList intList = xml.SelectNodes("/response/lst[@name=\"facet_counts\"]/lst[@name=\"facet_queries\"]/int");
            Dictionary<String, int> facetQueries = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            foreach (XmlNode intItem in intList)
            {
                XmlNode nameItem = intItem.Attributes.GetNamedItem("name");
                String name = nameItem.Value;
                if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(intItem.InnerText))
                    facetQueries.Add(name, Convert.ToInt32(intItem.InnerText));
            }
            if (facetQueries.Count > 0)
                results.FacetQueries = facetQueries;
        }

        private static int GetRecordId(XmlNode result, out string type)
        {
            // the name looks like "Chaos.Model.Banner.1291"
            String name = result.Attributes["name"].Value;
            type = null;
            // strip off Chaos.Model.Banner (or whatever) to get the recordId
            // and turn it to an int.
            int indexDot = name.LastIndexOf('.');
            if (indexDot == -1 || name.Length < (indexDot + 2))
            {
                log.Error("Can't parse recordid from SOLR result name {0}", name);
                return -1;
            }
            type = name.Substring(0, indexDot);
            String recordIdStr = name.Substring(indexDot + 1);
            int id;
            if (!Int32.TryParse(recordIdStr, out id))
            {
                log.Error("Can't parse recordid for record name = {0}, ignoring", name);
                return -1;
            }
            return id;
        }

        public delegate T LoadT<T>(Type type, int id);

        public static IDictionary<String, IList<T>>
        GenerateMoreLikeThis<T>(XmlDocument xml, LoadT<T> loader)
        {
            // get the MoreLikeThis results

            // get the outer list of original results that are parents to
            // the child SimilarResults nodes
            XmlNodeList lsts = xml.SelectNodes("/response/lst[@name=\"moreLikeThis\"]/result");

            // create a datastructure for the overall MoreLikeThis results
            Dictionary<String, IList<T>> similarResults = new Dictionary<String, IList<T>>();
            // for each parent result node
            Type t = null;
            foreach (XmlNode result in lsts)
            {
				String name = result.Attributes["name"].Value;
                List<T> moreLikeThisKids = new List<T>();
                XmlNodeList docs = result.SelectNodes("doc");
                foreach (XmlNode doc in docs)
                {
                    moreLikeThisKids.Add(loader(t, int.Parse(doc.SelectSingleNode("int[@name=\"RecordId\"]").InnerText)));                 
                }
                similarResults[name] = moreLikeThisKids;
            }
            return similarResults;
        }
    }
	
	public class EmergeTkSolrSerializer<T> : ISolrDocumentSerializer<T> where T : AbstractRecord, new()
	{
		public XmlDocument Serialize(T doc, double? boost)
		{
			//REQUIRED FIELDS: RecordDefinition, RecordId, RecordType
			var xml = new XmlDocument();
			XmlNode docNode;
            List<Field> fields = new List<Field>();
			string typename =  doc.GetType().FullName;
			fields.Add( new Field( "RecordDefinition", typename + "." + doc.Id ) );
			fields.Add( new Field( "RecordId", doc.Id ) );
			fields.Add( new Field( "RecordType", typename ) );
			IndexerFactory.Instance.IndexRecord(doc,fields);
			
			DefaultFieldSerializer fieldSerializer = new DefaultFieldSerializer();
			if( fields == null )
			{	
				docNode = xml.CreateComment("ignore-node");
				xml.AppendChild( docNode );
				return xml;
			}
			docNode = xml.CreateElement("doc");
            foreach (var kv in fields) {
                var fieldNode = xml.CreateElement("field");
                var nameAtt = xml.CreateAttribute("name");
                nameAtt.InnerText = kv.Name;
                fieldNode.Attributes.Append(nameAtt);
                if( fieldSerializer != null && kv != null && kv.Value != null )
                {
					foreach( PropertyNode n in fieldSerializer.Serialize( kv.Value ) )
						fieldNode.InnerText = n.FieldValue;	
				}
                
                docNode.AppendChild(fieldNode);
			}
			
            xml.AppendChild(docNode);
			//Console.WriteLine ( xml.OuterXml );
            return xml;
		}
	}	

	public class SolrSearchOptions : ISearchOptions
	{
		SortInfo sort;
        bool random = false;

		public String Type { get; set; }
		public int Start { get; set; }
		public int Rows { get; set; }
		public List<SortInfo> Sorts {get; set;}

        public bool RandomSort
        {
            get
            {
                return random;
            }
            set
            {
                random = value;
            }
        }
		public IFacets Facets { get; set; }
		public IMoreLikeThis MoreLikeThis { get; set; }
        public IDictionary<String, String> ExtraParams { get; set; }
        

		public SolrSearchOptions()
		{
            Facets = null;
            MoreLikeThis = null;
		}
	}

	public class SolrFacets : IFacets
	{
		private List<String> fields;
		public List<String> Fields 
		{ 
			get
			{
				return fields;
			}
			set
			{
				fields = value;
			}
		}
		public int Limit { get; set; }
		public int MinCount { get; set; }

		public SolrFacets()
		{
			fields = new List<String>();
		}
	}

	public class SolrMoreLikeThis : IMoreLikeThis
	{
		private bool boost;
		private List<String> fields;

		public List<String> Fields
		{
			get
			{
				return fields;
			}
			set
			{
				fields = value;
			}
		}

		public int ChildCount { get; set; }
		public int MinDocFreq { get; set; }
		public int MinTermFreq { get; set; }

        public int Start { get; set; }
        public int Rows { get; set; }

		public bool Boost
		{
			get
			{
				return boost;
			}
			set
			{
				boost = value;
			}
		}

		public SolrMoreLikeThis()
		{
			fields = new List<String>();
			boost = true;
		}
	}

	public class SolrSearchProviderQueryResults<T> : ISearchProviderQueryResults<T>
	{
		private Dictionary<String, ICollection<KeyValuePair<String, int>>> facets;
		private IEnumerable<T> results;
        private IDictionary<T, int> moreLikeThisOrder;
		private int numFound = 0;
		
		public IEnumerable<T> Results
		{
			get
			{
				return results;
			}
			internal set
			{
				results = value;
			}
		}

		public IDictionary<String, ICollection<KeyValuePair<String, int>>> Facets
		{
			get
			{
				return facets;
			}
			internal set
			{
				facets = (Dictionary<String, ICollection<KeyValuePair<String, int>>>)value;
			}
		}


        public IDictionary<string, int> FacetQueries { get; set; }

		public int NumFound
		{
			get
			{
				return numFound;
			}
			internal set
			{
				numFound = value;
			}
		}
        public IDictionary<T, int> MoreLikeThisOrder
        {
            internal set
            {
                moreLikeThisOrder = value;
            }
            get
            {
                return moreLikeThisOrder;
            }
        }
	}

}
