using System;
using System.Collections.Generic;

namespace EmergeTk.Model.Search
{
	public interface IIndexer<T> where T : AbstractRecord, new()
	{
		void Index(T record, List<Field> fields);
	}
	
	public class IndexableAttribute : Attribute
	{
		public Type Indexer { get; set; }
		
		public IndexableAttribute(Type indexer)
		{
			this.Indexer = indexer;
		}
	}
	
	public class IndexerFactory
	{
		public static IndexerFactory Instance = new IndexerFactory();
		
		private Dictionary<Type,object> indexerMap = new Dictionary<Type, object>();
		
		private IndexerFactory()
		{
			Attribute[] attributes;
			var types = TypeLoader.GetTypesWithAttribute(typeof(IndexableAttribute), false, out attributes );
			for( int i = 0; i < types.Length; i++ )
			{
				Type recordType = types[i];
				IndexableAttribute att = (IndexableAttribute)attributes[i];
				indexerMap[recordType] = Activator.CreateInstance(att.Indexer);
			}
		}
		
		public IIndexer<T> GetIndexer<T>() where T : AbstractRecord, new()
		{
			return indexerMap[typeof(T)] as IIndexer<T>;
		}
		
		public void IndexRecord(object record, List<Field> fields)
		{
			TypeLoader.InvokeGenericMethod(this.GetType(),"IndexRecordT", new Type[]{record.GetType()}, this, new object[]{record, fields});
		}
		
		private void IndexRecordT<T>(T record, List<Field> fields) where T : AbstractRecord, new()
		{
			GetIndexer<T>().Index(record, fields);
		}
	}
}

