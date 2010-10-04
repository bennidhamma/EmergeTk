using System;
namespace EmergeTk.Model
{
	public class DataSourceAttribute : Attribute
	{
		public string Name { get; set; }
		public DataSourceAttribute (string name)
		{
			Name = name;
		}
	}
}

