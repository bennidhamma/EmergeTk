using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Text;
using EmergeTk.Model;
using SimpleJson;

namespace EmergeTk.WebServices
{
	public static class XmlSerializer
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(XmlSerializer));
		
		public static string SerializeToXml(JsonArray list)
		{
			StringBuilder sb = new StringBuilder();
			StringWriter stringWriter = new StringWriter(sb);
			XmlTextWriter writer = new XmlTextWriter(stringWriter);
			SerializeXml( list, writer );
			return sb.ToString();
		}
		
		public static void SerializeToXml(JsonArray list, TextWriter writeStream)
		{
			XmlTextWriter writer = new XmlTextWriter(writeStream);
			SerializeXml( list, writer );
		}
		
		public static string SerializeToXml(JsonObject node)
		{
			return SerializeToXml(node,false);
		}
		
		public static string SerializeToXml(JsonObject node, bool format)
		{
			StringBuilder sb = new StringBuilder();
			StringWriter stringWriter = new StringWriter(sb);
			XmlTextWriter writer = new XmlTextWriter(stringWriter);
			if( format )
				writer.Formatting = Formatting.Indented;
			SerializeXml( node, writer );
			return sb.ToString();
		}
		
		public static void SerializeToXml(JsonObject node, TextWriter writeStream)
		{
			XmlTextWriter writer = new XmlTextWriter(writeStream);
			SerializeXml( node, writer );
			return;
		}
		
		private static void SerializeXml( JsonObject message, XmlWriter writer )
		{
			int numKeys = message.KeyCount;
			if( numKeys > 1 && ! string.IsNullOrEmpty( message.Name ) )
			{
				writer.WriteStartElement(message.Name);				
			}
			
			foreach( string k in message.GetKeys() )
			{	
				SerializeFieldXml(k, message[k], writer);
			}
			
			if( numKeys > 1 && ! string.IsNullOrEmpty( message.Name ) )
			{
				writer.WriteEndElement();	
			}
		}
		
		private static void SerializeXml(JsonArray list, XmlWriter writer)
		{
			writer.WriteStartElement(list.ListName);
			writer.WriteAttributeString("type","array");
			foreach( object o in list )
			{
				SerializeFieldXml(list.ItemName, o, writer);
			}
			writer.WriteEndElement();
		}
		
		private static void SerializeFieldXml( string name, object o, XmlWriter writer )
		{
			if( o is MessageNode )
			{
				SerializeXml( (MessageNode)o, writer );
			}
			else if(o is MessageList)
			{
				SerializeXml((MessageList)o, writer);
			}
			else
			{
				writer.WriteStartElement(name);
				if(o != null)
					writer.WriteString(o.ToString());
				writer.WriteEndElement();
			}
		}
		
		public static JsonObject DeserializeXml(string input)
		{
			//Root of XML is a MessageNode.
			//examine children - if there are two or more with same name, it is an array.
			//else it is a message node.
			//if there is only text below, it is a scalar value.
			
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(input);
				
			MessageNode root = DeserializeNode(doc.FirstChild);
			
			return root;
		}
		
		public static JsonObject DeserializeXml(Stream input)
		{
			//Root of XML is a MessageNode.
			//examine children - if there are two or more with same name, it is an array.
			//else it is a message node.
			//if there is only text below, it is a scalar value.
			
			XmlDocument doc = new XmlDocument();
			doc.Load(input);
				
			MessageNode root = DeserializeNode(doc.FirstChild);
			
			return root;
		}
		
		private static bool IsArray(XmlNode inNode)
		{
			if( inNode.HasChildNodes )
			{
				if( inNode.Attributes["type"] != null && inNode.Attributes["type"].Value == "array" )
					return true;
				else
					return false;
			}
			return false;
		}
		
		private static object ParseNodeProperty(XmlNode inNode )
		{
			object ret = null;
			//first, get the common case out of the way - is this a scalar property?
			if( inNode.HasChildNodes && inNode.ChildNodes.Count == 1 && inNode.FirstChild.NodeType == XmlNodeType.Text )
			{
				ret = inNode.InnerText;
			}
			else if( ! inNode.HasChildNodes )
			{
				ret = null;	
			}				
			//next, are we an array?
			else if( IsArray( inNode ) )
			{
				MessageList list = new MessageList();
				list.ListName = inNode.Name;
				foreach( XmlNode childNode in inNode )
				{
					//we assume homegoneous arrays.
					list.ItemName = childNode.Name;
					list.Add( ParseNodeProperty( childNode ) );
				}
				ret = list;
			}
			else // we are a MessageNode
			{
				ret = DeserializeNode( inNode );
			}
			return ret;
		}
		
		private static JsonObject DeserializeNode(XmlNode inNode)
		{
			MessageNode outNode = new MessageNode(inNode.Name);	
			
			//TODO: do we want to handle single text children here? or treat them like props?
			
			//if there are more than one child, and more than one of the same name, this is an array.
			
			foreach( XmlNode childNode in inNode.ChildNodes )
			{				
				outNode[childNode.Name] = ParseNodeProperty( childNode );
			}
			
			return outNode;
		}
	}
}
