using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AgentDotNet
{
    public enum MessageType { Unicast, Broadcast, Revelation }
    public class Message
    {
        [JsonConstructor]
        public Message(string Nonce, string Session, string Body, MessageType Type, List<ScopeUri> RecieverScopes, List<AgentUri> RecieverAgents, AgentUri Sender)
        {
            _nonce = Nonce;
            _session = Session;            
            _body = Body;
            _type = Type;
            _recieverScopes = RecieverScopes;
            _recieverAgents = RecieverAgents;
            _sender = Sender;
        }
        public Message(AgentUri Sender, string Body, AgentUri Reciever, string Session = null)
        {
            _body = Body;
            _nonce = Guid.NewGuid().ToString();
            _session = Session ?? Guid.NewGuid().ToString();            
            _sender = Sender;
            _type = MessageType.Unicast;
            _recieverAgents = new List<AgentUri>() { Reciever };
        }
        public Message(string Body, AgentUri Reciever, Socket Socket = null, AgentUri Sender=null)
        {
            _body = Body;            
            _recieverAgents = new List<AgentUri>() { Reciever };
            this.Socket = Socket;
            _sender = Sender;
            _type = MessageType.Revelation;
        }
        public Message(AgentUri Sender, string Body, ScopeUri Recievers, string Session = null)
        {
            _body = Body;
            _nonce = Guid.NewGuid().ToString();
            _session = Session ?? Guid.NewGuid().ToString();
            _sender = Sender;
            _type = MessageType.Broadcast;            
            _recieverScopes = new List<ScopeUri>() { Recievers };
        }
        public Message(AgentUri Sender, string Body, List<AgentUri> Recievers, string Session = null)
        {
            _body = Body;
            _nonce = Guid.NewGuid().ToString();
            _session = Session ?? Guid.NewGuid().ToString();
            _sender = Sender;
            _type = MessageType.Unicast;
            _recieverAgents = Recievers;                        
        }
        public Message(AgentUri Sender, string Body, List<ScopeUri> Recievers, string Session = null)
        {
            _body = Body;
            _nonce = Guid.NewGuid().ToString();
            _session = Session ?? Guid.NewGuid().ToString();
            _sender = Sender;
            _type = MessageType.Broadcast;            
            _recieverScopes = Recievers;            
        }

        string _nonce;
        public string Nonce { get { return _nonce; } }

        string _session;
        public string Session { get { return _session; } }

        string _body;
        public string Body { get { return _body; } }

        MessageType _type;
        public MessageType Type { get { return _type; } }

        List<ScopeUri> _recieverScopes;
        public List<ScopeUri> RecieverScopes { get { return _recieverScopes; } }

        List<AgentUri> _recieverAgents;
        public List<AgentUri> RecieverAgents { get { return _recieverAgents; } }

        AgentUri _sender;
        public AgentUri Sender { get { return _sender; } }
        
        [JsonIgnore]
        public Socket Socket { get; set; }

        public void Reveal(bool throwException=false)
        {
            try
            {                
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new IPAddressConverter());
                settings.Converters.Add(new IPEndPointConverter());
                settings.Formatting = Formatting.Indented;
                var j = JsonConvert.SerializeObject(this, settings);
                byte[] buffer = Encoding.UTF8.GetBytes(j);

                if (_type == MessageType.Revelation)
                {
                    var agentUri = RecieverAgents[0];
                    if (Socket == null)
                    {
                        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        Socket.Connect(new IPEndPoint(agentUri.ScopeUri.ScopeAddress, agentUri.ScopeUri.ScopePort));
                    }
                    Socket.Send(buffer);
                }
                else throw new Exception("This is not a revelation");               
            }
            catch (Exception e)
            {
                if (throwException) throw e;
            }            
        }        
        public Message RecieveRevelation(int bufferSize = 1024 * 32)
        {
            byte[] buffer = new byte[bufferSize];            
            int size = Socket.Receive(buffer);
            Array.Resize(ref buffer, size);
            string s = Encoding.UTF8.GetString(buffer);

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPAddressConverter());
            settings.Converters.Add(new IPEndPointConverter());
            settings.Formatting = Formatting.Indented;

            Message m = JsonConvert.DeserializeObject<Message>(s, settings);
            if (m == null) return m;
            m.Socket = Socket;
            return m;
        }
        public async Task<Message> RecieveRevelationAsync(int bufferSize = 1024 * 32)
        {
            Message m = null;
            await Task.Run(() =>
            {
                m = RecieveRevelation();
            });
            return m;
        }
        public void Send(bool throwException=false)
        {
            try
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new IPAddressConverter());
                settings.Converters.Add(new IPEndPointConverter());
                settings.Formatting = Formatting.Indented;
                var j = JsonConvert.SerializeObject(this, settings);
                byte[] buffer = Encoding.UTF8.GetBytes(j);

                if (_type == MessageType.Broadcast)
                {
                    foreach (var scopeReciever in RecieverScopes)
                    {
                        var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sock.Connect(new IPEndPoint(scopeReciever.ScopeAddress, scopeReciever.ScopePort));
                        sock.Send(buffer);
                        sock.Disconnect(false);
                        sock.Close();
                    }
                }
                else
                {
                    foreach (var agentReciever in RecieverAgents)
                    {
                        var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sock.Connect(new IPEndPoint(agentReciever.ScopeUri.ScopeAddress, agentReciever.ScopeUri.ScopePort));
                        sock.Send(buffer);
                        sock.Disconnect(false);
                        sock.Close();
                    }
                }
            }
            catch(Exception e)
            {
                if (throwException) throw e;
            }
        }
        public Message CreateReply(AgentUri Sender, string Body)
        {
            if (_type == MessageType.Revelation)
            {
                return new Message(Body, _sender, Socket, Sender);
            }
            else
            {
                return new Message(Sender, Body, _sender, Session);
            }
        }        
    }
}
