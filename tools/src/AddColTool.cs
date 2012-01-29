using System;

namespace emergetool
{
	public class AddColTool : IEmergeTool
	{
		public AddColTool ()
		{
		}

		#region IEmergeTool implementation
		public void Preopts ()
		{
		}

		public void ParseArgs (string[] args, int idx)
		{
			throw new NotImplementedException ();
		}

		public void Postopts ()
		{
		}

		public void Preflight ()
		{
		}

		public void Run ()
		{
			throw new NotImplementedException ();
		}

		public void Postflight ()
		{
		}

		public string Name {
			get {
				return "addcol";
			}
		}

		public string Usage {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion
	}
}

