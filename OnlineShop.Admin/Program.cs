using Microsoft.Azure;
using Microsoft.Azure.ServiceBus;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using OnlineShop.Admin.TableEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Admin
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Press enter to setup Azure resources.");
            Console.ReadLine();

            await CreateSubscriptionRules();
            Console.WriteLine($"Filtered Subscriptions Created");

            //await SeedTables();
            //Console.WriteLine($"Tables Seeded");

            Console.ReadKey();
        }

        private static async Task SeedTables()
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureStorageConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();

            //await SeedCustomers(storageAccount, tableClient);
            await SeedProducts(storageAccount, tableClient);
        }

        private static async Task CreateSubscriptionRules()
        {
            try
            {
                var processPaymentInventoryCheckedClient = new SubscriptionClient(CloudConfigurationManager.GetSetting("ServiceBusConnectionString"),
                                                                     CloudConfigurationManager.GetSetting("InventoryCheckedTopic"),
                                                                     CloudConfigurationManager.GetSetting("ProcessPaymentSubscription"));

                var sendEmailInventoryCheckedClient = new SubscriptionClient(CloudConfigurationManager.GetSetting("ServiceBusConnectionString"),
                                                                     CloudConfigurationManager.GetSetting("InventoryCheckedTopic"),
                                                                     CloudConfigurationManager.GetSetting("SendEmailSubscription"));

                var updateInventoryPaymentProcessedClient = new SubscriptionClient(CloudConfigurationManager.GetSetting("ServiceBusConnectionString"),
                                                                    CloudConfigurationManager.GetSetting("PaymentProcessedTopic"),
                                                                    CloudConfigurationManager.GetSetting("UpdateInventorySubscription"));

                await CleanUpRules(processPaymentInventoryCheckedClient, "$Default");
                await AddRule(processPaymentInventoryCheckedClient, "Verified", "VerifiedInventory-ProcessPayment");

                await CleanUpRules(sendEmailInventoryCheckedClient, "$Default");
                await AddRule(sendEmailInventoryCheckedClient, "Insufficient", "InsufficientInventory-SendEmail");

                await CleanUpRules(updateInventoryPaymentProcessedClient, "$Default");
                await AddRule(updateInventoryPaymentProcessedClient, "Verified", "VerifiedPayment-UpdateInventory");

                await processPaymentInventoryCheckedClient.CloseAsync();
                await sendEmailInventoryCheckedClient.CloseAsync();
                await updateInventoryPaymentProcessedClient.CloseAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static async Task CleanUpRules(SubscriptionClient client, string ruleName)
        {
            var rules = await client.GetRulesAsync();

            if (rules.FirstOrDefault(r => r.Name == ruleName) != null)
            {
                await client.RemoveRuleAsync(ruleName);
            }
        }

        private static async Task AddRule(SubscriptionClient client, string correlationFilter, string ruleName)
        {
            var rules = await client.GetRulesAsync();

            if (rules.FirstOrDefault(r => r.Name == ruleName) == null)
            {
                await client.AddRuleAsync(new RuleDescription
                {
                    Filter = new CorrelationFilter(correlationFilter),
                    Name = ruleName
                });
            }
        }

        private static async Task SeedCustomers(CloudStorageAccount cloudStorageAccount, CloudTableClient tableClient)
        {
            try
            {
                var customerEntityOne = new CustomerEntity("USA-IL", Guid.NewGuid().ToString())
                {
                    FirstName = "Customer",
                    LastName = "One"
                };

                var customerEntityTwo = new CustomerEntity("USA-IL", Guid.NewGuid().ToString())
                {
                    FirstName = "Customer",
                    LastName = "Two"
                };

                var customerEntityThree = new CustomerEntity("USA-IL", Guid.NewGuid().ToString())
                {
                    FirstName = "Customer",
                    LastName = "Three"
                };

                var customers = new List<CustomerEntity>();
                customers.Add(customerEntityOne);
                customers.Add(customerEntityTwo);
                customers.Add(customerEntityThree);

                foreach (var customer in customers)
                {
                    var customerTable = tableClient.GetTableReference("Customer");
                    var insertCustomerOperation = TableOperation.Insert(customer);
                    await customerTable.ExecuteAsync(insertCustomerOperation);
                }
            }
            catch (Exception ex)
            {
                //Log the exception and re-throw
                throw ex;
            }
        }

        private static async Task SeedProducts(CloudStorageAccount cloudStorageAccount, CloudTableClient tableClient)
        {
            try
            {
                var barcaJerseySKU = Guid.NewGuid().ToString();
                var barcaShortsSKU = Guid.NewGuid().ToString();
                var adidasShoesSKU = Guid.NewGuid().ToString();
                var liverpoolJerseySKU = Guid.NewGuid().ToString();
                var liverpoolShortsSKU = Guid.NewGuid().ToString();
                var nikeShoesSKU = Guid.NewGuid().ToString();

                var products = new List<ProductEntity>();

                var barcaJersey = new ProductEntity(barcaJerseySKU, Guid.NewGuid().ToString())
                {
                    Category = "Soccer Apparel",
                    Name = "Barcelona Jersey",
                    Price = 29.75D
                };

                var barcaShorts = new ProductEntity(barcaShortsSKU, Guid.NewGuid().ToString())
                {
                    Category = "Soccer Apparel",
                    Name = "Barcelona Shorts",
                    Price = 19.99D
                };

                var adidasShoes = new ProductEntity(adidasShoesSKU, Guid.NewGuid().ToString())
                {
                    Category = "Soccer Shoes",
                    Name = "Adidas Copa 18.1 FG",
                    Price = 75.00D
                };

                var liverpoolJersey = new ProductEntity(liverpoolJerseySKU, Guid.NewGuid().ToString())
                {
                    Category = "Soccer Apparel",
                    Name = "Liverpool Jersey",
                    Price = 22.00D
                };

                var liverpoolShorts = new ProductEntity(liverpoolShortsSKU, Guid.NewGuid().ToString())
                {
                    Category = "Soccer Apparel",
                    Name = "Liverpool Shorts",
                    Price = 19.99D
                };

                var liverpoolShoes = new ProductEntity(nikeShoesSKU, Guid.NewGuid().ToString())
                {
                    Category = "Soccer Shoes",
                    Name = "Nike Mercurial Super Fly FG",
                    Price = 110.00D
                };

                products.Add(barcaJersey);
                products.Add(barcaShorts);
                products.Add(adidasShoes);
                products.Add(liverpoolJersey);
                products.Add(liverpoolShorts);
                products.Add(liverpoolShoes);

                foreach (var product in products)
                {
                    var productTable = tableClient.GetTableReference("Product");
                    var insertProductOperation = TableOperation.Insert(product);
                    await productTable.ExecuteAsync(insertProductOperation);
                }

                var barcaJerseyInventory = new InventoryEntity("Soccer Apparel", barcaJerseySKU)
                {
                    Quantity = 10
                };

                var barcaShortsInventory = new InventoryEntity("Soccer Apparel", barcaShortsSKU)
                {
                    Quantity = 10
                };

                var adidasShoesInventory = new InventoryEntity("Soccer Shoes", adidasShoesSKU)
                {
                    Quantity = 10
                };

                var liverpoolJerseyInventory = new InventoryEntity("Soccer Apparel", liverpoolJerseySKU)
                {
                    Quantity = 10
                };

                var liverpoolShortsInventory = new InventoryEntity("Soccer Apparel", liverpoolShortsSKU)
                {
                    Quantity = 10
                };

                var nikeShoesInventory = new InventoryEntity("Soccer Shoes", nikeShoesSKU)
                {
                    Quantity = 10
                };

                var inventoryTable = tableClient.GetTableReference("Inventory");
                var insertInventoryOperation = TableOperation.Insert(barcaJerseyInventory);
                await inventoryTable.ExecuteAsync(insertInventoryOperation);

                insertInventoryOperation = TableOperation.Insert(barcaShortsInventory);
                await inventoryTable.ExecuteAsync(insertInventoryOperation);

                insertInventoryOperation = TableOperation.Insert(adidasShoesInventory);
                await inventoryTable.ExecuteAsync(insertInventoryOperation);

                insertInventoryOperation = TableOperation.Insert(liverpoolJerseyInventory);
                await inventoryTable.ExecuteAsync(insertInventoryOperation);

                insertInventoryOperation = TableOperation.Insert(liverpoolShortsInventory);
                await inventoryTable.ExecuteAsync(insertInventoryOperation);

                insertInventoryOperation = TableOperation.Insert(nikeShoesInventory);
                await inventoryTable.ExecuteAsync(insertInventoryOperation);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
