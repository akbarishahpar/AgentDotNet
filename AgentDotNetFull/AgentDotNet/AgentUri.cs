using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AgentDotNet
{
    public class AgentUri
    {
        ScopeUri _scopeUri;
        string _agentName;

        [JsonConstructor]
        public AgentUri(string AgentName, ScopeUri ScopeUri)
        {
            _agentName = AgentName;
            _scopeUri = ScopeUri;
        }

        public AgentUri(string AgentName, IPAddress ScopeAddress, int ScopePort)
        {
            _agentName = AgentName;
            _scopeUri = new ScopeUri(ScopeAddress, ScopePort);
        }

        public AgentUri(string uri)
        {
            try
            {
                var sepration1 = uri.Split('@');
                _agentName = sepration1[0];

                string scopePart = sepration1.Length == 1 ? "local:def" : sepration1[1];
                if (scopePart == "undefiend") _scopeUri = null;
                else
                {
                    var sepration2 = scopePart.Split(':');
                    var _scopeAddress = IPAddress.Parse(sepration2[0] == "local" ? "127.0.0.1" : sepration2[0]);
                    var _scopePort = int.Parse(sepration2[1] == "def" ? "24000" : sepration2[1]);
                    _scopeUri = new ScopeUri(_scopeAddress, _scopePort);
                }
            }
            catch
            {
                throw new Exception("Invalid uri to parse");
            }
        }
        public ScopeUri ScopeUri { get { return _scopeUri; } }
        public string AgentName { get { return _agentName; } }
        public override string ToString()
        {
            if (ScopeUri == null) return $"{AgentName}@undefiend";
            else return $"{AgentName}@{_scopeUri.ScopeAddress.ToString()}:{_scopeUri.ScopePort}";
        }        
    }
}