
using System;
using System.Collections.Generic;

namespace EmergeTk.Model
{
	public delegate T[] FetchPage<T>( PagingRecordList<T> list, int PageNumber ) where T : AbstractRecord, new();
	
	public class PagingRecordList<T> : RecordList<T>, IRecordList where T : AbstractRecord, new()
	{	
		protected new static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(PagingRecordList<T>));		
		
		public int PageSize { get; set; }
		
		int count;
		public override int Count {
			get {
				return count;
			}
			set 
			{
				count = value;
			}
		}

		
		Dictionary<int, T> values = new Dictionary<int, T>();
		
		public FetchPage<T> FetchPageHandler;
		
		public PagingRecordList(int pageSize, FetchPage<T> fetchPageHandler )
		{
			this.FetchPageHandler = fetchPageHandler;
			this.PageSize = pageSize;
		}
		
		private int GetPageNumberOfIndex( int index )
		{
			return index / PageSize;	
		}
		
		public void SetPage( int start, params T[] newItems )
		{
			for( int  i = 0; i < newItems.Length; i++ )
				values[ start + i ] = newItems[ i ];
		}
		
		public override T this[int index] {
			get {
				if( ! values.ContainsKey( index ) )
				{
					int page = GetPageNumberOfIndex( index );
					SetPage( page * PageSize, FetchPageHandler( this, page ) );
				}
				
				if( values.ContainsKey( index ) )
				{ 
					return values[ index ];
				}
				else
				{
					//throw new ArgumentOutOfRangeException(string.Format("Index {0} invalid in pagerecordlist with {1} items.", index, Count));
					log.ErrorFormat("Index {0} invalid in pagerecordlist with {1} items.", index, Count);
					return null;
				}
				
			}
			set {
				values[index] = value;
			}
		}

		AbstractRecord IRecordList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				this[index] = (T)value;
			}
		}
		
		//HACK: use for loop maybe instead in record serializer?
		public override IEnumerable<AbstractRecord> GetEnumerable()
		{
			foreach( AbstractRecord t in values.Values )
				yield return t;
		}
		
		Type IRecordList.RecordType { get { return typeof(T); } set { throw new System.NotSupportedException("Cannot set type on a generic recordlist.");  } }
	}
}
