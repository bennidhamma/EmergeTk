using System.Globalization;
using System;
using System.Collections.Generic;
using EmergeTk;
using EmergeTk.Model;

namespace emergetool
{
	
	/// <summary>
	/// EmergeTools is a command line program for using emergeTk tools
	/// 
	/// Currently implemented tools:
	/// 
	/// 	synch: checks the project's database for synchronization with the
	/// 		   project's model and prints SQL alter statements to fix any-
	/// 		   thing that is out of sync
	/// 
	/// 	help: prints usage for this program
	/// 
	/// Adding a tool:
	/// 
	/// If you wish to add a tool:
	/// 1. make a tool class that implements the IEmergeTool interface
	/// 2. add the tool's name to the switch block in getToolFromName
	/// 3. add the tool's name to the availableTools array
	/// 
	/// </summary>

	public class EmergeTools
	{
		
		/// <summary>
		/// the name of the tool, as determined by the method callTool
		/// </summary>
		static IEmergeTool tool;
		
		/// <summary>
		/// currently available tools
		/// </summary>
		static string[] availableTools = {"help", "synch"};
		
		/// <summary>
		/// figures out which tool to use and calls its method, passing the command line
		/// arguments along too
		/// </summary>
		/// <param name="args">
		/// A <see cref="System.String"/>
		/// arguments from the command line
		/// </param>
		public static void Main(string[] args) {
			if (args.Length == 0) 
			{
				Console.Error.WriteLine("Error: No tool specified\n");
				printUsage(1);
			}
			
			if (args[0].Equals("help"))
				help(args);
			else
			{
				tool = getToolFromName(args[0]);
				try 
				{
					tool.Preopts();
					tool.ParseArgs(args, 1);
					tool.Postopts();
					tool.Preflight();
					tool.Run();
					tool.Postflight();
				}
				catch(Exception e)
				{
					Console.WriteLine(Util.BuildExceptionOutput(e));
					Console.WriteLine(tool.Usage + "\n");
					Environment.Exit(1);
				}
			}
			
		}
		
		/// <summary>
		/// get a new IEmergeTool implementation based on the name passed.
		/// if toolName doesn't have a tool associated with it, the program exists
		/// </summary>
		/// <param name="toolName">
		/// A <see cref="System.String"/>
		/// the name of the tool you want to get back
		/// </param>
		/// <returns>
		/// A <see cref="IEmergeTool"/>
		/// </returns>
		private static IEmergeTool getToolFromName(string toolName)
		{
			IEmergeTool result = null;
			
			switch(toolName)
			{	
			case "synch":
				result = new SynchronizerEmergeTool();
				break;
			default:
				Console.Error.WriteLine("Error: no tool " + toolName + "\n");
				printUsage(1);
				break;
			}
			
			return result;
		}
		
		/// <summary>
		/// help tool (for info on using this program and its subcommands)
		/// </summary>
		/// <param name="args">
		/// A <see cref="System.String"/>
		/// </param>
		private static void help(string[] args)
		{
			if (args.Length < 2)
				printUsage(0);
			else 
			{
				IEmergeTool toolToPrint = getToolFromName(args[1]);
				Console.WriteLine(toolToPrint.Usage);
				Console.WriteLine();
				System.Environment.Exit(0);	
			}
		}
				
		/// <summary>
		/// print main usage message, and quit the program
		/// </summary>
		/// <param name="exitCode">
		/// A <see cref="System.Int32"/>
		/// the exit code with which to exit with
		/// </param>
		private static void printUsage(int exitCode)
		{
			Console.WriteLine("usage: emergetool <subcommand> [options] [args]");
			Console.WriteLine("emergetool is a command-line tool for analyzing EmergeTk projects\n");
			Console.WriteLine("Type 'emergetool help <subcommand>' for help on a specific subcommand\n");
			Console.WriteLine("Available subcommands:");
			foreach(string s in availableTools)
				Console.WriteLine("\t" + s);
			Console.WriteLine();
			System.Environment.Exit(exitCode);
		}
	}
}