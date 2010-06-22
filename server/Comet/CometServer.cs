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
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Web;
using System.Threading;
using System.Configuration;

namespace EmergeTk
{
    public class CometServer
    {
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(CometServer));	
		
        public static int PortNumber 
		{ 
			get 
    		{
				try
				{
    				if( ConfigurationManager.AppSettings != null || ConfigurationManager.AppSettings["cometPortNumber"] == null )
    					return 0;
    				return int.Parse(ConfigurationManager.AppSettings["cometPortNumber"]); 
				}
				catch( Exception )
				{
					return 0;
				}
			}
		}
        static CometServer singleton = new CometServer(PortNumber);
        public static CometServer Singleton 
		{ get 
			{ 
				try
				{
        			if( ConfigurationManager.AppSettings["cometPortNumber"] == null ) return null;
        			return singleton; 
				} 
				catch(Exception )
				{
					return null;	
				}
			}
		}
        static object listening = (object)0;
        static CometServer()
        {
            if (listening.Equals(0))
            {
                lock (listening)
                {
                    if (listening.Equals(0))
                    {
                        Thread t = new Thread(singleton.Listen);
                        t.Start();
                        listening = 1;
                        AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
						
						Thread ft = new Thread(singleton.FlashListen);
                        ft.Start();
                    }
                }
            }            
        }

        static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            singleton.Shutdown();
        }

        private int port;

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        private CometServer(int port)
        {
            this.port = port;
			System.Text.ASCIIEncoding a = new System.Text.ASCIIEncoding();
			XMLPolicyBytes = a.GetBytes(XMLPolicy);
        }

        private CometServer() : this(81) { }

        Socket listener;
		Socket flashPolicyServer;
        ~CometServer()
        {
            if (listener != null)
            {
                listener.Close();
            }
			if( flashPolicyServer != null )
			{
				flashPolicyServer.Close();
			}
        }

		public const string XMLPolicy =
"<cross-domain-policy><allow-access-from domain=\"*\" to-ports=\"*\" /></cross-domain-policy>\0";
		byte[] XMLPolicyBytes; //ASCIIEncoding.GetBytes(XMLPolicy);

		public void FlashListen()
		{
			flashPolicyServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint flashEnd = new IPEndPoint( IPAddress.Any, 6667 );
			try
			{
				log.Debug("binding flash policy socket on port 6667");
				flashPolicyServer.Bind(flashEnd);
			}
			catch( Exception e )
            {
				log.Error("flash policy server bind error", Util.BuildExceptionOutput(e) );
                return;
            }

			flashPolicyServer.Blocking = true;
			flashPolicyServer.Listen(-1);

			while (true)
            {
                Socket s = null;
                try
                {
                    s = flashPolicyServer.Accept();
                }
                catch(Exception e)
                {
                	log.Error(Util.BuildExceptionOutput(e));
                    Thread.Sleep(1000);
                    continue;
                }

                try
                {
					log.Debug("policy request received.  sending cross domain policy");
                    s.Send( XMLPolicyBytes );
					s.Close();
                }
                catch(Exception e)
                {
                	log.Error(Util.BuildExceptionOutput(e));
                    if( s != null )
                        s.Close();  
                }
            }
		}

        public void Listen()
        {
			//test
			listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (port == 0)
                port = PortNumber;
            IPEndPoint end = new IPEndPoint(IPAddress.Any, port);
            
			try
            {
				log.Debug("binding comet server on port ", port );
                listener.Bind(end);
            }
            catch( Exception e )
            {
				log.Error("bind error", Util.BuildExceptionOutput(e) );
                return;
            }

			listener.Blocking = true;
            listener.Listen(-1);
            while (true)
            {
                Socket s = null;
                try
                {
                    s = listener.Accept();
                }
                catch(Exception e)
                {
                	log.Error(Util.BuildExceptionOutput(e));
                    Thread.Sleep(1000);
                    continue;
                }

                try
                {
                    CometClient c = new CometClient(s);
                    c.Setup();                    
                }
                catch(Exception e)
                {
                	log.Error(Util.BuildExceptionOutput(e));
                    if( s != null )
                        s.Close();  
                }
            }
        }

        public void Shutdown()
        {
			if( listener != null )
			{
            	listener.Close();
            	listener = null;
			}
			if( flashPolicyServer != null )
			{
				flashPolicyServer.Close();
				flashPolicyServer = null;
			}
        }
    }
}
