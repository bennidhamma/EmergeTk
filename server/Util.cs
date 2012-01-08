/**
 * Project: emergetk: stateful web framework for the masses
 * File name: .cs
 * Description:
 *   
 * @author Ben Joldersma, All-In-One Creations, Ltd. http://all-in-one-creations.net, Copyright (C) 2006.
 *   
 * @see The GNU Public License (GPL)
 */
/* 
 * This program is free software; you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation; either version 2 of the License, or 
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License along 
 * with this program; if not, write to the Free Software Foundation, Inc., 
 * 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Web;
using Boo.Lang.Compiler;
using Boo.Lang.Interpreter;
 
namespace EmergeTk
{
	/// <summary>
	/// Summary description for Util.
	/// </summary>
	public static class Util
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(Util));
		
        public static string RootPath { get { return HttpContext.Current.Server.MapPath(HttpContext.Current.Request.ApplicationPath + "\\"); } }

		public static string Join(this IList list, string sep)
		{
			return Join( list, sep, false );
		}

        /*public static string Join(IList list, string sep)
		{
			return Join( list, sep, false );
		}*/

        public static string Join(IList list, string sep, bool formatForClient)
        {
           return  Join(list, sep, formatForClient, 0);
        }

        public static string Join(IList list, string sep, bool formatForClient, int startIndex)
        {
			if( list != null )
            	return Join(list, sep, formatForClient, startIndex, list.Count - 1);
			else
				return null;
        }     

        public static string Join(IList list, string sep, bool formatForClient, int startIndex, int finishIndex)
        {
            if (list == null || list.Count == 0)
                return null;
            StringBuilder sb = new StringBuilder();
            for (int i = startIndex; i < finishIndex; i++)
            {
                string val;
                val = prepareValue(list, formatForClient, i);
                sb.Append(val);
                sb.Append(sep);
            }
            sb.Append(prepareValue( list, formatForClient, finishIndex ));
            return sb.ToString();
        }

        private static string prepareValue(IList list, bool formatForClient, int i)
        {
            string val;
            if (formatForClient)
                val = FormatForClient(list[i] as string);
            else
            {
                if (list[i] != null)
                    val = list[i].ToString();
                else
                    val = string.Empty;
            }
            return val;
        }


        public static string Join<T>(T[] cols)
        {
            return Join(cols, ",");
        }

        public static string Join<T>(List<T> cols)
        {
            return Join(cols, ",");
        }
		
		public static string JoinToString<T>(this IEnumerable<T> list, string sep)
		{
			StringBuilder sb = new StringBuilder();
            foreach( T t in list )
            {
                sb.Append(t);
                sb.Append(sep);
            }
			string result = sb.ToString();
            return result.Substring( 0, result.Length-sep.Length );
		}
		
		public static void ForEach<T>(this IEnumerable<T> list, Action<T> func)
		{
			foreach( T t in list ) func(t);				
		}
		
		public static void Times(this int count, Action<int> func)
		{
			for( int i = 0; i < count; i++ )
				func(i);
		}
        
        public static string Quotize(string s)
        {
            if (s == null || s == string.Empty)
            {
                return "\"\"";
            }
            //s = s.Replace("</script>", "</scr' + 'ipt>");
            //s = s.Replace("</SCRIPT>", "</scr' + 'ipt>");
			return "\"" + s + "\"";
            //return Surround(s, "\"");    
        }

        public static string Surround( string inner, string surrounder )
        {
            return string.Format("{0}{1}{0}",surrounder, inner);
        }

        public static string SurroundTag(string inner, string tag)
        {
            return string.Format("<{0}>{1}</{0}>", tag, inner);
        }
        
        public static string FormatForClient( string input )
		{
			if( input == null )
				return string.Empty;
			StringBuilder sb = new StringBuilder (input.Length * 2);
			for (int i = 0; i < input.Length; i++)
			{
				char c = input[i];
				switch(c)
				{
				case '\t':
					sb.Append ("\\t");
					break;
				case '\r':
					sb.Append ("\\r");
					break;
				case '\n':
					sb.Append ("\\n");
					break;
				case '\"':
					sb.Append ("\\\"");
					break;
				case '\\':
					sb.Append ("\\\\");
					break;
				default:
					sb.Append(c);
					break;
				}
			}			
            return sb.ToString();
		}

        public static string ToJavaScriptString(string input)
        {
            return Quotize(FormatForClient(input));
        }

        public static string Coalesce(params string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                    return args[i];
            }
            return null;
        }

        public static string Textalize(string source)
		{
			Regex newLineRemoveReg = new Regex(@"(==|\>)\n");
			source = newLineRemoveReg.Replace(source,"$1");
			Regex r = new Regex(@"(?<tag>====|===|==|--|'''|\''|\|)(?<inner>.*?)\k<tag>",RegexOptions.Singleline);
			MatchEvaluator me = new MatchEvaluator( matchTag );
			source = r.Replace( source, me );
            source = matchLists( source ); 
            source = source.Replace("\r\n\r\n", "<br><br>");
            source = source.Replace("\n\n", "<br><br>");
            source = source.Replace("\\n", "<br>");
            source = source.Replace("\\n", "<br>");
			return source;
		} 
		
		static private string matchLists( string source )
		{
			Regex numReg = new Regex(@"((^#+)(.*)\n)+([^#]?)",RegexOptions.Compiled|RegexOptions.Multiline);
			MatchEvaluator me = new MatchEvaluator( matchListOL );			
			source = numReg.Replace( source,me );
			Regex bulletReg = new Regex(@"((^\*+)(.*)\n)+([^#]?)",RegexOptions.Compiled|RegexOptions.Multiline);
			me = new MatchEvaluator( matchListUL );
			source = bulletReg.Replace( source,me );
			return source;
		}
		
		static private string matchListOL( Match m )
		{
			return matchList( m, "OL" );
		}
		
		static private string matchListUL( Match m )
		{
			return matchList( m, "UL" );
		}
		
		static private string matchList( Match m, string tag )
		{
			//group 0 is <OL>, group n-1 is </OL>
			//all groups in between are <LI>			
			string open = "<" + tag + ">", close = "</" + tag + ">";
			string s = open;
			int numOpen = 1;
			int line = 0;
			foreach( Capture c in m.Groups[2].Captures )
			{
				Capture c2 = m.Groups[3].Captures[line++];
				string hashes = c.Value;
				if( hashes.Length > numOpen )
				{
					numOpen++;
					s += open;
				}
				else if( hashes.Length < numOpen )
				{
					numOpen--;
					s += close;
				}
				s += "<LI>" + c2.Value + "</LI>";
			}
			s += close + m.Groups[4].Value;
			
			return s;
		}

		static private string matchTag( Match m )
		{
			string tag = string.Empty;
			string atts = string.Empty;
            string inner = m.Groups["inner"].Value;
			switch( m.Groups["tag"].Captures[0].Value )
			{
				case "'''":
					tag = "B";
					break;
				case "--":
					tag = "DEL";
					break;
				case "''":
					tag = "I";
					break;
				case "|":
					tag = "A";
					string[] parts = m.Groups["inner"].Value.Split(new char[]{' '}, 2, StringSplitOptions.RemoveEmptyEntries);
					atts = " HREF='" + parts[0] + "'";
					inner = parts.Length == 2 ? parts[1] : parts[0];					
					break;
                case "##":
                    tag = "OL";
                    inner = inner.Replace("#.", "<LI>");
                    break;
                case "**":
                    tag = "UL";
                    inner = inner.Replace("*.", "<LI>");
                    break;
                case "====":
                    tag = "H3";
                    break;
                case "===":
                    tag = "H2";
                    break;
                case "==":
                    tag = "H1";
                    break;


            }
            return string.Format("<{0}{2}>{1}</{0}>", tag, Util.Textalize(inner), atts);
		}
		
		static public string PascalToHuman( string p )
		{
			if( p == "_" )
				return p;
			Regex r = new Regex("([a-z0-9][A-Z]|[a-z][0-9])");
			MatchCollection matches = r.Matches(p);
			foreach( Match m in matches )
				p = p.Replace(m.Groups[0].Value, m.Groups[0].Value.Insert(1," "));
			p = p.Replace("_", " ");
			p = p.Trim();
			return p;
		}
		
		// Convert MakeFoo to makeFoo, CTR to CTR, HTMLInput to HTMLInput, HtmlInput to htmlInput.
		static public string PascalToCamel(string input)
		{
			if( input.Length > 1 && char.IsUpper(input[1]) )
				return input;
			return input[0].ToString().ToLower() + input.Substring(1);
		}
		
		//makeFoo to MakeFoo, CTR to CTR, htmlInput to HtmlInput
		static public string CamelToPascal(string input)
		{			
			return input[0].ToString().ToUpper() + input.Substring(1);
		}

		static public object Coalesce( params object[] values )
		{
			for( int i = 0; i < values.Length; i++ )
				if( values[i] != null )
					return values[ i ];
			return null;
		}

        static InteractiveInterpreter booInterpreter = null;
        public static object Eval(string eval)
        {
        	eval = '(' + eval + ')';	
            if( booInterpreter == null )
            {
                booInterpreter = new InteractiveInterpreter();  
                booInterpreter.RememberLastValue = true;
            }              
            try
            {
            	log.Debug("Boo Eval'ing ", eval );
            	booInterpreter.Reset();
                booInterpreter.Eval(eval);
                log.Debug("Boo Result: ", booInterpreter.LastValue ); 
                return booInterpreter.LastValue;
            }
            catch (Exception e)
            {
                log.Error(string.Format("=====\n\njscript eval error: {0} eval'ing: {1}\n=====\n\n", e.Message, eval) );
                return null;
            }
        }

        public static string HashToMime(Dictionary<string, string> input)
        {
            List<string> pairs = new List<string>();
            foreach( string key in input.Keys )
            {
                pairs.Add(key + "=" + input[key]);
            }
            return Join(pairs, "&");
        }
        
        public static string BuildExceptionOutput(Exception e)
        {
        	List<string> outputs = new List<string>();
	       	while( e != null )
	       	{
	            	outputs.Add( string.Format(
	                	"{0} ({1})\n\nStackTrace:\n{2}\n\n",
	                		e.Message,
	                		e.GetType(),
	                		e.StackTrace ) );
	       		e = e.InnerException;
	       	}
	       	outputs.Reverse();
	       	return Util.Join(outputs,"\n\n");
        }
        
        public static long ConvertFromBase32(string base32number)
		{
		   char[] base32 = new char[] {
		      '0','1','2','3','4','5','6','7',
		      '8','9','a','b','c','d','e','f',
		      'g','h','i','j','k','l','m','n',
		      'o','p','q','r','s','t','u','v'
		   };

		   long n = 0;

		   foreach (char d in base32number.ToLowerInvariant())
		   {
		      n = n << 5;
		      int idx = Array.IndexOf(base32, d);

		      if (idx == -1)
		         throw new Exception("Provided number contains invalid characters");

		      n += idx;
		   }

		   return n;
		}
		
		static char[] base32 = new char[] {
		      'A','B','C','D','E','F','G','H',
		      'I','J','K','L','M','N','O','P',
		      'Q','R','S','T','U','V','W','X',
		      'Y','Z','a','b','c','d','e','f'
		};
		
		public static string ConvertToBase32( long x )
		{
			string v;
		   	if( x >  0 )
				v = string.Empty;
			else
				v = "A"; 
			while( x > 0 )
			{
				v = base32[ x % 32 ] + v;
				x /= 32;
			}
			return v;
		}
		
		public static string ConvertToBase32( byte[] bytes )
		{
			string v = string.Empty;
			foreach( byte b in bytes )
			{
			   	if( b >  0 )
			   	{
			   		int x = b;
			   		while( x > 0 )
					{
						v += base32[ x % 32 ];
						x /= 32;
					}
				}
				else
					v += "0";
			}
			return v;
		}

        public static string GetBase32Guid()
        {
            return ConvertToBase32( Guid.NewGuid().ToByteArray() ).Replace("=","").Replace("+","");
        }
        
        public static string FriendlyTimeSpan( TimeSpan s )
        {
        	return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", s.Days, s.Hours, s.Minutes, s.Seconds); 
        }

		public static string Shorten(this string s, int length )
		{
			//shorten to first space inside of length.
			
			if(s== null || s.Length <=length )
				return s;

			s = s.Substring(0,length);
			if( s.Contains( " " ) )
			{
				s = s.Substring(0, s.LastIndexOf(' '));				
			}
			s += "...";
			return s;
		}
		
		public static IEnumerable<int> Range(this int max)  
        {  
            for (int i = 0; i < max; i++)  
                yield return i;  
        }
    }
}
