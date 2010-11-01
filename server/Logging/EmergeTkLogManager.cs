using System;
using log4net;

namespace EmergeTk
{
	
	public class EmergeTkLogManager
	{

        public EmergeTkLogManager()
		{
		}
		
		public static EmergeTkLog GetLogger(Type type)
		{
			return new EmergeTkLog(LogManager.GetLogger(type).Logger);
		}

        public static EmergeTkLog GetLogger(String name)
        {
            return new EmergeTkLog(LogManager.GetLogger(name).Logger);
        }

	}
}
