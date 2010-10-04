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
 * File name: FilterInfo.cs
 * Description: filter defininition for RecordLists.
 *   
 * Author: Ben Joldersma
 *   
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model
{
    public enum FilterOperation
    {
        GreaterThan,
		GreaterThanOrEqual,
        LessThan,
		LessThanOrEqual,
        Equals,
        DoesNotEqual,
        In,
        NotIn,
        Contains,
        NotContains
    }

    public class FilterInfo : IQueryInfo, IFilterRule
    {
        public string ColumnName;
        public object Value;
        public FilterOperation Operation;
        
        public FilterInfo(string columnName, object value):this(columnName,value,FilterOperation.Equals){}

        public FilterInfo(string columnName, object value, FilterOperation op)
        {
            ColumnName = columnName;
            Value = value;
            Operation = op;
        }

        public static bool Filter(FilterOperation op, object lvalue, object rvalue)
        {
            bool isMatch = false;
            switch (op)
            {
                case FilterOperation.Equals:
                case FilterOperation.GreaterThan:
                case FilterOperation.LessThan:
                case FilterOperation.DoesNotEqual:
                    if (lvalue == null || rvalue == null)
                    {
                        return op == FilterOperation.DoesNotEqual;
                    }
                    if (lvalue.GetType() != rvalue.GetType())
                    {
                        try
                        {
                            rvalue = PropertyConverter.Convert(rvalue, lvalue.GetType());
                        }
                        catch
                        {
                            if (op == FilterOperation.DoesNotEqual)
                                return true;
                        }
                    }
                    IComparable l = lvalue as IComparable;
                    IComparable r = rvalue as IComparable;
                    
                    if( l == null || r == null )
                    {
                    	if( op == FilterOperation.Equals )
						{
							if( rvalue == null && lvalue == null ) //should null == null?
								return true;
							else if( rvalue == null || lvalue == null )
								return false;
                    		return rvalue.Equals(lvalue);
						}
                    	else if( op == FilterOperation.DoesNotEqual )
                    	{
                    		if( rvalue == null || lvalue == null )
                    			return true;
                    		return ! rvalue.Equals(lvalue);
                    	}
                    }
                    
                    int result = l.CompareTo( r );
                    if( result == 0 )
                        isMatch = op == FilterOperation.Equals;
                    else if( result < 0 )
                        isMatch = op == FilterOperation.LessThan || op == FilterOperation.DoesNotEqual;
                    else
                        isMatch = op == FilterOperation.GreaterThan || op == FilterOperation.DoesNotEqual;
                    break;
                case FilterOperation.In:
                case FilterOperation.NotIn:
                    if (rvalue is IList)
                    {
                        IList list = rvalue as IList;
                        isMatch = list.Contains(lvalue);
                    }
					else if( rvalue is IRecordList )
					{
						IRecordList irl = (IRecordList)rvalue;
						isMatch = irl.Contains(lvalue as AbstractRecord);
					}
                    else if (rvalue is string)
                    {
                        isMatch = rvalue.ToString().Contains(lvalue.ToString());
                    }
                    if (op == FilterOperation.NotIn) isMatch = !isMatch;
                    break;
                case FilterOperation.Contains:
                case FilterOperation.NotContains:
                	if( lvalue is IList )
                	{
                		isMatch = (lvalue as IList).Contains(rvalue);
                	}
                	else if( lvalue is string && rvalue != null )
                	{
                		isMatch  = (lvalue as string).Contains(rvalue.ToString());
                	}
                	if( op == FilterOperation.NotContains )
                		isMatch = ! isMatch;
                	break;                	
            }
            return isMatch;
        }

        public override string ToString()
        {
        	object oVal = Value is AbstractRecord ? (Value as AbstractRecord).ObjectId : Value;
            string v = oVal != null ? oVal.ToString() :null;
            if( this.Operation == FilterOperation.Equals && oVal == null )
            {
            	return ColumnName + " IS NULL";
            }
            if( this.Operation == FilterOperation.DoesNotEqual && oVal == null )
            {
            	return ColumnName + " IS NOT NULL";
            }
            if( this.Operation == FilterOperation.Contains || this.Operation == FilterOperation.NotContains )
            {
            	v = Util.Surround(v,"%");
            }
            else if( this.Operation == FilterOperation.In || this.Operation == FilterOperation.NotIn)
            {
				if( Value is IList )
            		v = string.Format("({0})",Util.Join( (IList)Value, ",", false ) );
				else if( Value is IRecordList )
				{
					IRecordList irl = (IRecordList)Value;
					v = string.Format("({0})",Util.Join( irl.ToIdArray(), ",", false ) );
				}
            }
            if( oVal is DateTime )
            {
            	DateTime d = (DateTime)oVal;
            	v = "'" + d.ToString("s") + "'";
            }
			else if (oVal is string || oVal is bool)
            {
                v = "'" + v.ToString().Replace("'", "''") + "'";
            }
			else if (oVal is Enum)
			{
				v = Convert.ToInt32(oVal).ToString();
			}
            return string.Format("{0} {1} {2}", ColumnName, FilterOperationToString(Operation), v);
        }
        
        public override bool Equals (object o)
        {
        	if( o is FilterInfo )
        	{
        		FilterInfo fi = o as FilterInfo;
        		return fi.ColumnName == this.ColumnName && fi.Operation == this.Operation && fi.Value == this.Value;
        	}
        	return base.Equals (o);
        }

		public override int GetHashCode ()
		{
			return (Operation.ToString() + ColumnName + Value.ToString()).GetHashCode();
		}
		
        static public string FilterOperationToString(FilterOperation op)
        {
            string symbol = "UNK";
            switch (op)
            {
                case FilterOperation.Equals:
                    symbol = "=";
                    break;
                case FilterOperation.DoesNotEqual:
                    symbol = "<>";
                    break;
                case FilterOperation.GreaterThan:
                    symbol = ">";
                    break;
                case FilterOperation.LessThan:
                    symbol = "<";
                    break;
                case FilterOperation.In:
                    symbol = "IN";
                    break;
                case FilterOperation.NotIn:
                    symbol = "NOT IN";
                    break;
                case FilterOperation.Contains:
                	symbol = "LIKE";
                	break;
                case FilterOperation.NotContains:
                	symbol = "NOT LIKE";
                	break;
				case FilterOperation.GreaterThanOrEqual:
					symbol = ">=";
					break;
				case FilterOperation.LessThanOrEqual:
					symbol = "<=";
					break;
            }
            return symbol;
        }
    }
}
