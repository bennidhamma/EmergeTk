// IGroupable.cs
//	
//

using System;
using System.Collections.Generic;
using EmergeTk.Model;

namespace EmergeTk
{
	public interface IGroupable
	{
		List<RepeaterGroup> Groups { get; }
		AbstractRecord GroupDeterminant { get; }
		int CurrentGroupLevel { get; }
	}
}
