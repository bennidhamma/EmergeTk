using System;
using System.IO;
using System.Text;
namespace EmergeTk.WebServices
{
	public class CsvMessageWriter : IMessageWriter
	{
		public CsvMessageWriter ()
		{
		}
	
		private Stream stm;
		
        public void WriteToStream(String s)
        {
            stm.Write(UTF8Encoding.Default.GetBytes(s), 0, s.Length);
        }
		
		static public CsvMessageWriter Create(Stream stm)
        {
            if (stm == null)
            {
                throw new ArgumentException("JsonMessageWriter.Create called with null Stream argument");
            }
            CsvMessageWriter This = new CsvMessageWriter();
            This.stm = stm;
            return This;
        }

		#region IMessageWriter implementation
		public void OpenRoot (string name)
		{
			throw new NotImplementedException ();
		}

		public void CloseRoot ()
		{
			throw new NotImplementedException ();
		}

		public void OpenObject ()
		{
			throw new NotImplementedException ();
		}

		public void CloseObject ()
		{
			throw new NotImplementedException ();
		}

		public void OpenList (string name)
		{
			throw new NotImplementedException ();
		}

		public void CloseList ()
		{
			throw new NotImplementedException ();
		}

		public void OpenProperty (string name)
		{
			throw new NotImplementedException ();
		}

		public void CloseProperty ()
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (string scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (int scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (bool scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (double scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (float scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (DateTime scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (decimal scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteScalar (object scalar)
		{
			throw new NotImplementedException ();
		}

		public void WriteProperty (string name, string scalarValue)
		{
			throw new NotImplementedException ();
		}

		public void WriteProperty (string name, int scalarValue)
		{
			throw new NotImplementedException ();
		}

		public void WriteRaw (string data)
		{
			WriteToStream (data);
		}

		public void Flush ()
		{
			throw new NotImplementedException ();
		}
		#endregion
}
}

