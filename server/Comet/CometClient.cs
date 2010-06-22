using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;

namespace EmergeTk
{
    public class CometClient : Surface
    {
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(CometClient));		

		static byte[] XMLPolicyBytes = Encoding.ASCII.GetBytes(CometServer.XMLPolicy);
			//UTF8Encoding.GetBytes(XMLPolicy);
			//Encoding.ASCIIEncoding.GetBytes(XMLPolicy);
		string url;
        NameValueCollection querystring;
        Dictionary<string, string> headers = new Dictionary<string, string>(),
            cookies = new Dictionary<string,string>();
        Socket s;
        Context context;
        ICometWriter writer;

		static CometClient()
		{
			//ASCIIEncoding a = new ASCIIEncoding();
			
			//XMLPolicyBytes = a.GetBytes(XMLPolicy);
		}

        public CometClient(Socket s)
        {
            this.s = s;
        }

        public bool Connected { get { return s.Connected; } }

        public string CacheKey { get { return url; } }
        public Context Context { get { return context; } set { context = value; } }
        public Dictionary<string, string> Headers { get { return headers; } }
        public Dictionary<string, string> Cookies { get { return cookies; } }
        protected NetworkStream ns;
        protected BufferedStream bs;
        protected StreamReader sr;
        protected StreamWriter sw;
        public void Setup()
        {
			log.Debug("setting up comet client");
            ns = new NetworkStream(s, FileAccess.ReadWrite);
            bs = new BufferedStream(ns);
            sr = new StreamReader(ns);
			sw = new StreamWriter(bs);
			log.Debug("parsing request ");
            if( ! parseRequest() )
				return;
			log.Debug("reading headers");
            readHeaders();
			log.Debug("done reading headers");
			if( querystring == null || querystring["sid"] == null )
				return;
			context = Context.GetContext(querystring["sid"], CacheKey);
            if (context != null)
            {
            	log.Debug("found context for ", querystring["sid"] );
                this.Context = context;
                writer.Context = context;
                context.ConnectComet(this);
            }
            else
            {
            	
                Write("alert('Comet lost context.');");
                Shutdown();
            }
            writeSuccess();
        }

        public void Shutdown()
        {
			log.Debug("shutting socket down");
            ns.Close();
            s.Close();
            if (Context != null)
            {
                Context.DisconnectComet();
            }
        }

        public bool parseRequest()
        {
			log.Debug("in parse request");
			//char[] data = new char[1];
			//while( sr.ReadBlock( data, 0, 1 ) > 0 )
			//	log.Debug(data);
            String request = sr.ReadLine();
            log.Debug(
			    "parsing request",
				request, 
			    request == "<policy-file-request/>\0" );
            if( request == "<policy-file-request/>\0" )
			{
				//writer = new FlashCometWriter(sw,ns);
				log.Debug("is a policy file request" );
				s.Send(XMLPolicyBytes);
				s.Close();
				log.Debug("done sending request");
				
            	//throw new Exception("reading policy headers");
				return false;
			}
            string[] tokens = request.Split(new char[] { ' ' });
            log.Debug("parsing request", request, tokens );
            if( tokens.Length > 1 && tokens[1].Length > 1 )
            {
            	url = tokens[1].Substring(1);
            	if( url.IndexOf('?') > -1 )
            	{
            		querystring = System.Web.HttpUtility.ParseQueryString( url.Substring( url.IndexOf('?') + 1 ) );
            	}
            }
            if( querystring == null )
            {
            	log.Error("no querystring was found for a comet connect request");
            	return false;
            }
            
            if (querystring["flash"] == "1" )
            {
                writer = new FlashCometWriter(sw,ns);
                StateObject so = new StateObject();
                so.workSocket = s;
                s.BeginReceive(so.buffer,0,so.buffer.Length, SocketFlags.None, new AsyncCallback(Receive), so);
            }
            else
            {
                writer = new HtmlCometWriter(sw);
                writer.Context = context;
            }
         	return true;  
        }

        string lastInput;
        int sameCount = 0;

        public void Receive(IAsyncResult ar)
        {
            StateObject so = ar.AsyncState as StateObject;

            if (!so.workSocket.Connected)
            {
                return;
            }

            try
            {
                int length;
                for (length = 0; length < so.buffer.Length; length++)
                {
                    if (so.buffer[length] == 0)
                        break;
                }
                string input = new string(Encoding.ASCII.GetChars(so.buffer, 0, length));
                if (input == lastInput)
                {
                    sameCount++;
                }
                else
                {
                    sameCount = 0;
                    lastInput = input;
                }
                so.workSocket.EndReceive(ar);
                
                if ( !so.workSocket.Connected || sameCount > 10 )
                {
                    context.Unregister();
                    Shutdown();
                    return;
                }
                
                Dictionary<string,object> data = (Dictionary<string,object>)JSON.Default.Decode( input );
                
                log.Debug( "recevied data", data );
                so.buffer.Initialize();
                so.workSocket.BeginReceive(so.buffer, 0, so.buffer.Length, SocketFlags.None, new AsyncCallback(Receive), so);
                if( data != null )
                	context.Transform( (string)data["id"], (string)data["evt"], (string)data["arg"] );
            }
            catch( Exception e )
            {
            	log.Error("Error receiving data", Util.BuildExceptionOutput(e) );
            }
        }

        public void readHeaders()
        {
        	try
        	{
	            String line;
	            while ((line = sr.ReadLine()) != null && line != "")
	            {
	                string[] tokens = line.Split(new char[] { ':' });
	                String name = tokens[0].Trim();
	                String value = "";
	                for (int i = 1; i < tokens.Length; i++)
	                {
	                    value += tokens[i].Trim();
	                    if (i < tokens.Length - 1) tokens[i] += ":";
	                }
	                headers[name] = value;
	                if (name == "Cookie")
	                {
	                    string[] cookieTokens = value.Split(new char[] { '=', ';' },StringSplitOptions.RemoveEmptyEntries);
	                    for (int i = 0; i < cookieTokens.Length; i += 2)
	                    {
	                    	string cookieName = cookieTokens[i].Trim();
	                    	string cookieValue = cookieTokens[i+1].Trim();
	                        cookies[cookieName] = cookieValue;
	                    }
	                }
	            }
	       	}
	       	catch( Exception e )
	       	{
	       		log.Error("Error Reading headers: " + Util.BuildExceptionOutput(e ) );
	       	}
        }

        public virtual void writeSuccess()
        {
            writer.WriteSuccess();
        }

        public override void Write(string data)
        {
        	if( sb == null )
        		sb = new StringBuilder();
	        sb.Append( data );
        }
        
        StringBuilder sb;

		public override void End ()
		{			
			base.End ();
			
			writer.Write( sb.ToString().Replace(@"\",@"\\") );
	       	sb = new StringBuilder();
		}

        // State object for reading client data asynchronously
        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }
    }
}
