
using System;

namespace emergetool
{
	
	/// <summary>
	/// an interface for various types of tools to be used by emergetool
	/// </summary>
	public interface IEmergeTool
	{
		
		/// <value>
		/// The name of the tool
		/// </value>
		string Name { get; }
		
		//// <value>
		/// the tool's usage printout
		/// </value>
		string Usage { get; }
		
		void Preopts();
		
		/// <summary>
		/// parses the given arguments for use in UseTool
		/// </summary>
		/// <param name="args">
		/// A <see cref="System.String"/>
		/// </param>
		void ParseArgs(string[] args, int idx);

		void Postopts();
		
		void Preflight();
		
		/// <summary>
		/// do the tool's function
		/// </summary>
		void Run();
		
		void Postflight();
		
	}
}
