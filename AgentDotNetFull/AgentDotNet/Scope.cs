using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDotNet
{
    public enum ScopeState { Stopped, Started }
    public class Scope
    {
        public readonly object agentsLock = new object();
        public readonly int port;
        public readonly IPAddress globalAddress;
        public readonly IPAddress listenerAddress;
        public readonly int bufferSize;
        Socket mainSock;

        ScopeState _scopeState = ScopeState.Stopped;
        public ScopeState State { get { return _scopeState; } }

        public ScopeUri Uri
        {
            get
            {
                return new ScopeUri(globalAddress, port);
            }
        }

        Dictionary<string, Agent> agents = new Dictionary<string, Agent>();        

        IPAddress selectGlobalAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                if (host.AddressList == null || host.AddressList.Length == 0) throw new Exception("No network adapters with an IPv4 address in the system!");
                var address = host.AddressList.Where(q => q.AddressFamily == AddressFamily.InterNetwork).OrderBy(q => isLocalIpAddress(q.ToString())).FirstOrDefault();
                if (address == null) throw new Exception("No address can be found");
                else return address;
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;                 
                }
                else throw e;
            }
        }

        bool isLocalIpAddress(string ipAddress)
        {
            try
            {
                int[] ipParts = ipAddress.Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(s => int.Parse(s)).ToArray();
                // in private ip range
                if (ipParts[0] == 10 ||
                    (ipParts[0] == 192 && ipParts[1] == 168) ||
                    (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31)))
                {
                    return true;
                }

                // IP Address is probably public.
                // This doesn't catch some VPN ranges like OpenVPN and Hamachi.
                return false;
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return false;
                }
                else throw e;
            }
        }

        public Scope(IPAddress GlobalAddress=null, int Port=24000, int BufferSize=1024*32, List<Agent> Agents = null, IPAddress ListenerAddress=null)
        {
            globalAddress = GlobalAddress ?? selectGlobalAddress();
            listenerAddress = ListenerAddress ?? IPAddress.Any;
            port = Port;
            bufferSize = BufferSize;

            if(Agents != null)
            {
                foreach (var agent in Agents)
                {
                    agent.Bind(this);
                }
            }
        }

        public delegate void OnStartDel();
        public OnStartDel OnStart;

        public delegate void OnStopDel();
        public OnStopDel OnStop;

        Thread mainThread;
        public Scope Start(bool singleThread=false)
        {
            try
            {
                _scopeState = ScopeState.Started;
                foreach (KeyValuePair<string, Agent> item in agents)
                {
                    item.Value.Start();
                }

                if (singleThread)
                {
                    OnStart?.Invoke();
                    Listen();
                    OnStop?.Invoke();
                    return this;
                }
                else
                {
                    ThreadStart starter = Listen;
                    starter += () =>
                    {
                        _scopeState = ScopeState.Stopped;
                        if (OnStop != null) new Thread(new ThreadStart(OnStop));
                    };
                    mainThread = new Thread(starter) { IsBackground = true };
                    mainThread.Start();
                    _scopeState = ScopeState.Started;
                    if (OnStart != null) new Thread(new ThreadStart(OnStart));
                    return this;
                }
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }

        public delegate void OnExceptionThrownDel(Scope sender, Exception e);
        public event OnExceptionThrownDel OnExceptionThrown;
        void Listen()
        {
            try
            {
                mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mainSock.Bind(new IPEndPoint(listenerAddress, port));
                mainSock.Listen(-1);
                while (_scopeState == ScopeState.Started)
                {
                    Thread.Sleep(1);

                    Socket senderSock = null;
                    try
                    {
                        senderSock = mainSock.Accept();
                    }
                    catch
                    {
                        break;
                    }

                    if (_scopeState != ScopeState.Started) break;

                    byte[] buffer = new byte[bufferSize];
                    int size = senderSock.Receive(buffer);
                    Array.Resize(ref buffer, size);
                    string s = Encoding.UTF8.GetString(buffer);

                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new IPAddressConverter());
                    settings.Converters.Add(new IPEndPointConverter());
                    settings.Formatting = Formatting.Indented;

                    Message m = JsonConvert.DeserializeObject<Message>(s, settings);
                    m.Socket = senderSock;
                    CheckSock(m.Socket);
                    if (m.Type == MessageType.Broadcast)
                    {
                        foreach (KeyValuePair<string, Agent> item in agents)
                        {
                            if (item.Value.State == AgentState.Stopped) continue;
                            item.Value.PushMessage(m);
                        }
                    }
                    else
                    {
                        foreach (var reciever in m.RecieverAgents)
                        {
                            var agent = agents[reciever.AgentName];
                            if (agent.State == AgentState.Started) agent.PushMessage(m);
                        }
                    }
                    if(m.Type != MessageType.Revelation) senderSock.Close();
                }
                mainSock.Close();
            }
            catch(Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }        
        void CheckSock(Socket sock)
        {
            var x = sock;            
        }
        public void Stop()
        {
            try
            {
                foreach (KeyValuePair<string, Agent> item in agents)
                {
                    item.Value.Halt();
                }
                mainSock.Close();

                mainThread.Abort();                                

                _scopeState = ScopeState.Stopped;
            }            
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        
        public void RegisterAgent(Agent Agent)
        {
            try
            {
                if (agents.ContainsKey(Agent.Name)) throw new Exception("Duplicate agent name");
                if (Agent.Scope == null) throw new Exception("Null Scope for agent is not permitted");
                lock (agentsLock)
                {
                    agents.Add(Agent.Name, Agent);
                }
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        public void UnRegisterAgent(string Name)
        {
            try
            {
                lock (agentsLock)
                {
                    agents.Remove(Name);
                }
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }       
    }
}
