
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
    public class JsonMessageWriter : IMessageWriter
    {
        private Stream stm;
        private JsonEntityStack current = new JsonEntityStack();

        public void WriteToStream(String s)
        {
            stm.Write(UTF8Encoding.Default.GetBytes(s), 0, s.Length);
        }

        static public JsonMessageWriter Create(Stream stm)
        {
            if (stm == null)
            {
                throw new ArgumentException("JsonMessageWriter.Create called with null Stream argument");
            }
            JsonMessageWriter This = new JsonMessageWriter();
            This.stm = stm;
            return This;
        }

        #region IMessageWriter Members

        public void OpenRoot(string name)
        { 
            WriteToStream("{");
            current.Push(JsonEntity.Create(JsonEntityType.RootObject));
        }

        public void CloseRoot()
        {
            WriteToStream("}");
            current.Pop();
            this.Flush();
        }

        public void OpenObject()
        {
            JsonEntity obj = JsonEntity.Create(JsonEntityType.Object);
            if (!current.ParentHasChildren)
            {
                current.ParentHasChildren = true;
                WriteToStream("{");
            }
            else
                WriteToStream(",{");

            current.Push(obj);
        }

        public void CloseObject()
        {
            WriteToStream("}");
            current.Pop();
        }

        public void OpenList(string name)
        {
            JsonEntity list = JsonEntity.Create(JsonEntityType.List);
            if (current.HasParent)
            {
                if (current.ParentHasChildren)
                    WriteToStream(",");
                else
                    current.ParentHasChildren = true;
            }
            WriteToStream("[");
            current.Push(list);
        }

        public void CloseList()
        {
            WriteToStream("]");
            current.Pop();
        }

        public void OpenProperty(string name)
        {
            name = Util.Quotize(name);
            if (current.HasParent)
            {
                if (current.ParentHasChildren)
                    WriteToStream(",");
                else
                    current.ParentHasChildren = true;

                if (!current.ParentIsList)
                    WriteToStream(name + ":");
            }
            current.Push(JsonEntity.Create(JsonEntityType.Property));
        }

        public void CloseProperty()
        {
            current.Pop();
        }

        public void WriteScalar(string scalar)
        {
            String value = JSON.Default.Encode(scalar);
            WriteScalarHelper(value);
        }

        public void WriteScalar(int scalar)
        {
            WriteScalarHelper(scalar.ToString());
        }

        public void WriteScalar(bool scalar)
        {
            WriteScalarHelper(scalar ? "true" : "false");
        }

        public void WriteScalar(double scalar)
        {
            WriteScalarHelper(scalar.ToString());
        }

        public void WriteScalar(float scalar)
        {
            WriteScalarHelper(scalar.ToString());
        }

        public void WriteScalar(DateTime scalar)
        {
            WriteScalarHelper(Util.ToJavaScriptString(scalar.ToString()));
        }

        public void WriteScalar(Decimal scalar)
        {
            WriteScalarHelper(scalar.ToString());
        }

        public void WriteScalar(Object scalar)
        {
            String value = JSON.Default.Encode(scalar);
            WriteScalarHelper(value);
        }

        protected void WriteScalarHelper(string scalar)
        {
            if (!current.ParentHasChildren)
            {
                WriteToStream(scalar);
                current.ParentHasChildren = true;
            }
            else
            {
                WriteToStream("," + scalar);

            }
        }

        public void WriteProperty(string name, string scalarValue)
        {
            WritePropertyHelper(Util.Quotize(name) + ":" + JSON.Default.Encode(scalarValue));
        }

        public void WriteProperty(string name, int scalarValue)
        {
            WritePropertyHelper(Util.Quotize(name) + ":" + scalarValue);
        }

        private void WritePropertyHelper(string prop)
        {
            if (!current.ParentHasChildren)
            {
                WriteToStream(prop);
                current.ParentHasChildren = true;
            }
            else
            {
                WriteToStream("," + prop);
            }
        }
		
		public void WriteRaw (string data)
		{
			WriteToStream (data);
		}

        public void Flush()
        {
            stm.Flush();
        }

        #endregion
    }
}
