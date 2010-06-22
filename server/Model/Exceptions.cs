using System;
using System.Collections.Generic;

namespace EmergeTk.Model
{
    public class ValidationException : UserNotifiableException
    {
		public List<ValidationError> Errors { get; set; }
		
    	public ValidationException(string message):base(message){}

		public ValidationException(string message, List<ValidationError> errors):base(message){
			Errors = errors;
		}
    }
	
	public struct ValidationError
	{
		public string Path;		
		public string Problem;
		public string Suggestion;
	}
    
    public class VersionOutOfDateException : Exception
    {
    	public VersionOutOfDateException(string message):base(message){}
    }

    public class TableNotFoundException : Exception
    {
        public TableNotFoundException(string message):base(message){}
    }
	
	public class RecordNotFoundException : Exception
	{
		public RecordNotFoundException(string message):base(message){}
	}
}
