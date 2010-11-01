
using System;

namespace EmergeTk.Model
{
	public delegate object Converter( object input );
	
	public class ConversionKey
	{
		protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ConversionKey));
		
		public Type InputType { get; set; }
		public Type OutputType { get; set; }
		
		public ConversionKey( Type inputType, Type outputType )
		{
			if( inputType == null || outputType == null)
				throw new ArgumentNullException();
			this.InputType = inputType;
			this.OutputType = outputType;
		}
		
		public override string ToString ()
		{
			return string.Format("[ConversionKey: InputType={0}, OutputType={1}]", InputType, OutputType);
		}

		public override int GetHashCode ()
		{
			return (int)(((long)InputType.GetHashCode() + (long)OutputType.GetHashCode()) % int.MaxValue);
		}
		
		public override bool Equals (object obj)
		{
			ConversionKey other = obj as ConversionKey;
			if( other != null && InputType == other.InputType && OutputType == other.OutputType)
			{
				return true;	
			}
			else
			{
				return false;
			}
		}
	}
}
