using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using log4net;
using log4net.Core;

namespace EmergeTk
{
	public delegate bool LogValidator();
	
	public class EmergeTkLog  : LogImpl
	{
		public List<string> Errors { get; set; }
		
		protected virtual string GetPrefix(){ return ""; } 
		
        public EmergeTkLog(ILogger logger)
            : base(logger)
		{
			Errors = new List<string>();
		}
		
        [Conditional ("DEBUG")]
		public void Debug (params object[] args)
		{
			if( logValidator != null && ! logValidator() )
				return;
			base.Debug( args );
		}

		public override void DebugFormat(string format, params object[] args )
		{
			Debug( string.Format( format, args ) );
		}
		
		public void Error (params object[] args)
		{
			if( logValidator != null && ! logValidator() )
				return;
			base.Error( args );
		}

		public void Warn (params object[] args)
		{
			if( logValidator != null && ! logValidator() )
				return;
			base.Warn( args );			
		}
	
		
		public void Info (params object[] args)
		{
			if( logValidator != null && ! logValidator() )
				return;
			base.Info( args );			
		}

		static LogValidator logValidator; 
		
		public static void RegisterLogValidator( LogValidator handler )
		{
			logValidator += handler;
		}
	}
}

