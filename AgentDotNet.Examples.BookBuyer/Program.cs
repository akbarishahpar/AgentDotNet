using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDotNet.Examples.BookBuyer
{
    class Program
    {
        static void Main(string[] args)
        {
            var scope = new Scope(Agents: new List<Agent>
            {
                new Agents.ServiceProviderAgent("ServiceProviderAgent"),
                
                new Agents.BookSellerAgent("SellerAgent1")
                {
                    Books = new List<Book>
                    {
                        new Book{ Name = "Harry potter", Price = 25000 },
                        new Book{ Name = "Lord of the rings", Price = 50000 },
                        new Book{ Name = "The hobbit", Price = 45000 }
                    }
                },

                new Agents.BookSellerAgent("SellerAgent2")
                {
                    Books = new List<Book>
                    {
                        new Book{ Name = "Harry potter", Price = 25500 },
                        new Book{ Name = "Lord of the rings", Price = 49000 },
                    }
                }
            });

            scope.Start();

            var bookBuyerAgent = new Agents.BookBuyerAgent("BookBuyerAgent", "Harry potter");
            bookBuyerAgent.Bind(scope);
            bookBuyerAgent.Start();

            Thread.Sleep(2000);
            Console.ResetColor();
            var bookBuyerAgent2 = new Agents.BookBuyerAgent("BookBuyerAgent2", "Harry potter");
            bookBuyerAgent2.Bind(scope);
            bookBuyerAgent2.Start();

            Console.ReadKey();
        }
    }
}
