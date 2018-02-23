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
    public static class CreateOrder
    {
        [FunctionName("CreateOrder")]
        public static async Task Run([ServiceBusTrigger("createordertopic", "CreateOrder", AccessRights.Listen,
            Connection = "ServiceBusConnectionString")]string topicMessage, TraceWriter log)
        {
            var topicClient = new Microsoft.Azure.ServiceBus.TopicClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString"),
                Environment.GetEnvironmentVariable("CheckInventoryTopicName"));

            try
            {
                if (string.IsNullOrEmpty(topicMessage))
                {
                    throw new Exception("Service Bus Topic Message is null or empty");
                }

                var order = JsonConvert.DeserializeObject<Order>(topicMessage);

                if (order == null)
                {
                    throw new Exception("Order is missing");
                }

                if (order.Customer == null)
                {
                    throw new Exception("Order is missing Customer information");
                }

                if (order.Products == null)
                {
                    throw new Exception("Order is missing Product information");
                }

                if (order.Payment == null)
                {
                    throw new Exception("Order is missing Payment information");
                }

                order.Status = "Created";
                order.CreatedDate = DateTime.UtcNow;
                order.UpdatedDate = DateTime.UtcNow;

                await InsertOrder(order);

                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(order)));
                message.MessageId = Guid.NewGuid().ToString();

                await topicClient.SendAsync(message);

                log.Info($"CreateOrder function processed message successfully: {topicMessage}");
            }
            catch (Exception ex)
            {
                log.Error($"CreateOrder function Failed. Exception: {ex.Message}");
            }
            finally
            {
                await topicClient.CloseAsync();
            }
        }

        private static async Task InsertOrder(Order order)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TableStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();

            var orderEntity = new OrderEntity(order.Customer.Id.ToString(), order.Id.ToString())
            {
                CustomerId = order.Customer.Id,
                Status = order.Status,
                CreatedDate = order.CreatedDate,
                UpdatedDate = order.UpdatedDate
            };

            //Insert Order
            var orderTable = tableClient.GetTableReference("Order");
            var insertOrderOperation = TableOperation.Insert(orderEntity);

            await orderTable.ExecuteAsync(insertOrderOperation);

            //Insert OrderProducts
            foreach(var product in order.Products)
            {
                var orderProductEntity = new OrderProductEntity(order.Id.ToString(), product.Id.ToString()) { };
                var orderProductTable = tableClient.GetTableReference("OrderProduct");
                var insertOrderProductOperation = TableOperation.Insert(orderProductEntity);

                await orderProductTable.ExecuteAsync(insertOrderProductOperation);
            }
        }
    }
}
