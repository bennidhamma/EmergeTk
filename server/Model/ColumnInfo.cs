	using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace EmergeTk.Model
{
    public class ColumnInfo
    {
        public string Name;
        public string FriendlyName;
        public string HelpText;
        public Type Type;		
        public DataType DataType;
        public bool Identity; 
        public bool IsDerived;
        public Type ModelType;
        public PropertyInfo PropertyInfo;
		public bool IsList;
		public bool IsRecord;
		public Type ListRecordType;
		public bool ReadOnly;
        public int? Length;

		public bool IsNullableEnum
		{
			get
			{
				if (this.Type == null)
					return false;

				Type underlyingType = Nullable.GetUnderlyingType(this.Type);
				return underlyingType != null && underlyingType.IsEnum;
			}
		}
		
        public ColumnInfo(string name, Type type, DataType dataType, Type modelType, bool readOnly )
        {
            Name = name;
            Type = type;
            DataType = dataType;
            ModelType = modelType;
			IsList = type.GetInterface("IRecordList") != null;
			IsRecord = typeof(AbstractRecord).IsAssignableFrom(type);
			Identity = false;
			FriendlyName = null;
			HelpText = null;
			PropertyInfo = null;
			ReadOnly = readOnly;
            Length = null;
			if( IsList )
			{
				ListRecordType = type.GetGenericArguments()[0];
			}
        }

		public override string ToString ()
		{
			return string.Format ("[ColumnInfo: Name={0}, Type={1}, ModelType={5}, DataType={2}, IsList={3}, IsRecord={4}]", Name, Type, DataType, IsList, IsRecord,
				ModelType);
		}
		
    }
}
