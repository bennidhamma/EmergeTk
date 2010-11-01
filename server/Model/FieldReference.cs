// FieldReference.cs
//	
//

using System;

namespace EmergeTk.Model
{
	
	
	public struct FieldReference
	{
		RecordDefinition record;
		string field;
		
		public RecordDefinition Record {
			get {
				return record;
			}
			set {
				record = value;
			}
		}
		
		public string Field {
			get {
				return field;
			}
			set {
				field = value;
			}
		}
		
		public FieldReference( RecordDefinition def, string field )
		{
			this.record = def;
			this.field = field;
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(FieldReference))
				return false;
			EmergeTk.Model.FieldReference other = (EmergeTk.Model.FieldReference)obj;
			return record == other.record && field == other.field;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return record.GetHashCode () ^ (field != null ? field.GetHashCode () : 0);
			}
		}
		
		
		public override string ToString ()
		{
            if (record.Type == null)
                return null;

			return string.Format( "{0}:{1}:{2}", record.Type.FullName, record.Id, field );
		}

	}
}
