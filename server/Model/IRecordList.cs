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
 * File name: IRecordList.cs
 * Description: non-generic record list interface.
 *   
 * Author: Ben Joldersma
 *   
 */

using System;
using System.Collections;
using System.Collections.Generic;
namespace EmergeTk.Model
{
    public interface IRecordList : IComparable, IJSONSerializable, IEnumerable
    {
        //properties
        int Count { get; }
        List<SortInfo> Sorts { get; set; }
        List<FilterInfo> Filters { get; set; }
        AbstractRecord this[int index] { get; set; }
        bool Live { get; set; }
        Type RecordType { get; set; }
        bool Preserve { get; set; }
		AbstractRecord Parent { get; set; }
		List<int> RecordSnapshot { get; set;	}
        
        //methods
        void Add(AbstractRecord value);
        void AddFilter(FilterInfo filterInfo);
        void Clear();
        bool Contains(AbstractRecord value);
        void Delete();
        void Delete(AbstractRecord value);
        void DeleteAt(int index);
        IRecordList Filter();
        IRecordList Filter(Predicate<AbstractRecord> record);
        IRecordList Filter(params FilterInfo[] filters);
       	//IEnumerator GetEnumerator();
		IEnumerable<AbstractRecord> GetEnumerable();
        int IndexOf(AbstractRecord value);
        void Insert(int index, AbstractRecord value);
        bool IsReadOnly { get; }
        AbstractRecord NewRow<T>() where T : AbstractRecord, new();
        void Remove(AbstractRecord value);
        void RemoveAt(int index);
		void RemoveRange( int index, int count );
        void Save();
        void Sort();
		void Randomize();
        void Sort(params SortInfo[] sorts);
        void Sort( Comparison<AbstractRecord> comparison );
        void ForEach( Action<AbstractRecord> action );
        string[] ToIdArray();
        U[] GetVector<U>(string column);
        new Dictionary<string,object> Serialize();
        new void Deserialize(Dictionary<string,object> h);
        string[] ToStringArray();
        string[] ToStringArray(string property);
        AbstractRecord[] ToArray();
        IRecordList Copy();
		bool TestAny( IRecordList irl );
		bool Clean { get; set; }
		
        //events
        event EventHandler<RecordEventArgs> OnRecordAdded;
        event EventHandler<RecordEventArgs> OnRecordChanged;
        event EventHandler<RecordEventArgs> OnRecordRemoved;
    }
}
