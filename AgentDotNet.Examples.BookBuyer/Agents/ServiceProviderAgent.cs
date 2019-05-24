using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentDotNet.Examples.BookBuyer.Agents
{
    class ServiceProviderAgent : Agent
    {
        public List<ServiceRecord> ServiceRecords { get; set; }
        public ServiceProviderAgent(string Name) : base(Name)
        {
            ServiceRecords = new List<ServiceRecord>();
        }

        protected override void Begin()
        {
            Console.WriteLine($"Starting agent {Name}...");
            while(true)
            {
                var message = Recieve();
                dynamic json = JObject.Parse(message.Body);
                if(json.Command == "register")
                {                    
                    var serviceName = (string)json.ServiceName;
                    var uri = (string)json.Uri;

                    Console.WriteLine($"{uri} registered as {serviceName}");

                    if (ServiceRecords.Any(q => q.ServiceName == serviceName && q.Uri.ToString() == Uri.ToString())) continue;
                    else
                    {
                        ServiceRecords.Add(new ServiceRecord
                        {
                            ServiceName = serviceName,
                            Uri = uri
                        });
                    }
                }
                else if(json.Command == "remove")
                {
                    var serviceName = (string)json.ServiceName;
                    var Uri = (string)json.Uri;
                    ServiceRecords.RemoveAll(q => q.ServiceName == serviceName && q.Uri.ToString() == Uri.ToString());
                }
                else if(json.Command == "query")
                {
                    var serviceName = (string)json.ServiceName;
                    var Uris = ServiceRecords.Where(q => q.ServiceName == serviceName).Select(q => q.Uri.ToString()).Distinct();
                    message.CreateReply(Uri, JsonConvert.SerializeObject(Uris)).Send();
                }
            }
        }
        protected override void Finish()
        {
            Console.WriteLine($"Shutting down agent {Name}...");
        }

    }
}
