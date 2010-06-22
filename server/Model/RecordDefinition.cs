// RecordDefinition.cs created with MonoDevelop
// User: ben at 7:20 P 11/06/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace EmergeTk.Model
{
	public struct RecordKey : IComparable
	{
        string type;
        int id;
		public string Type 
        {
            get { return type; }
            set { type = value; }
        }
		public int Id 
        {
            get { return id; }
            set { id = value; }
        }
		
		public override string ToString ()
		{
			return string.Format("[RecordKey: Type={0}, Id={1}]", Type, Id);
		}
		
		public AbstractRecord ToRecord()
		{
			Type type = TypeLoader.GetType(Type);
			return AbstractRecord.Load(type, Id);
		}

        // IComparable implementation, so we can sort these.
        public int CompareTo(Object o)
        {
            return Id - ((RecordKey)o).Id;
        }

        public RecordKey(string type, int id)
        {
            this.type = type;
            this.id = id;
        }
	}
	
	public struct RecordDefinition
	{
		Type type;
		int id;
		
		public Type Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}

		public int Id {
			get {
				return id;
			}
			set {
				id = value;
			}
		}
		
		public RecordDefinition(Type type, int id)
		{
			if( type == null )
				throw new ArgumentNullException();
			this.type = type;
			this.id = id;
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(RecordDefinition))
				return false;
			EmergeTk.Model.RecordDefinition other = (EmergeTk.Model.RecordDefinition)obj;
			return type == other.type && id == other.id;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (type != null ? type.GetHashCode () : 0) ^ id.GetHashCode ();
			}
		}
		
		public override string ToString ()
		{
			return string.Format("Record:{0}:{1}", Type, Id);
		}
		
		public static RecordDefinition FromString(string inString)
		{
			string[] parts = inString.Split(':');
			return new RecordDefinition(TypeLoader.GetType(parts[1]), int.Parse(parts[2]));
		}

		public static bool operator == (RecordDefinition a, RecordDefinition b )
		{
			return a.Equals(b);	
		}
		
		public static bool operator != (RecordDefinition a, RecordDefinition b )
		{
			return !a.Equals(b);	
		}
	}
}
