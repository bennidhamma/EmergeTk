//  IndexManager.cs created with MonoDevelop
// User: ben at 10:29 PM 7/3/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using EmergeTk;
using Mono.Addins;
using Mono.Addins.Description;
using LuceneField = Lucene.Net.Documents.Field;

namespace EmergeTk.Model.Search
{
	public class Field
	{
		private bool stored = false;
		private bool indexed = true;
		private bool tokenized = true;
		private bool omitNorms = false;
		
		public string Name { get; set; }
		public object Value { get; set; }
		public bool Stored { get { return stored; } set {stored = value; } }
		public bool Indexed { get { return indexed; } set {indexed = value; } }	
		public bool Tokenized { get { return tokenized; } set {tokenized = value; } }
		public bool OmitNorms { get { return omitNorms; } set {omitNorms = value; } }
		
		public Field()
		{
			Stored = false;
			Indexed = true;
			Tokenized = true;
		}
		
		public Field( string Name, object Value )
		{
			this.Name = Name;
			this.Value = Value;
		}
	}

	public class LuceneSearchServiceProvider : ISearchServiceProvider
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(LuceneSearchServiceProvider));
		
		string directory;

		public bool CommitEnabled
		{
			get 
			{ 
				throw new NotImplementedException(); 
			}
			set
			{
				throw new NotImplementedException(); 
			}
		}

        public SearchSerializerTestHook TestHook
        {
            set
            {
                throw new NotImplementedException();
            }
        }

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public void Optimize()
		{
			throw new NotImplementedException();
		}
		
		internal LuceneSearchServiceProvider()
		{
			try
			{
				directory = Setting.GetValueT<string>("IndicesPath");
				if( string.IsNullOrEmpty(directory) )
					directory = System.Web.HttpContext.Current.Server.MapPath("/Indices");				
				System.IO.Directory.CreateDirectory(directory);
			}
			catch( Exception e )
			{
				log.Error("Error creating index manager", e );
				//we may be a test or something.  make a default directory.
				directory = Path.Combine(Path.GetTempPath(),"chaostest-indices");
				System.IO.Directory.CreateDirectory(directory);
			}
		}
		
		private IndexWriter GetWriter()
        {
            try
            {
                IndexWriter writer = new IndexWriter(directory, new StandardAnalyzer(), false);
                return writer;
            }
            catch
            {
                IndexWriter writer = new IndexWriter(directory, new StandardAnalyzer(), true);
                return writer;
            }
        }

		#region ISearchServiceProvider implementation
		public void GenerateIndex (IRecordList elements)
		{
			foreach( AbstractRecord r in elements )
			{
				GenerateIndex(r);
			}
		}
		
		public void GenerateIndex (AbstractRecord r)
		{
			GetWriter();
			Delete( r );		
			IndexWriter writer = GetWriter();
			List<Field> fields = new List<Field>();
			IndexerFactory.Instance.IndexRecord(r, fields);
			writer.AddDocument( createDocument(r, fields ) );
			writer.Optimize();
			writer.Close();
			#if DEBUG
			  log.Debug( "GENERATED INDEX DOCUMENT.", r ); 
            #endif
		}
		
		private Document createDocument(AbstractRecord r, List<Field> fields)
		{
			Document indexDocument = new Document();
			indexDocument.Add( new LuceneField( "_type", r.GetType().FullName, LuceneField.Store.YES, LuceneField.Index.TOKENIZED  ) );
			indexDocument.Add( new LuceneField( "_id", r.Id.ToString(), LuceneField.Store.YES, LuceneField.Index.TOKENIZED  ) );

			foreach( Field f in fields )
			{
				if( f != null && f.Value != null )
					indexDocument.Add( ToLuceneField( f ) );
			}
			return indexDocument;
		}
		
		private LuceneField ToLuceneField( Field field )
		{
			if( field == null || field.Value == null )
				return null;
			//this is an imperfect map.  Lucene does not give us very C# friendly objects.
			//basically we distill the OmitNorms, Tokenize and Indexed bools in our Field class to their
			//last Index parameter.
			LuceneField lfield = new LuceneField
				(field.Name, field.Value.ToString(), field.Stored ? LuceneField.Store.YES : LuceneField.Store.NO,
					field.Tokenized ? LuceneField.Index.TOKENIZED : field.Indexed ? LuceneField.Index.UN_TOKENIZED :
				  	field.OmitNorms ? LuceneField.Index.NO_NORMS : LuceneField.Index.NO );
			return lfield;
			
		}
		
		public void Delete (AbstractRecord r)
		{
			IndexReader reader = IndexReader.Open( directory );
			IndexSearcher searcher = new IndexSearcher( directory );
			QueryParser qp = new QueryParser("_type", new StandardAnalyzer() );
			string key = string.Format( "_type:{0} AND _id:{1}", r.GetType(), r.Id );
			#if DEBUG
				log.Debug( "SEARCHING TO DELETE", key ); 
			#endif
			Query q = qp.Parse(key );
			
			
			Hits hits = searcher.Search( q );
			IEnumerator ie = hits.Iterator();
			
			#if DEBUG
				log.Debug( "HIT COUNT", hits.Length() ); 
			#endif
			
			int i = 0;
			while( ie.MoveNext() )
			{
				
				#if DEBUG
					log.Debug( "FOUND OLD RECORD.  DELETING INDEX DOCUMENT." ); 
				#endif
				Hit hit = (Hit)ie.Current;
				log.Debug("Hit String: " + hit.ToString());
				log.Debug("Hit Type: " + hit.GetType());
				log.Debug("Hit Document: " + hit.GetDocument());
				log.Debug("Hit Document.Fields: " + hit.GetDocument().GetField("Body"));
				log.Debug("Hit _type: " + hit.GetDocument().GetField("_type"));
				log.Debug("Hit _id: " + hit.GetDocument().GetField("_id"));
				
				reader.DeleteDocument( hits.Id( i++ ) );
			}
			reader.Close();
		}
		
		public void Delete( IRecordList elements )
		{
			foreach( AbstractRecord r in elements )
				Delete( r );
		}
		
		public IRecordList Search (string field, string key)
		{
			if( ! Directory.Exists( directory ) || Directory.GetFiles(directory).Length == 0 )
			{
				log.Warn("No indices created on this machine.", directory );
				return new RecordList();
			}
			IndexSearcher searcher = new IndexSearcher( directory );
			QueryParser qp = new QueryParser(field, new StandardAnalyzer() );
			
			Query q = qp.Parse( key );
			
			Hits hits = searcher.Search( q );
			IEnumerator ie = hits.Iterator();
			RecordList results = new RecordList();
			while( ie.MoveNext() )
			{
				Hit hit = (Hit)ie.Current;
				string type = hit.Get("_type");
				
				string id = hit.Get("_id");
				AbstractRecord r = AbstractRecord.Load( TypeLoader.GetType(type), id );
				if( r != null )
					results.Add( r );
			}
			return results;
			//Query query = QueryParser.Parse(, "text", new StandardAnalyzer());
		}
		
		public IRecordList Search( string query )
		{
			throw new NotImplementedException();		
		}
		
		public void DeleteAll()
		{
			throw new NotImplementedException();
		}
		
		public void DeleteAllOfType<T>()
		{
			throw new NotImplementedException();
		}
		
		public IRecordList<T> Search<T> (string field, string key, List<string> types ) where T : AbstractRecord, new()		
		{
			log.Debug("searching ", field, key );
			if( ! Directory.Exists( directory ) || Directory.GetFiles(directory).Length == 0 )
			{
				log.Warn("No indices created on this machine.", directory );
				return new RecordList<T>();
			}
			IndexSearcher searcher = new IndexSearcher( directory );
			QueryParser qp = new QueryParser(field, new StandardAnalyzer() );
			
			Query q = qp.Parse( key );
			
			Hits hits = searcher.Search( q );
			IEnumerator ie = hits.Iterator();
			IRecordList<T> results = new RecordList<T>();
			while( ie.MoveNext() )
			{
				log.Debug( "found a hit" ); 
				Hit hit = (Hit)ie.Current;
				string type = hit.Get("_type");
				
				#if DEBUG
					log.Debug( "hit found of type ", type ); 
				#endif
				
				if( types.Contains( type ) )
				{
					Type testType = TypeLoader.GetType(type);
				
					string id = hit.Get("_id");
					AbstractRecord r = AbstractRecord.Load(testType, id );
					if( r != null )
						results.Add( r );
				}
			}
			return results;
		}
		
		public IRecordList<T> Search<T> (string field, string key ) where T : AbstractRecord, new()
		{
			IRecordList<T> results = new RecordList<T>();	
			int numFound = -1;
			List<RecordKey> ids = SearchInt( field, key, typeof(T).FullName , null, -1, -1, out numFound );
			foreach( RecordKey rk in ids )
			{
					AbstractRecord r = AbstractRecord.Load<T>( rk.Id );
					if( r != null )
						results.Add( r );
			}
			return results;
			//Query query = QueryParser.Parse(, "text", new StandardAnalyzer());
		}
		
		public IRecordList<T> Search<T> (string query) where T : AbstractRecord, new()
		{
			log.Debug("Lucene searching query", query );
			return Search<T>(null, query);
		}
		
		public IRecordList<T> Search<T>( string query, SortInfo sort, int start, int count, out int numFound ) where T : AbstractRecord, new()
		{
			throw new NotImplementedException();
		}

        public List<RecordKey> SearchInt(string field, FilterSet mainQuery, FilterSet cachedQuery, string type, SortInfo[] sorts, int start, int count, out int numFound)
        {
            throw new NotImplementedException();
        }
		
		public List<RecordKey> SearchInt(string field, string key, string typeName, SortInfo[] sorts, int start, int count, out int numFound)
		{
			numFound = 0;
			log.Debug("searching ", field, key );
			if( ! Directory.Exists( directory ) || Directory.GetFiles(directory).Length == 0 )
			{
				log.Warn("No indices created on this machine.", directory );
				return new List<RecordKey>();
			}
			IndexSearcher searcher = new IndexSearcher( directory );
			QueryParser qp = new QueryParser(field, new StandardAnalyzer() );
			
			if( typeName != null )
				key += "AND _type:" + typeName;
			
			Query q = qp.Parse( key );
			
			Hits hits = searcher.Search( q );
			log.Debug("hit length: ", hits.Length() );
			IEnumerator ie = hits.Iterator();
			List<RecordKey> results = new List<RecordKey>();
			while( ie.MoveNext() )
			{
			//	log.Debug( "found a hit" ); 
				
				Hit hit = (Hit)ie.Current;
				string type = hit.Get("_type");
				int id = int.Parse(hit.Get("_id"));
				RecordKey rk = new RecordKey();
				rk.Id = id;
				rk.Type = type;
				//log.Debug( "hit found of type ", type, id ); 
				
				results.Add( rk );
			}
			numFound = results.Count;
			return results;
		}
		
		public ISearchFilterFormatter GetFilterFormatter()
		{
			return new LuceneFilterFormatter();
		}

        public ISearchProviderQueryResults<T> Search<T>(String mainQuery, FilterSet cachedQueries, ISearchOptions options) where T : AbstractRecord, new()
        {
            throw new NotImplementedException();
        }

        public ISearchProviderQueryResults<RecordKey> SearchInt(String mainQuery, FilterSet cachedQueries, ISearchOptions options)
        {
            throw new NotImplementedException();
        }

        public ISearchOptions GenerateOptionsObject() { throw new NotImplementedException(); }
        public IFacets GenerateFacetsObject() { throw new NotImplementedException(); }
        public IMoreLikeThis GenerateMoreLikeThisObject() { throw new NotImplementedException(); }

		#endregion
		
		
	}
	
	public class IndexManager
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(IndexManager));
		
		static private ISearchServiceProvider instance;
		static public ISearchServiceProvider Instance
		{
			get
			{
				//log.Debug("getting instance", instance, AddinHost.Running);
				if( instance == null )
				{
					start();
				}
				return instance;
			}
			set
			{
				instance = value;	
			}
		}

		/*
		static void debug()
		{
			log.Info("debugging addins " + AppDomain.CurrentDomain.Id );

			
			//AddinManager.LoadAddin(null, "EmergeTk");
			//AddinManager.LoadAddin(null, "MemCached");
			
			log.Info("addin EmergeTk loaded?" + AddinManager.IsAddinLoaded("EmergeTk") );
			log.Info("addin SolrSearchProvider loaded?" + AddinManager.IsAddinLoaded("SolrSearchProvider") );

				foreach( Addin a in AddinManager.Registry.GetAddinRoots() )
				{
					log.Debug("addin root ", a, a.Enabled );
					foreach( ExtensionPoint e in a.Description.ExtensionPoints )
					{
						log.DebugFormat("extension point {0} {1} {2}", e, e.Name, e.Path );
						foreach( ExtensionNodeType n in e.NodeSet.NodeTypes )
						{
							log.DebugFormat( "ext node {0} {1}", n.Id, n.NodeName );
						}
					}
				}
	
				foreach( Addin a in AddinManager.Registry.GetAddins() )
				{
					log.Debug("addin" + a );
					
					foreach( ModuleDescription md in a.Description.AllModules )
					{
						//ExtensionNodeDescription end = md.Extensions[0].ExtensionNodes[0];
						//log.Debug("module", end.NodeName, end.Id, end.GetNodeType().TypeName);
						
						foreach( Extension e in md.Extensions )
						{
							log.Debug("extension: " + e.Path );
							foreach( ExtensionNodeDescription end in e.ExtensionNodes )
							{
								ExtensionNodeType ent = end.GetNodeType();
								
								log.Debug("ext node:" + end.NodeName );
								if( ent != null )
								{
									log.DebugFormat("e node type {0} {1} {2} {3}",
									          ent.ObjectTypeName, 
									          ent.Description, 
									          ent.TypeName,
									          ent.NodeTypes );
								}
							}
						}	
					}
				}
		}
		*/
		
		static void start()
		{
			log.Debug("Addin host running? ", AddinHost.Running );
			string path = "/EmergeTk/Model/SearchServiceProvider";
			
			
			
			if( AddinHost.Running )
			{
				try
				{
					log.Debug("looking for extensions to ISearchServiceProvider");
					foreach (ISearchServiceProvider c in AddinManager.GetExtensionObjects ( path ))
					{
						instance = c;
						log.Debug("Using ISearchServiceProvider: ", instance );
					}
	
					log.Debug("looking for extensions to " + path);
	
					foreach( ExtensionNode node in AddinManager.GetExtensionNodes(path) )
					{
						log.Debug("found extensions for ", node );
					}

					AddinManager.AddExtensionNodeHandler(path, OnExtensionChanged);
				}
				catch(Exception e )
				{
					log.Error("Error loading ISearchServiceProvider addins", e );
				}
			}
			else
			{
				AddinHost.OnAddinStart += delegate {
					instance = null;	
				};
			}
			
			if( instance == null )
			{
				instance = new LuceneSearchServiceProvider();
				log.Debug("!!!!!!!!!!!!!!!!!!!!!!!!USING LUCENE!!!!!!!!!!!!!!!!!!!!!!!!!!");
			}
			
			log.Debug("Using ISearchServiceProvider: ", instance, AddinHost.Running );
		}
		
		static void OnExtensionChanged( object sender, ExtensionNodeEventArgs args )
		{
			log.Debug("extension changed", args.Change, args.Path, args.ExtensionNode.Id );
			if( args.Change == ExtensionChange.Remove )
			{
				instance = null;
			}
			else
			{
				TypeExtensionNode node = args.ExtensionNode as TypeExtensionNode;
				object i = node.CreateInstance();
				Type icp1 = i.GetType().GetInterface("ISearchServiceProvider");
				Type icp2 = typeof(ISearchServiceProvider);
				log.Debug("adding from node ", 
				          node, 
				          i, 
				          icp1 == icp2,
				          icp1.AssemblyQualifiedName,
				          icp2.AssemblyQualifiedName);
				instance = (ISearchServiceProvider)node.CreateInstance();
			}

			log.Debug("Using ISearchServiceProvider: ", instance );
		}
	}
}
