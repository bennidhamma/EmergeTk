// SortRecord.cs
//	
//

using System;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model
{
	public class Scalar<T> : AbstractRecord
	{
		T dataValue;
		
		public T DataValue {
			get {
				return dataValue;
			}
			set {
				dataValue = value;
			}
		}

		public override string ToString ()
		{
			if( DataValue == null )
				return base.ToString();
			return DataValue.ToString();
		}

		public override bool Equals (object obj)
		{
			Scalar<T> o = obj as Scalar<T>;
			if( o != null && o.DataValue != null )
			{
				return o.DataValue.Equals( DataValue );
			}
			return base.Equals (obj);
		}

		public override int GetHashCode() 
		{
			return (dataValue.ToString() + id.ToString()).GetHashCode();
		}
		
		public Scalar()
		{
		}
	}
}
