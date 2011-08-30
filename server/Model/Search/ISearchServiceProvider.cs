using System;
using System.Collections.Generic;
using System.Xml;

namespace EmergeTk.Model.Search
{
    public delegate void SearchSerializerTestHook(XmlDocument doc);

    public interface IMoreLikeThis
    {
        List<String> Fields { get; set; }
        int ChildCount { get; set; }  // number of child MLT docs per parent result doc.
        int MinDocFreq { get; set; }
        int MinTermFreq { get; set; }
        bool Boost { get; set; }
        int Start { get; set; }
        int Rows { get; set; }
    }

    public interface IFacets
    {
        List<String> Fields { get; set; }
        int Limit { get; set; }
        int MinCount { get; set; }
    }

    public interface ISearchOptions
    {
        String Type { get; set; }   // leave empty for heterogenous search results - used with SearchInt
        int Start { get; set; }
        int Rows { get; set; }
        List<SortInfo> Sorts { get; set; }
        bool RandomSort { get; set; }  // add an additional random sort on the end? 
        IFacets Facets { get; set;  }
        IMoreLikeThis MoreLikeThis { get; set; }
        IDictionary<String, String> ExtraParams {get; set;}
    }

    public interface ISearchProviderQueryResults<T>
    {
        int NumFound { get; }
        IEnumerable<T> Results { get; }
        IDictionary<T, int> MoreLikeThisOrder { get; }
        IDictionary<String, ICollection<KeyValuePair<String, int>>> Facets { get; }
        IDictionary<String, int> FacetQueries { get; }
    }    

	public interface ISearchServiceProvider
	{
		bool CommitEnabled
		{
			get;
			set;
		}

        SearchSerializerTestHook TestHook
        {
            set;
        }

		ISearchFilterFormatter GetFilterFormatter();
		void GenerateIndex(IRecordList elements );
		void GenerateIndex(AbstractRecord r );
		void Delete( AbstractRecord r );
		void Delete( IRecordList elements );
		void DeleteAllOfType<T>();
		void DeleteAll();
		void Commit();
		void Optimize();
		IRecordList Search( string field, string key );
		IRecordList Search( string query );
		IRecordList<T> Search<T>( string field, string key, List<string> types )  where T : AbstractRecord, new();
		IRecordList<T> Search<T>( string field, string key ) where T : AbstractRecord, new();
		IRecordList<T> Search<T>( string query ) where T : AbstractRecord, new();
        IRecordList<T> Search<T>(string query, SortInfo sort, int start, int count, out int numFound) where T : AbstractRecord, new();
        List<RecordKey> SearchInt(string field, string query, string type, SortInfo[] sorts, int start, int count, out int numFound);
        List<RecordKey> SearchInt(string field, FilterSet mainQuery, FilterSet cachedQueries, string type, SortInfo[] sorts, int start, int count, out int numFound);
        ISearchProviderQueryResults<T> Search<T>(String mainQuery, FilterSet cachedQueries, ISearchOptions options) where T : AbstractRecord, new();
        ISearchProviderQueryResults<RecordKey> SearchInt(String mainQuery, FilterSet cachedQueries, ISearchOptions options);
        ISearchOptions GenerateOptionsObject();
        IFacets GenerateFacetsObject();
        IMoreLikeThis GenerateMoreLikeThisObject();
	}
}

