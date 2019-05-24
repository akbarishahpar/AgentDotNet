using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDotNet.Examples.BookBuyer.Agents
{
    class BookBuyerAgent : Agent
    {
        public string BookToBuy { get; set; }

        public BookBuyerAgent(string Name, string BookToBuy) : base(Name)
        {
            this.BookToBuy = BookToBuy;
        }

        protected override void Begin()
        {
            Thread.Sleep(500);
            Console.WriteLine($"Starting agent {Name}...");
            var response = Ask(JsonConvert.SerializeObject(new { Command = "query", ServiceName = "BookSeller" }), new AgentUri("ServiceProviderAgent"), timeOut: 5000);
            if(response == null)
            {
                Console.WriteLine($"Agent {Uri} failed to do anything!");
                return;
            }
            var uris = JsonConvert.DeserializeObject<IEnumerable<string>>(response.Body);
            string targetUri = "";
            decimal targetPrice = decimal.MaxValue;
            foreach (var uri in uris)
            {
                Console.WriteLine($"Asking for {BookToBuy} from {uri}");
                var r = Ask(JsonConvert.SerializeObject(new { Command = "query", BookName = BookToBuy }), new AgentUri(uri), 5000);                
                if (r == null) Console.WriteLine($"{uri} has no such book");
                else
                {
                    dynamic bookInfo = JObject.Parse(r.Body);

                    if (bookInfo.Command == "not found")
                    {
                        Console.WriteLine($"{uri} has no such book");
                        continue;
                    }

                    string buyerUri = (string)r.Sender.ToString();
                    decimal bookPrice = (decimal)bookInfo.BookPrice;

                    if (bookPrice < targetPrice)
                    {
                        targetUri = buyerUri;
                        targetPrice = bookPrice;
                    }

                    Console.WriteLine($"{uri} has {bookInfo.BookName} with price {bookInfo.BookPrice}");
                }
            }

            if (!string.IsNullOrEmpty(targetUri))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"BestPrice={targetPrice}, BestSeller={targetUri.ToString()}");
                var r = Ask(JsonConvert.SerializeObject(new { Command = "accept", BookName = BookToBuy, BookPrice = targetPrice }), new AgentUri(targetUri), 5000);
                dynamic resultInfo = JObject.Parse(r.Body);
                string command = (string)resultInfo.Command;

                if (command == "your welcome")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Done!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("OOPS!");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Oops! Couldn't find anything!");
            }
        }

        protected override void Finish()
        {
            Console.WriteLine($"Shutting down agent {Name}...");
        }

    }
}
