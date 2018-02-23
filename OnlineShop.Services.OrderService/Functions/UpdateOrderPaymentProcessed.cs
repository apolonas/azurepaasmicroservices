using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using OnlineShop.Services.OrderService.Models;
using OnlineShop.Services.OrderService.TableEntities;
using System;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.OrderService.Functions
{
    public static class UpdateOrderPaymentProcessed
    {
        [FunctionName("UpdateOrderPaymentProcessed")]
        public static async Task Run([ServiceBusTrigger("paymentprocessedtopic", "UpdateOrder", AccessRights.Listen,
            Connection = "ServiceBusConnectionString")]string topicMessage, TraceWriter log)
        {
            try
            {
                if (string.IsNullOrEmpty(topicMessage))
                {
                    throw new Exception("ServiceBus topic message is null or empty.");
                }

                var order = JsonConvert.DeserializeObject<Order>(topicMessage);
                order.UpdatedDate = DateTime.UtcNow;

                await UpdateOrderStatus(order);

                if (order.RequiresResponse)
                {
                    var orderResponse = new OrderResponse()
                    {
                        OrderId = order.Id,
                        Status = order.Status
                    };

                    var message = new Message();
                    message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(orderResponse));
                    message.MessageId = Guid.NewGuid().ToString();
                    message.SessionId = order.Id.ToString();

                    var queueClient = new Microsoft.Azure.ServiceBus.QueueClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString"), order.Id.ToString());
                    await queueClient.SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                log.Error($"UpdateOrderPaymentProcessed function failed. Exception: {ex.Message}");
            }

            log.Info($"UpdateOrderPaymentProcessed function finished execution: {topicMessage}");
        }

        public static async Task UpdateOrderStatus(Order order)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TableStorage"));
                var tableClient = storageAccount.CreateCloudTableClient();

                var table = tableClient.GetTableReference("Order");
                var retrieveOperation = TableOperation.Retrieve<OrderEntity>(order.Customer.Id.ToString(), order.Id.ToString());

                var retrievedResult = await table.ExecuteAsync(retrieveOperation);
                var updateEntity = (OrderEntity)retrievedResult.Result;

                if (updateEntity != null)
                {
                    updateEntity.Status = order.Status;

                    var updateOperation = TableOperation.Replace(updateEntity);
                    await table.ExecuteAsync(updateOperation);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
