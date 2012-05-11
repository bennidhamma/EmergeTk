/** Copyright (c) 2006, All-In-One Creations, Ltd.
*  All rights reserved.
* 
* Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
* 
*     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
*     * Neither the name of All-In-One Creations, Ltd. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
/**
 * Project: emergetk: stateful web framework for the masses
 * File name: RecordList.cs
 * Description: non-generic default implentation of IRecordList.  Should cover most cases, but you will want to
 * write a custom implementation of IRecordList and IRecordListT for when you must be sensitive to storage allocations,
 * are pulling data from forward-only cursors, etc.
 *   
 * Author: Ben Joldersma
 *   
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Reflection;
using System.Data;

namespace EmergeTk.Model
{
    //TODO: we should implement IList here.
    public class RecordList : IRecordList, IComparable, IJSONSerializable, IEnumerable
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(RecordList));		
		
		protected List<AbstractRecord> items, originalItems;
		protected List<int> recordSnapshot;
		AbstractRecord parent;
		
		bool preserve = true;
		bool clean;

        private Type recordType;

        public virtual Type RecordType
        {
            get { return recordType; }
            set { recordType = value; }
        }	

		public RecordList()
		{
            items = new List<AbstractRecord>();
		}
		
		public RecordList( List<AbstractRecord> items )
		{
			this.items = items;
		}
		
		public List<int> RecordSnapshot 
		{
			get
			{
				if (recordSnapshot == null)
					recordSnapshot = new List<int>();
				return recordSnapshot;	
			}
			set {
				//log.Debug("setting snapshot", value);
				recordSnapshot = value;	
			}
		}
		
		#region IList Members

		public bool IsReadOnly
		{
			get
			{
				// TODO:  Add RecordList.IsReadOnly getter implementation
				return false;
			}
		}

        public virtual AbstractRecord this[int index]
		{
			get
			{
				AbstractRecord r = items[index];
				if( r != null && r.IsStale )
				{
					items[index] = AbstractRecord.Load(r.GetType(), r.Id);
				}
				return items[index] as AbstractRecord;
			}
			set
			{
				items[index] = value;
			}
		}

		public void RemoveAt(int index)
		{
			clean = false;
            items.RemoveAt(index);
		}

        public void Insert(int index, AbstractRecord value)
		{
			items.Insert( index, value );
			if( parent != null )
				value.Parent = parent;
		}

        private List<SortInfo> sorts;

        public List<SortInfo> Sorts
        {
            get { if( sorts == null ) sorts = new List<SortInfo>(); return sorts; }
            set { sorts = value; }
        }

        private List<FilterInfo> filters;
        public List<FilterInfo> Filters
        {
            get { if (filters == null) filters = new List<FilterInfo>(); return filters; }
            set { filters = value; }
        }

        public void Remove(AbstractRecord value)
		{
			clean = false;
			items.Remove( value );
		}
		
		public void RemoveRange(int index, int count)
		{
			for(int i = index; count > 0; count -- )
			{
				RemoveAt(i);
			}
		}
		
		//note: this method does not fire removal events.
		public void RemoveAll (Predicate<AbstractRecord> match)
		{
			clean = false;
			items.RemoveAll (match);
		}

        public void Delete(AbstractRecord value)
        {
            Remove(value);
            value.Delete();
        }

		public bool Contains(AbstractRecord value)
		{
			return items.Contains( value );
		}

		public void Clear()
		{
			clean = false;
            items.Clear();
		}

        public int IndexOf(AbstractRecord value)
		{
			return items.IndexOf( value );
		}

        public void Add(AbstractRecord value)
		{
			if (value == null)
			{
                throw new InvalidOperationException ("Trying to add NULL value into a record list. Invalid.");
			}
			clean = false;
			items.Add( value );
			if( parent != null )
				value.Parent = parent;
		}

        public AbstractRecord NewRow<T>() where T : AbstractRecord, new()
        {
            AbstractRecord n = new T();
            Add(n);
            return n;
        }

		public virtual int Count
		{
			get
			{
				return items.Count;
			}
			set
			{
				throw new ReadOnlyException("Count");
			}
		}

		public virtual bool Preserve {
			get {
				return preserve;
			}
			set {
				preserve = value;
			}
		}

		public AbstractRecord Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		public bool IsDeserializing {
			get {
				return isDeserializing;
			}
			set {
				isDeserializing = value;
			}
		}

		public bool Clean {
			get {
				return clean;
			}
			set {
				clean = value;
			}
		}

		#endregion

        public void Sort()
        {
            if (sorts != null)
            {
                items.Sort( new RecordComparer<AbstractRecord>(sorts) );
            }
        }

		public void Randomize()
		{
			Random rnd = new Random();
			Sort( delegate( AbstractRecord x, AbstractRecord y ) {
					return rnd.Next(-5,5);
				});
		}
		
        public void Sort( Comparison<AbstractRecord> comparison )
        {
        	items.Sort( comparison );
        }
        
        public void Sort( params SortInfo[] sorts )
        {
        	Sorts.Clear();
        	Sorts.AddRange(sorts);
        	Sort();
        }

        public void AddFilter(FilterInfo filterInfo)
        {
            if (filters == null) filters = new List<FilterInfo>();
            if( ! filters.Contains( filterInfo ) )
            	filters.Add(filterInfo);
        }

		///this logic probably needs to be revisited.  We should probably treat the current list as immutable,
		///and return a new, filtered sublist.  Then the user has the option of copying or preserving themselves.
        public IRecordList Filter()
        {
        	return Filter( delegate(AbstractRecord elem)
        		{  
        			foreach (FilterInfo fi in filters)
                        if (!FilterInfo.Filter(fi.Operation, elem[fi.ColumnName], fi.Value))
                        	return false;
                     return true;
                }
            );            
        }
        
        public IRecordList Filter( Predicate<AbstractRecord> match )
        {
        	if( originalItems == null )
        		originalItems = items;
        	List<AbstractRecord> newItems = items.FindAll(match);
        	if( ! preserve )
            {
            	items = newItems;
            	return this;
            }
            else
            {
            	//TODO: this is probably wrong.  we need to reset the filters and sorts at a minimum as well.
            	RecordList rl = this.MemberwiseClone() as RecordList;
            	rl.items = newItems;
            	rl.originalItems = null;
            	return rl; 
            }
        }
        
        public IRecordList Filter(params FilterInfo[] filters)
        {
        	this.filters = new List<FilterInfo>(filters);
        	return Filter();
        }
       
        public string[] ToStringArray()
        {
            List<string> strings = new List<string>(this.Count);
            foreach (AbstractRecord r in this)
            {
                strings.Add(r.ToString());
            }
            return strings.ToArray();
        }

        public string[] ToStringArray(string property)
        {
            List<string> strings = new List<string>(this.Count);
            foreach (AbstractRecord r in this)
            {
                strings.Add(r[property].ToString());
            }
            return strings.ToArray();
        }
        
        public U[] GetVector<U>(string property )
        {
        	List<U> us = new List<U>(this.Count);
        	foreach (AbstractRecord r in this)
            {
                us.Add((U)r[property]);
            }
            return us.ToArray();
        }

        public string[] ToIdArray()
        {
            List<string> ids = new List<string>(this.Count);
            foreach (AbstractRecord r in this)
            {
                ids.Add(r.Id.ToString());
            }
            return ids.ToArray();
        }

		public void Save()
		{
            items.ForEach(new Action<AbstractRecord>(delegate(AbstractRecord r) { r.Save(); }));
		}

        public void ForEach(Action<AbstractRecord> action)
        {
            items.ForEach(action);
        }

		public void Delete()
		{
			clean = false;
			List<AbstractRecord> recordsToDelete = new List<AbstractRecord>(items);
			foreach (AbstractRecord record in recordsToDelete)
				record.Delete();			
            Clear();
		}

        public void DeleteAt(int index)
        {
            Delete(this[index]);
        }

        bool isDeserializing = false;
        
        public Dictionary<string,object> Serialize()
        {
        	Dictionary<string,object> h = new Dictionary<string,object>();
        	//TODO: add other props - Parent, handlers, etc.
        	h["_type"] = this.GetType().FullName;
        	h["_items"] = this.items;
        	return h;
        }
        
        public void Deserialize(Dictionary<string,object> h)
        {
        	if( h.ContainsKey( "_items" ) )
        	{
        		IList list = h["_items"] as IList;
        		foreach( AbstractRecord r in list )
        		{
        			Add( r );
        		}
        	}
        }
        
        #region IComparable Members

        public int CompareTo(object obj)
        {
            IRecordList other = obj as IRecordList;
            if (other != null)
            {
                int myWeight = 0, otherWeight = 0;
                foreach (AbstractRecord r in this)
                {
                    myWeight += r.ComputeRecordWeight();
                }
                foreach (AbstractRecord r in other)
                {
                    otherWeight += r.ComputeRecordWeight();
                }
                return myWeight - otherWeight;
            }
            return 0;
        }

		public bool TestAny( IRecordList irl )
		{
			if( irl == null || irl.Count == 0 )
				return false;
			foreach( AbstractRecord r in items )
			{
				if( irl.Contains(r) )
				   return true;
			}
			return false;
		}

        #endregion
        
		public AbstractRecord[] ToArray()
		{
			return items.ToArray();
		}
		
		public void Debug()
		{
			foreach( object o in items )
			{
				log.Debug(o);
			}
		}
		
		public IRecordList Copy()
		{
			RecordList r = new RecordList();
			foreach( AbstractRecord o in items )
			{
				r.Add( o );
			}
			return r;
		}
		
		public IEnumerator GetEnumerator ()
		{
			foreach (var rec in items)
			{
				var r = rec;
				if( rec != null && rec.IsStale )
				{
					var index = this.IndexOf (rec);
					r = items[index] = AbstractRecord.Load(rec.GetType(), rec.Id);
				}
				if (r != null)
					yield return r;
			}
		}
		
		public virtual IEnumerable<AbstractRecord> GetEnumerable()
		{
			foreach (var rec in this)
				yield return rec as AbstractRecord;
		}
    }
}
