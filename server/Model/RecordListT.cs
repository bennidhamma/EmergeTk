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
 * File name: RecordListT.cs
 * Description: A generic friendly wrapper to RecordList.
 *   
 * Author: Ben Joldersma
 *   
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Data;

namespace EmergeTk.Model
{
	/// <summary>
	/// Summary description for RecordList.
	/// </summary>
    public class RecordList<T> : RecordList, IRecordList, IRecordList<T>  where T : AbstractRecord, new()
	{
        public override Type RecordType { get { return typeof(T); } set { throw new System.NotSupportedException("Cannot set type on a generic recordlist.");  } }

        public virtual new T this[int index]
		{
			get
			{
				return base[index] as T;
			}
			set
			{
				base[index] = value;
			}
		}
		
		public RecordList():base()
		{
			
		}
		
		public RecordList(IEnumerable<T> records)
		{
			items = new List<AbstractRecord>(records.Cast<AbstractRecord>());
		}
		
		//This is a 'complete' loading constructor.  it will retain and utilize a loading context to avoid going to cache/db, and it 
		//will attempt to make meaningful sense of the parent object.
		public RecordList(IEnumerable<RecordDefinition> keys, AbstractRecord parent, Dictionary<RecordDefinition,AbstractRecord> loadingContext)
		{
			items = new List<AbstractRecord>();
			this.Parent = parent;
			foreach( RecordDefinition key in keys )
			{
				if( loadingContext.ContainsKey(key) )
				{
					T t = (T)loadingContext[key];
					//since objects may enter the loading context from different parents, we must set the instance parent to null.
					t.Parent = null;
					items.Add( t );
				}
				else
				{
					T t = AbstractRecord.Load<T>(key.Id);
					t.Parent = parent;
					items.Add( t );
					t.LoadingContext = loadingContext;
					loadingContext[key] = t;
				}
			}
			Clean = true;
		}
		
		//this is a 'sufficient' constructor.  it assumes few if any shared objects across the loading context, and simply loads 
		//records from integer ids.
		public RecordList(IEnumerable<int> ids, AbstractRecord parent)
		{
			items = new List<AbstractRecord>();
			this.Parent = parent;
			if( ids != null )
			{
				foreach( int id in ids )
				{
					T t = AbstractRecord.Load<T>(id);
					if( t == null )
						continue;
					t.Parent = parent;
					items.Add( t );
				}
			}
			Clean = true;
		}

        public void Insert(int index, T value)
		{
			base.Insert( index, value );
		}

        public void Remove(T value)
        {
            base.Remove(value);
        }

		public bool Contains(T value)
		{
            return base.Contains(value);
		}

        public int IndexOf(T value)
		{
            return base.IndexOf(value);
		}

        public void Add(T value)
		{
            base.Add(value);
		}

        public T NewRowT()
        {
            return NewRow<T>() as T;
        }

		public new IRecordList<T> Filter()
		{
			return base.Filter() as IRecordList<T>;
		}
		
		public new IRecordList<T> Filter(params FilterInfo[] filters )
		{
			return base.Filter(filters) as IRecordList<T>;
		}
		
		public new IRecordList<T> Filter(Predicate<AbstractRecord> match)
		{
			return base.Filter(match) as IRecordList<T>;
		}

        public new IRecordList<T> Copy()
        {
        	RecordList<T> newList = new RecordList<T>();
        	foreach( T t in this )
        		newList.Add(t);

            newList.RecordSnapshot = this.RecordSnapshot;
        	return newList;
        }
        
        public void ForEach( Action<T> a )
        {
        	base.ForEach(a as Action<AbstractRecord>);
        }
        
      	public T[] ToArrayT()
      	{
			//we need to make a new list and add the values in.
			List<T> values = new List<T>();
			foreach (T t in this)
				values.Add(t);
			return values.ToArray();
      	}
      	
      	IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			for( int i = 0; i < items.Count; i++ )
			{
				if( items[i] != null && items[i].IsStale )
				{
					items[i] = AbstractRecord.Load<T>(items[i].Id);
				}
				yield return items[i] as T;
			}
		}
    }
}
