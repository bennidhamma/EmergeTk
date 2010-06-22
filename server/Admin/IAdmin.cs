using System;
using EmergeTk;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using EmergeTk.Widgets.Html;


namespace EmergeTk
{
	public interface IAdmin
	{
		string AdminName { get; }
		string Description { get; }
		Permission AdminPermission { get; }
	}
}
