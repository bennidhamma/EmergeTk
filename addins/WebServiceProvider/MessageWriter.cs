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
                {WebServiceFormat.Xml, XmlMessageWriter.Create},
			  	{WebServiceFormat.Csv, CsvMessageWriter.Create}
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


}
