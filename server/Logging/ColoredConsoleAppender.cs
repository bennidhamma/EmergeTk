using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using log4net.Core;
using log4net.Layout;
using log4net.Util;
using log4net.Appender;

namespace EmergeTk
{
	public class ColoredConsoleAppender : AppenderSkeleton
	{
		
		protected override void Append (LoggingEvent loggingEvent)
		{
			
			StringBuilder sb = new StringBuilder();

			string[] classParts = loggingEvent.LoggerName.Split('.');
			string className =  loggingEvent.LoggerName;
			if( classParts.Length > 0 )
				className = classParts[classParts.Length-1];
			className = className.PadRight(15);

			string header = string.Format( "{0}\t[{1}]\t{2}\t - ",
				loggingEvent.Level.Name,
				loggingEvent.TimeStamp.ToString("o"),
				className);
			if( loggingEvent.MessageObject is IEnumerable && ! ( loggingEvent.MessageObject is string ) )
			{
				foreach( object o in (IEnumerable)loggingEvent.MessageObject )
				{
					try
					{
						if( o is IList )
						{
							sb.Append(string.Format("{0}\t",Util.Join((IList)o,", ")));
						}
						sb.Append(string.Format("{0}\t",o != null ? o : "null"));
					}
					catch(Exception e )
					{
						System.Console.WriteLine("error", e.Message );
					}
				}
			}
			else
			{
				sb.Append( loggingEvent.MessageObject );
			}
			
			Log( loggingEvent.Level, header, sb.ToString() );
		}

		protected override void Append (params LoggingEvent[] loggingEvents)
		{
			foreach( LoggingEvent l in loggingEvents )
			{
				Append( l );
			}
		}

		public void Log (Level level, string header, string message)
		{
			switch (level.Name) {
			case "FATAL":
				ConsoleCrayon.ForegroundColor = ConsoleColor.Yellow;
				ConsoleCrayon.BackgroundColor = ConsoleColor.Red;
				break;
			case "ERROR":
				ConsoleCrayon.ForegroundColor = ConsoleColor.Red;
				ConsoleCrayon.BackgroundColor = ConsoleColor.Yellow;
				break;
			case "WARN":
				ConsoleCrayon.ForegroundColor = ConsoleColor.Yellow;
				break;
			case "INFO":
				ConsoleCrayon.ForegroundColor = ConsoleColor.Green;
				break;
			case "DEBUG":
				ConsoleCrayon.ForegroundColor = ConsoleColor.Blue;
				break;
			default:
				break;
			}
			
			Console.Write ("{0}", header );
			ConsoleCrayon.ResetColor ();
			Console.WriteLine (" " + message);
		}
	}
}
