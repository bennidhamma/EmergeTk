using System;
using System.Reflection;
using Mono.Options;

namespace emergecli
{
	class MainClass
	{		
		public static void Main (string[] args)
		{
			var p = new ParseArgs ();
			var command = p.Parse (args);
			if (command != null)
				command.Run ();
			Environment.Exit (0);
		}
	}
	
	public interface ICommand
	{
		void Run ();
		bool Validate ();
		OptionSet GetOptions (string[] args);
	}
	
	public class CommandAttribute : Attribute
	{
		public string Name {get; set;}
	}

	public class ParseArgs 
	{
		public ICommand Parse (string[] args)
		{
			if(args.Length == 0)
			{
				ShowHelp (null, null);
				return null;
			}
			
			var command = GetCommand (args[0]);
			var options = command.GetOptions (args);
			
			try
			{
				options.Parse (args);
			}
			catch
			{
				ShowHelp (command, options);
				Environment.Exit (1);
			}
			
			if (!command.Validate ())
			{
				ShowHelp (command, options);
				Environment.Exit (1);
			}
				
			return command;
		}
		
		public ICommand GetCommand (string name)
		{
			foreach (Type t in Assembly.GetCallingAssembly ().GetTypes ())
			{
				var atts = t.GetCustomAttributes (typeof(CommandAttribute), false);
				if (atts != null && atts.Length > 0)
				{
					var ca = (CommandAttribute)atts[0];
					if (ca.Name == name)
						return (ICommand)Activator.CreateInstance (t);
				}
			}
			return null;
		}
		
		public void ShowHelp (ICommand cmd, OptionSet options)
		{
			Console.WriteLine ("Showing help...");
			
			if (options != null)
				options.WriteOptionDescriptions (Console.Out);
		}
	}
}
