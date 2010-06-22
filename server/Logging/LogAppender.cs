using System;
using log4net.Appender;
using log4net.Core;

namespace EmergeTk
{

	public class FirebugAppender : AppenderSkeleton
	{
		
		protected override void Append (LoggingEvent loggingEvent)
		{
			if( EmergeTk.Context.Current != null && EmergeTk.Context.Current.HttpContext.Request["data"] == null )
			{
				string msgobj = JSON.Default.Encode(loggingEvent.MessageObject);
				if( msgobj.StartsWith("[") && msgobj.EndsWith("]") )
					msgobj = msgobj.Substring(1,msgobj.Length - 2 );

				if( loggingEvent.Level != Level.Info &&
				! loggingEvent.LocationInformation.ClassName.Contains("EmergeTkLog") )
				{
					EmergeTk.Context.Current.RawCmd(
						"console.{0}('[{1}][{2}.{3}(line:{4})]',{5});",
						loggingEvent.Level.ToString().ToLower(), //0
						loggingEvent.LoggerName, //1
						loggingEvent.LocationInformation.ClassName, //2
						loggingEvent.LocationInformation.MethodName, //3
						loggingEvent.LocationInformation.LineNumber, //4
						msgobj //5
					);
				}
				else
				{	
					EmergeTk.Context.Current.RawCmd(
						"console.{0}('[{1}]',{2});",
						loggingEvent.Level.ToString().ToLower(),
						loggingEvent.LoggerName,
						msgobj
					);
				}
			}
		}

		protected override void Append (params LoggingEvent[] loggingEvents)
		{
			foreach( LoggingEvent l in loggingEvents )
			{
				Append( l );
			}
		}		
	}
}
