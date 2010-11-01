//#define CLASS
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
    public class MessageWriterFactory
    {
        private static Dictionary<WebServiceFormat, Func<Stream, IMessageWriter>> funcMap =
            new Dictionary<WebServiceFormat, Func<Stream, IMessageWriter>>()
            {
                {WebServiceFormat.Json, JsonMessageWriter.Create},
                {WebServiceFormat.Xml, XmlMessageWriter.Create}
            };
        
        public static IMessageWriter Create(WebServiceFormat format, Stream stm)
        {
            return funcMap[format](stm);
        }
    }

    public enum JsonEntityType
    {
        Numeric = 0,
        String = 1,
        Object = 2,
        RootObject = 3,
        List = 4,
        Property = 5
    };

    public class JsonEntity
    {
        public JsonEntityType EntityType;
        public bool HasChildren;

        public static JsonEntity Create(JsonEntityType type)
        {
            JsonEntity This = new JsonEntity();
            This.EntityType = type;
            This.HasChildren = false;
            return This;
        }
    }

#if true
    public class JsonEntityStack : Stack<JsonEntity>
    {
        public bool HasParent
        {
            get
            {
                return this.Count > 0;
            }
        }

        public bool ParentHasChildren
        {
            get
            {
                return this.Count > 0 && this.Peek().HasChildren;
            }
            set
            {
                if (this.Count == 0)
                    return;

#if (CLASS) 
                this.Peek().HasChildren = value;
#else
                if (!this.Peek().HasChildren)
                {
                    JsonEntity parent = this.Pop();
                    parent.HasChildren = true;
                    this.Push(parent);
                }
#endif
            }
        }

        public bool ParentIsList
        {
            get
            {
                return this.Count > 0 && this.Peek().EntityType == JsonEntityType.List;
            }
        }
    }
#else
    public class JsonEntityStack
    {
        private int stackSize = 128;
        private JsonEntity[] data;
        private int index = -1;

        public JsonEntityStack()
        {
            data = new JsonEntity[stackSize];
        }

        public int Count
        {
            get
            {
                return index + 1;
            }
        }

        public bool HasParent
        {
            get
            {
                return (index == -1) ? false : true;
            }
        }

        public bool ParentHasChildren
        {
            get
            {
                if (index == -1)
                    return false;

                return data[index].HasChildren;
            }
            set
            {
                if (index != -1)
                    data[index].HasChildren = value;
            }
        }

        public void Push(JsonEntity item)
        {
            if (Count == stackSize)
            {
                int oldStackSize = stackSize;
                stackSize *= 2;
                JsonEntity[] newData = new JsonEntity[stackSize];
                for (int i = 0; i < oldStackSize; i++)
                    newData[i] = data[i];

                data = null;
                data = newData;
            }
            data[++index] = item;
        }

        public bool ParentIsList
        {
            get
            {
                return data[index].EntityType == JsonEntityType.List;
            }
        }

        public JsonEntity Pop()
        {
            return data[index--];
        }

        public JsonEntity Peek()
        {
            return data[index];
        }
    }
#endif

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

        public void Flush()
        {
            stm.Flush();
        }

        #endregion
    }

    // Change this to one struct with enum for type

    public enum XmlMessageWriterEntityType
    {
        List = 0,
        Property = 1
    };

    public struct XmlMessageWriterEntity
    {
        private XmlMessageWriterEntityType entityType;

        static public XmlMessageWriterEntity Create(String name, XmlMessageWriterEntityType type)
        {
            XmlMessageWriterEntity This = new XmlMessageWriterEntity();
            This.EntityType = type;
            This.Name = name;
            return This;
        }

        public String Name { get; set; }
        public XmlMessageWriterEntityType EntityType 
        {
            get
            {
                return this.entityType;         
            }
            set
            {
                this.entityType = value;
            }
        }
    }

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
