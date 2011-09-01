
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmergeTk.Model;
using EmergeTk.Model.Security;
using System.Text;
using System.Xml;
namespace EmergeTk.WebServices
{
    public class XmlMessageWriter : IMessageWriter
    {
        protected Stack<XmlMessageWriterEntity> stack = new Stack<XmlMessageWriterEntity>();
        protected Stream stm;
        protected XmlWriter writer;

        protected XmlMessageWriter()
        {
        }


        static public XmlMessageWriter Create(Stream stm)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = UTF8Encoding.UTF8;
            settings.CloseOutput = false;
            settings.OmitXmlDeclaration = false;
            settings.Indent = false;
            settings.NewLineOnAttributes = false;
            settings.CheckCharacters = true;
            settings.NewLineHandling = NewLineHandling.None;
            settings.ConformanceLevel = ConformanceLevel.Auto;
            return XmlMessageWriter.Create(stm, settings);
        }

        static public XmlMessageWriter Create(Stream stm, XmlWriterSettings settings)
        {
            if (stm == null)
                throw new ArgumentException("XmlMessageWriter.Create called with null stream");

            XmlMessageWriter This = new XmlMessageWriter();
            This.writer = XmlTextWriter.Create(stm, settings);
			This.stm = stm;
            return This;
        }

        protected String ListNameOnStack()
        {
            if (stack.Count > 0)
            {
                XmlMessageWriterEntity itemOnStack = stack.Peek();
                if (itemOnStack.EntityType == XmlMessageWriterEntityType.List)
                {
                    return itemOnStack.Name;
                }
            }
            return String.Empty;
        }

        protected bool ListOnStack()
        {
            return stack.Count > 0 && stack.Peek().EntityType == XmlMessageWriterEntityType.List;
        }

        #region IMessageWriter Members

        public void OpenRoot(string name)
        {
            writer.WriteStartElement(name);
        }

        public void CloseRoot()
        {
            writer.WriteFullEndElement();
            writer.Close();
        }

        public void OpenObject()
        {
#if false
            if (stack.Count > 0)
                writer.WriteStartElement(stack.Peek().Name);
#endif
        }

        public void CloseObject()
        {
#if false
            if (stack.Count > 0)
                writer.WriteEndElement();
#endif
        }

        public void OpenList(string name)
        {
            if (stack.Count > 0)
                writer.WriteAttributeString("type", "array");

            stack.Push(XmlMessageWriterEntity.Create(name, XmlMessageWriterEntityType.List));
        }

        public void CloseList()
        {
            stack.Pop();
        }


        public void WriteScalar(string scalar)
        {
            WriteScalarHelper(scalar);
        }

        public void WriteScalar(int scalar)
        {
            WriteScalarHelper(JSON.Default.Encode(scalar));
        }

        public void WriteScalar(bool scalar)
        {
            WriteScalarHelper(JSON.Default.Encode(scalar));
        }
        
        public void WriteScalar(double scalar)
        {
            WriteScalarHelper(JSON.Default.Encode(scalar));
        }

        public void WriteScalar(DateTime scalar)
        {
            WriteScalarHelper(scalar.ToString());
        }

        public void WriteScalar(float scalar)
        {
            WriteScalarHelper(JSON.Default.Encode(scalar));
        }

        public void WriteScalar(Decimal scalar)
        {
            WriteScalarHelper(JSON.Default.Encode(scalar));
        }

        public void WriteScalar(Object scalar)
        {
            String val;

            if (scalar == null || scalar is System.DBNull)
            {
                WriteScalarHelper((String)null);
                return;
            }
            else if (scalar is string)
            {
                WriteScalarHelper(scalar.ToString());
                return;
            }
            if (scalar is bool)
                val = JSON.Default.Encode((bool)scalar);
            else if (scalar is double)
                val = JSON.Default.Encode((double)scalar);
            else if (scalar is float)
                val = JSON.Default.Encode((float)scalar);
            else if (scalar is decimal)
                val = JSON.Default.Encode((decimal)scalar);
            else if (scalar is int)
                val = JSON.Default.Encode((int)scalar);
            else if (scalar is DateTime)
                val = scalar.ToString();
            else if (scalar.GetType().IsEnum)
                val = scalar.ToString();
            else
                throw new InvalidOperationException(String.Format("XmlMsgWriter.WriteScalar can't handle type of {0}", scalar.GetType().ToString()));

            WriteScalarHelper(val);
        }

        private void WriteScalarHelper(String scalar)
        {
            scalar = String.IsNullOrEmpty(scalar) ? "null" : scalar;
            String listOnStack = ListNameOnStack();
            if (!String.IsNullOrEmpty(listOnStack))
            {
                // if it's a list, we need to write out the entire <name>value</name> business
                writer.WriteElementString(listOnStack, scalar);
            }
            else
            {
                // just write out the value - the element is already written.
                System.Diagnostics.Debug.Assert(stack.Peek().EntityType == XmlMessageWriterEntityType.Property);
                writer.WriteValue(scalar);
            }
        }
		
		public void WriteRaw (string data)
		{
			byte[] bytes = UTF8Encoding.UTF8.GetBytes(data);
			stm.Write (bytes, 0, bytes.Length);
		}

        public void OpenProperty(string name)
        {
            stack.Push(XmlMessageWriterEntity.Create(name, XmlMessageWriterEntityType.Property));
            writer.WriteStartElement(name);
        }

        public void CloseProperty()
        {
            stack.Pop();
            writer.WriteEndElement();
        }

        public void WriteProperty(string name, string scalarValue)
        {
            writer.WriteElementString(name, scalarValue);
        }

        public void WriteProperty(string name, int scalarValue)
        {
            writer.WriteElementString(name, scalarValue.ToString());
        }

        public void Flush()
        {
            writer.Flush();
        }

        #endregion
    }
}
