using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using OnlineShop.Services.InventoryService.Models;
using OnlineShop.Services.InventoryService.TableEntities;
using System;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.InventoryService.Functions
{
    public static class CheckOrderInventory
    {
        [FunctionName("CheckOrderInventory")]
        public static async Task Run([ServiceBusTrigger("checkinventorytopic", "CheckOrderInventory", AccessRights.Listen,
            Connection = "ServiceBusConnectionString")]string topicMessage, TraceWriter log)
        {
            var topicClient = new Microsoft.Azure.ServiceBus.TopicClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString"),
                Environment.GetEnvironmentVariable("InventoryCheckedTopicName"));

            try
            {
                if (string.IsNullOrEmpty(topicMessage))
                {
                    log.Info($"ServiceBus topic message is null or empty.");
                    return;
                }

                var order = JsonConvert.DeserializeObject<Order>(topicMessage);

                if (order.Payment == null)
                {
                    throw new Exception("Order is missing Payment information.");
                }

                var isInventoryVerified = await IsInventoryVerified(order);

                Message message = new Message();

                if (isInventoryVerified)
                {
                    order.Status = "Inventory Verified";
                    order.UpdatedDate = DateTime.UtcNow;
                    message.MessageId = Guid.NewGuid().ToString();
                    message.CorrelationId = "Verified";
                }
                else
                {
                    order.Status = "Insufficient Inventory";
                    order.UpdatedDate = DateTime.UtcNow;
                    message.MessageId = Guid.NewGuid().ToString();
                    message.CorrelationId = "Insufficient";

                    log.Info($"Insufficient inventory to complete order.");
                }

                message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(order));

                await topicClient.SendAsync(message);

                log.Info($"CheckOrderInventory function processed message successfully: {topicMessage}");
            }
            catch (Exception ex)
            {
                log.Error($"CheckOrderInventory function Failed. Exception: {ex.Message}");
            }
            finally
            {
                await topicClient.CloseAsync();
            }
        }

        private static async Task<bool> IsInventoryVerified(Order order)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TableStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("Inventory");

            foreach (var product in order.Products)
            {
                var partitionKey = product.Category;
                var rowKey = product.SKU.ToString();
                var retrieveOperation = TableOperation.Retrieve<InventoryEntity>(partitionKey, rowKey);
                var inventory = await table.ExecuteAsync(retrieveOperation);

                if (((InventoryEntity)inventory.Result).Quantity < 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
