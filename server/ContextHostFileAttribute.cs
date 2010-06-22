using System;

namespace EmergeTk
{
	public class ContextHostFileAttribute : Attribute
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ContextHostFileAttribute));			
		
		string name;
		public virtual string Name
        {
			get { return name; }
			set { name = value; }
		}

		public ContextHostFileAttribute(string name)
		{
			this.name = name;
		}
	}
}
