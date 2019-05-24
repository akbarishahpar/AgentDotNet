using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AgentDotNet
{
    public class ScopeUri
    {
        IPAddress _scopeAddress;
        int _scopePort;        

        [JsonConstructor]
        public ScopeUri(IPAddress ScopeAddress, int ScopePort)
        {            
            _scopeAddress = ScopeAddress;
            _scopePort = ScopePort;
        }
        public ScopeUri(string uri)
        {
            try
            {                
                var sepration = uri.Split(':');                
                _scopeAddress = IPAddress.Parse(sepration[0]);
                _scopePort = int.Parse(sepration[1]);
            }
            catch
            {
                throw new Exception("Invalid Uri to parse");
            }
        }
        public IPAddress ScopeAddress { get { return _scopeAddress; } }
        public int ScopePort { get { return _scopePort; } }
        public override string ToString()
        {
            return $"{_scopeAddress.ToString()}:{_scopePort}";
        }

        //public override bool Equals(object obj)
        //{
        //    var that = obj as ScopeUri;
        //    if (that == null) return false;
        //    if (this.ScopeAddress.ToString() != that.ScopeAddress.ToString()) return false;
        //    if (this.ScopePort != that.ScopePort) return false;
        //    return true;
        //}
    }
}
