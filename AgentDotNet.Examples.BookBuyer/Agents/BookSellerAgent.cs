using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDotNet.Examples.BookBuyer.Agents
{    
    public class BookSellerAgent : Agent
    {
        public List<Book> Books { get; set; }
        public BookSellerAgent(string Name):base(Name) { }

        protected override void Begin()
        {
            Thread.Sleep(100);
            Console.WriteLine($"Starting agent {Name}...");

            Say(JsonConvert.SerializeObject(new { Command = "register", ServiceName = "BookSeller", Uri = Uri.ToString() }), new AgentUri("ServiceProviderAgent"));

            while (true)
            {
                var message = Recieve();

                dynamic json = JObject.Parse(message.Body);
                if (json.Command == "query")
                {
                    var bookName = (string)json.BookName;
                    var book = Books.FirstOrDefault(q => q.Name == bookName);
                    if (book == null) message.CreateReply(Uri, JsonConvert.SerializeObject(new { Command = "not found" })).Send();
                    else message.CreateReply(Uri, JsonConvert.SerializeObject(new { Command = "price", BookName = book.Name, BookPrice = book.Price })).Send();
                }
                else if (json.Command == "accept")
                {
                    var bookName = (string)json.BookName;
                    var bookPrice = (decimal)json.BookPrice;
                    var book = Books.FirstOrDefault(q => q.Name == bookName && q.Price == bookPrice);
                    Books.Remove(book);
                    message.CreateReply(Uri, JsonConvert.SerializeObject(new { Command = "your welcome", BookName = bookName, BookPrice = bookPrice })).Send();
                }
                else message.CreateReply(Uri, "undefined request").Send();
            }            
        }

        protected override void Finish()
        {
            Console.WriteLine($"Shutting down agent {Name}...");
        }
    }
}
