using System;
using System.Collections.Generic;
using System.Text;

namespace EmergeTk
{
    public class StringSurface : Surface
    {
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(StringSurface));		
		
        StringBuilder sb;
        public override void Write(string data)
        {
            if (sb == null) sb = new StringBuilder();
            sb.Append(data);            
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}
