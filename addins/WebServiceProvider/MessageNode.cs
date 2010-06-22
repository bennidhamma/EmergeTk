using System;
using System.Collections;
using System.Collections.Generic;
using EmergeTk;

namespace EmergeTk.WebServices
{
	public class MessageNode : IJSONSerializable
	{
		//private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(MessageNode));
		private Dictionary<string,object> hash = new Dictionary<string, object>();
		public string Name { get; set; }
		
		public object this[string key]
		{
			get
			{
				return hash[key];	
			}
			set
			{
				hash[key] = value;
			}
		}

		public MessageNode()
		{
		}
		
		public MessageNode(string name)
		{
			Name = name;
		}		
		
		public IEnumerable<string> GetKeys()
		{
			foreach( string k in this.hash.Keys )
				yield return k;
		}
		
		public int KeyCount
		{
			get { return this.hash.Keys.Count; }
		}
		
		public bool ContainsKey(string key)
		{
			return hash.ContainsKey(key);	
		}
		
		public void Remove(string key)
		{
			hash.Remove(key);
		}

        public Dictionary<String, object> Hash
        {
            get
            {
                return hash;
            }
        }
		
		#region IJSONSerializable implementation
		public Dictionary<string, object> Serialize ()
		{
			/* JeffW feels that enclosing nodes in Name element wrappers (to transpose more closely to xml.)
			 * is excessive.  we could also add a reserved field, __name__ or some such to the object and provide 
			 * this there.
			 * Dictionary<string,object> output = null;
			if( ! string.IsNullOrEmpty( Name ) )
			{
				output = new Dictionary<string, object>();
				output[Name] = hash;
			}
			else
				output = hash;
			return output;
			*/
			return hash;
		}		
		
		public void Deserialize (Dictionary<string, object> json)
		{
			//if there is only 1 item in this json, we are dealing with a named element.
			//unpack as the name and get 
			throw new NotImplementedException();
		}
		
		public bool IsDeserializing { get; set; }
		#endregion
		
		public static MessageNode ConvertFromRaw( Dictionary<string,object> input )
		{
			MessageNode node = new MessageNode(null);
            if (input != null)
            {
                node.hash = new Dictionary<string, object>(input);
                foreach (string k in input.Keys)
                {
                    object o = input[k];
                    if (o is Dictionary<string, object>)
                        node.hash[k] = ConvertFromRaw((Dictionary<string, object>)input[k]);
                    else if (o is IList)
                        node.hash[k] = MessageList.ConvertFromRaw((IList)input[k]);
                }
            }
            else
            {
                node.hash = new Dictionary<string, object>();
            }
			
			return node;
		}
	}
}
