
using System;
using System.Collections.Generic;
using System.IO;

namespace emergetool
{
	
	/// <summary>
	/// SynchronizerEmergeTool is an IEmergeTool that uses a DatabaseSynchronizer object
	/// to analyze a .dll and report information on synchronization between it and its
	/// database.  See IEmergeTool for further documentation.
	/// </summary>
	public class SynchronizerEmergeTool : IEmergeTool
	{
		private bool tablesOnly;
		private bool colOnly;
		private bool ignoreEmergeTk;
		private bool verbose;
		string connectionString = null;
		
		private bool argsParsed;
		DatabaseSynchronizer dSynch;
		
		public string Name { 
			get { return "synchronizer"; }
		}

		string usage = @"synch: assesses synchronization issues between the given EmergeTk 
project .dll(s) and the corresponding database. Outputs SQL alter 
statements to fix synchonization issues such as missing tables, missing 
columns and extra columns.
usage: synchronizer [-t | -c | -i | -v | -d <string> ] [ <dll(s)> ]

Arguments:
	-t		: generate CREATE TABLE statements only
	-c		: generate ALTER TABLE statements only
	-i		: ignore AbstractRecords in the EmergeTk namespace
	-d <string>	: database connection <string>
	-v		: verbose mode
	<dll(s)>	: list of dll(s) to be analyzed

All .dll(s) in the directory in which this tool is used will also be analyzed.
If you wish to specify .dll(s) in other directories, list them with the 
optional argument <dll(s)>.

Note that if -t and -c are asserted, both tables and columns will be analyzed.

Disclaimer: This tool makes instances of all AbstractRecord derived classes in the 
given dll(s), meaning that any code in AbstractRecord derived class constructors 
will be run.";
		
		public string Usage { 
			get {
				return usage;
			}
		}
		
		public void Preopts() 
		{
			dSynch = new DatabaseSynchronizer();
			
			argsParsed = tablesOnly = colOnly = ignoreEmergeTk = verbose = false;	
			connectionString = null;
		}
					
		public void ParseArgs(string[] args, int idx)
		{
			argsParsed = true;

			int i;
			for (i = idx; i < args.Length; i++)
			{
				if (args[i].Equals("-d")) {
					i++;
					if (i >= args.Length)
						throw new ArgumentException("no connection string");
					connectionString = args[i];
				} else if ( args[i].StartsWith("-") )
					parseOpts( args[i].Substring(1) );
				else // done with arguments
					break;
			}
			
			//if ( args.Length == i )
			//	throw new ArgumentException("no dll arguments");
			
			// load dlls
			for (int j = i; j < args.Length; j++) 
			{
				dSynch.LoadAssembly(args[j]);	
			}			
		}

		private void parseOpts(string opts) {
			char[] optsArray = opts.ToCharArray();
			foreach (char op in optsArray) {
				if (op == 't')
				    tablesOnly = true;
				else if (op == 'c')
				    colOnly = true;
				else if (op == 'v')
					verbose = true;
				else if (op == 'i')
					ignoreEmergeTk = true;
				else
					throw new ArgumentException(op + " is not a valid option");
			}
		}

		
		public void Postopts() 
		{
			dSynch.Verbose = verbose;
			dSynch.IgnoreEmergeTk = ignoreEmergeTk;
			
			if ( connectionString != null )
				dSynch.SetConnectionString(connectionString);	
		}
		
		public void Preflight() {}

		public void Run() {
			if ( argsParsed ) {
				if (tablesOnly || !colOnly)
					Console.WriteLine(dSynch.GenerateCreateTableStatements());
				if (colOnly || !tablesOnly)
					Console.WriteLine(dSynch.GenerateAlterColumnStatements());
			} 
			else 
				throw new Exception("arguments not set");
		}
		
		public void Postflight() {}
	}
}
