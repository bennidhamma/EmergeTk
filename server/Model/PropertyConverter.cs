/** Copyright (c) 2006, All-In-One Creations, Ltd.
*  All rights reserved.
* 
* Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
* 
*     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
*     * Neither the name of All-In-One Creations, Ltd. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
/**
 * Project: emergetk: stateful web framework for the masses
 * File name: PropertyConverter.cs
 * Description: framework specific conversion logic.
 *   
 * Author: Ben Joldersma
 *   
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace EmergeTk.Model
{   
    public class PropertyConverter
    {
    	protected static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(PropertyConverter));
		
		private static Dictionary<ConversionKey,Converter> converters = new Dictionary<ConversionKey,Converter>();
		
		public static void AddConverter( ConversionKey ck, Converter c )
		{
			log.Debug("Adding Converter ", ck.InputType, ck.OutputType );
			converters[ck] = c;
		}

		static public object Convert(object input, Type destinationType)
    	{
    		return Convert( input, destinationType, null );
    	}
    	
        static public object Convert(object input, Type destinationType, object hint)
        {     	
        	try
        	{
				//log.DebugFormat( "converting [{0}] to [{1}] (null? {2}) empty? {3}", 
				//                input, destinationType, input != null ? input.GetType().ToString() : "null", input == "" );
        		if ( destinationType.IsInstanceOfType( input ) )
	                return input;
				if (input == null )
					return null;
				object output = input;
				ConversionKey ck = new ConversionKey(input.GetType(),destinationType);
				if( converters.ContainsKey(ck) )
				{
					return converters[ck](input);
				}
	            else if (destinationType.IsSubclassOf(typeof(AbstractRecord)) && !(output is AbstractRecord))
	            {
	                if (input == DBNull.Value || ( input is string && ((string)input == "NULL" || (string)input == string.Empty )))
	                    output = null;
	                else
	                {
	                	if( hint == null )
	                	{
							output = TypeLoader.InvokeGenericMethod( typeof(AbstractRecord), "Load", new Type[]{destinationType},null, new Type[]{typeof(object)}, new object[]{input} );
		                }
						else
						{
							output = TypeLoader.InvokeGenericMethod( typeof(AbstractRecord), "LoadUsingRecord", new Type[]{destinationType}, null, new Type[]{destinationType, typeof(object)}, new object[]{hint,input}  );
						}
	                }
	            }
	            else if( destinationType == typeof(int) && input.GetType().IsSubclassOf(typeof(AbstractRecord) ) )
	            {
	                output = (int)input;
	            }
	            else if( destinationType == typeof(bool) && input is int )
	            {
	            	output = ((int)input == 1);
	            }
	            else if( destinationType == typeof(bool) && input is System.SByte )
	            {
	            	output = ((System.SByte)input == 1);
	            }
	            /*else if( destinationType == typeof(bool) && input is String )
	            {
	            	log.Debug("converting to boolean");
	            	throw new Exception();
					output = ! string.IsNullOrEmpty(input as String);
				}
	            else if( destinationType == typeof(bool) )
	            {
	            	log.Debug("converting to boolean (non-string)");
					output = input != null;
				}*/
	            else if (destinationType == typeof(int) && input.GetType().IsEnum)
	            {
	                output = System.Convert.ToInt16(input);
	            }  
				else if (destinationType.IsEnum && input is string)
				{
					output = Enum.Parse (destinationType, (string)input);
				}
	            else if (destinationType == typeof(TimeSpan) && input is long)
	            {
	            	output = new TimeSpan( (long)input );
	            }
	            else if (destinationType == typeof(DateTime) && input is int)
	            {
					output = DateTime.SpecifyKind(System.Convert.ToDateTime(input), DateTimeKind.Utc);
	            }  				
	            else
	            {
	                TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
	                if (!converter.CanConvertFrom(output.GetType()))
	                {
	                    output = output.ToString();
						if( output.Equals("") )
							return null;
	                }
	                try
	                {
	                    output = converter.ConvertFrom(output);
	                }
	                catch (Exception e)
	                {
	                    log.Error(string.Format("Error converting [{0}]({3}) to [{1}]({2})", input, destinationType, Util.BuildExceptionOutput( e ), input.GetType() ));
	                    throw new Exception("error",e);
	                }
	            }
	            return output;
	         }
	         catch( Exception e )
	         {
	         	log.Error("Error converting from %o (%s) to %s using hint %o: %s", input, input.GetType(), destinationType, hint, Util.BuildExceptionOutput(e) );
	         	throw new Exception("error",e);
	         }
        }   
    }
}
