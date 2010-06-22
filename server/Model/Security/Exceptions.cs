using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk.Model.Security
{
    // TODO: create generic ValidationException base class
    //  modelform can catch this exception and handle it
    //  include a default error message for each type of ValidationException
    //  also include what field (ColumnInfo) caused the error for nice client side error display

    public class UserAlreadyExistsException : Exception
    {
    	public UserAlreadyExistsException(string message):base(message){}
    }
    
    public class UnauthorizedRecordAccessException : Exception
    {
    	public UnauthorizedRecordAccessException(string message):base(message){}
    }
}
