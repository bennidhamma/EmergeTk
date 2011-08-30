using System;

namespace EmergeTk.Model
{
	public enum DataType
	{
        None = 0,
        Text,
		SmallText,
		LargeText,
        RecordSelect,
        RecordSelectOrCreate,
        RecordList,
        Integer,
        Decimal,
        Float,
        DateTime,
        Xml,
		Json,
        Ignore,
        Slider,
        ReadOnly,
		Volatile //writable only to memory.
	}

    public class DataTypeHelper
    {
        public static Type SystemForDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.LargeText:
                case DataType.SmallText:
                case DataType.Text:
                case DataType.Xml:
                    return typeof(string);
                case DataType.DateTime:
                    return typeof(DateTime);
                case DataType.Decimal:
                    return typeof(decimal);
                case DataType.Float:
                    return typeof(float);
                case DataType.Integer:
                    return typeof(int);
                case DataType.RecordSelect:
                    return typeof(AbstractRecord);
                case DataType.RecordList:
                    return typeof(object);
            }
            return null;
        }
	}

	public class PropertyTypeAttribute : Attribute
	{
		private DataType type;
        private int? length = null;

		public DataType Type
        {
            get { return type; }
        }

        public int? Length
        {
            get { return length; }
        }

		public PropertyTypeAttribute( DataType type )
		{
			this.type = type;
		}
		
		public PropertyTypeAttribute() { }

        public PropertyTypeAttribute(DataType type, int length)
        {
            this.type = type;
            this.length = length;
        }
	}
}
