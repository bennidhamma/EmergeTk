using System;
using System.Collections;
using System.Collections.Generic;

namespace EmergeTk.WebServices
{


	public class MessageList : IList
	{
		/// <summary>
		/// Required for xml.  If the list is the root xml object, then ListName is required.
		/// </summary>
		public string ListName { get; set; }
		
		/// <summary>
		/// The node name of each individual item in the array (required for xml.)
		/// </summary>
		public string ItemName { get; set; }
		ArrayList items;
		
		internal MessageList(IList items)
		{
			this.items = new ArrayList(items);	
		}
		
		public MessageList()
		{
			items = new ArrayList();	
		}

		#region IList implementation
		public int Add (object value)
		{
			return items.Add( value );
		}
		
		public void Clear ()
		{
			items.Clear();
		}
		
		public bool Contains (object value)
		{
			return items.Contains(value);
		}
		
		public int IndexOf (object value)
		{
			return items.IndexOf(value);
		}
		
		public void Insert (int index, object value)
		{
			items.Insert(index, value);
		}
		
		public void Remove (object value)
		{
			items.Remove(value);
		}
		
		public void RemoveAt (int index)
		{
			items.RemoveAt(index);
		}

		
		public bool IsFixedSize {
			get {
				return items.IsFixedSize;
			}
		}
		
		public bool IsReadOnly {
			get {
				return items.IsReadOnly;
			}
		}
		
		public object this[int index] {
			get {
				return items[index];
			}
			set {
				items[index] = value;
			}
		}
		#endregion

		#region ICollection implementation
		public void CopyTo (Array array, int index)
		{
			items.CopyTo(array, index);
		}

		
		public int Count {
			get {
				return items.Count;
			}
		}
		
		public bool IsSynchronized {
			get {
				return items.IsSynchronized;
			}
		}
		
		public object SyncRoot {
			get {
				return items.SyncRoot;
			}
		}
		#endregion

		#region IEnumerable implementation
		public IEnumerator GetEnumerator ()
		{
			return items.GetEnumerator();
		}
		#endregion		
		
		public static MessageList ConvertFromRaw(IList input)
		{
			MessageList list = new MessageList();
			foreach( object o in input )
			{
				Dictionary<string,object> d = o as Dictionary<string,object>;
				if( d != null )
				{
					list.Add( MessageNode.ConvertFromRaw(d) );
					continue;
				}
				IList l = o as IList;
				if( l != null )
				{
					list.Add( MessageList.ConvertFromRaw( l ) );
					continue;
				}
				list.Add(o);
			}
			
			return list;
		}
	}
}
