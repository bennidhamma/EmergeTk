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
 * File name: SortInfo.cs
 * Description: defines a sorting rule for IRecordLists.
 *   
 * Author: Ben Joldersma
 *   
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model
{
    public enum SortDirection
    {
        Ascending = 1,
        Descending = -1
    }
    public class SortInfo : IQueryInfo
    {
        public string ColumnName;
        public SortDirection Direction;

		public SortInfo(){}
        public SortInfo(string columnName) { this.ColumnName = columnName; Direction = SortDirection.Ascending; }
        public SortInfo(string columnName, SortDirection direction)
        {
            ColumnName = columnName;
            Direction = direction;
        }

        override public string ToString()
        {
        	if( ColumnName.Contains(".") )
        		return string.Empty;
            return ColumnName + (Direction == SortDirection.Ascending ? string.Empty : " DESC");
        }
    }
}
