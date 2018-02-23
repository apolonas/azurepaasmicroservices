using Newtonsoft.Json;
using OnlineShop.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine("You have the following options:");
            Console.WriteLine();
            Console.WriteLine(" 1. Enter \"order --async\" to place an order and not wait for a response");
            Console.WriteLine(" 2. Enter \"order --response\" to place an order and wait for a response");

            try
            {
                await ProcessInput();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            Console.Read();
        }

        private static async Task ProcessInput()
        {
            var command = Console.ReadLine();

            if (command == "order --async")
            {
                var order = await Task.FromResult<Order>(CreateOrder(requiresResponse: false));

                Console.WriteLine();
                Console.WriteLine($"Sending async order with id: {order.Id}");

                var httpRequestSender = new HttpRequestSender();
                await httpRequestSender.PlaceOrderAsync(order);

                Console.WriteLine($"Order with id: {order.Id} sent");
                Console.WriteLine();
                Console.WriteLine("Place another order using the same command options.");

                await ProcessInput();
            }
            else if (command == "order --response")
            {
                var order = await Task.FromResult<Order>(CreateOrder(requiresResponse: true));

                Console.WriteLine();
                Console.WriteLine($"Sending order with id: {order.Id} and waiting for response.");

                var httpRequestSender = new HttpRequestSender();
                var response = await httpRequestSender.PlaceOrderAsyncWaitForResponse(order);

                var orderResponseString = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(orderResponseString);

                Console.WriteLine($"Order with id: {orderResponse.OrderId} finished processing with status {orderResponse.Status}");
                Console.WriteLine();
                Console.WriteLine("Place another order using the same command options.");

                await ProcessInput();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Invalid command. Try again.");
                await ProcessInput();
            }
        }

        private static Order CreateOrder(bool requiresResponse)
        {
            var customer = new Customer()
            {
                Id = Guid.Parse("1924ca3c-2dc2-487b-9541-b31b1ebcd03f"),
            };

            var order = new Order()
            {
                Id = Guid.NewGuid(),
                Status = "Initiated",
                Customer = customer,
                RequiresResponse = requiresResponse,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Products = new List<Product>()
                {
                    new Product()
                    {
                        Id = Guid.Parse("3928f991-571f-4342-a1d6-adf47427f94e"),
                        SKU = Guid.Parse("f79792c4-b2c7-4001-b7b8-e4fc5379bf09"),
                        Category = "Soccer Shoes"
                    },
                    new Product()
                    {
                        Id = Guid.Parse("be7004c7-f8fb-45f4-8ea8-e4dbb3f469b4"),
                        SKU = Guid.Parse("1014dd1b-37ad-47d3-a238-fa36bf3eb512"),
                        Category = "Soccer Apparel"
                    },
                    new Product()
                    {
                        Id = Guid.Parse("a0ec88be-db8d-4c48-a3a5-51ed1ed88dc3"),
                        SKU = Guid.Parse("cff46b65-172b-43d3-9e0a-667905d84d72"),
                        Category = "Soccer Apparel"
                    }
                },
                Payment = new Payment()
                {
                    Customer = customer,
                    CreditCardNumber = "1234123412341234",
                    ExpirationDate = "01/2019",
                    SecurityCode = "007"
                }
            };

            return order;
        }
    }
}
