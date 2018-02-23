using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using OnlineShop.Services.InventoryService.Models;
using OnlineShop.Services.InventoryService.TableEntities;
using System;
using System.Threading.Tasks;

namespace OnlineShop.Services.InventoryService.Functions
{
    public static class UpdateInventoryPaymentProcessed
    {
        [FunctionName("UpdateInventoryPaymentProcessed")]
        public static async Task Run([ServiceBusTrigger("paymentprocessedtopic", "UpdateInventory", AccessRights.Listen,
            Connection = "ServiceBusConnectionString")]string topicMessage, TraceWriter log)
        {
            try
            {
                if (string.IsNullOrEmpty(topicMessage))
                {
                    log.Info($"ServiceBus topic message is null or empty.");
                    return;
                }

                var order = JsonConvert.DeserializeObject<Order>(topicMessage);

                if (order.Products == null)
                {
                    log.Info($"Order is missing Product information. Could not update Inventory.");
                    return;
                }

                await UpdateInventory(order);
                log.Info($"CheckOrderInventory function processed message successfully: {topicMessage}");
            }
            catch (Exception ex)
            {
                log.Info($"UpdateInventory function Failed. Exception: {ex.Message}");
            }
        }

        private static async Task UpdateInventory(Order order)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TableStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("Inventory");

            foreach (var product in order.Products)
            {
                var retrieveOperation = TableOperation.Retrieve<InventoryEntity>(product.Category, product.SKU.ToString());
                var retrievedResult = await table.ExecuteAsync(retrieveOperation);
                var updateEntity = (InventoryEntity)retrievedResult.Result;

                if (updateEntity != null)
                {
                    if (updateEntity.Quantity != 0)
                    {
                        updateEntity.Quantity = updateEntity.Quantity - 1;

                        var updateOperation = TableOperation.Replace(updateEntity);
                        await table.ExecuteAsync(updateOperation);
                    }
                }
            }
        }
    }
}
