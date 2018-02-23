using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using OnlineShop.Services.PaymentService.Models;
using OnlineShop.Services.PaymentService.TableEntities;
using System;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.PaymentService.Functions
{
    public static class ProcessPaymentInventoryChecked
    {
        [FunctionName("ProcessPaymentInventoryChecked")]
        public static async Task Run([ServiceBusTrigger("inventorycheckedtopic", "ProcessPayment",
            AccessRights.Listen, Connection = "ServiceBusConnectionString")]string topicMessage, TraceWriter log)
        {
            var topicClient = new Microsoft.Azure.ServiceBus.TopicClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString"),
                Environment.GetEnvironmentVariable("PaymentProcessedTopicName"));

            try
            {
                if (string.IsNullOrEmpty(topicMessage))
                {
                    throw new Exception("ServiceBus topic message is null or empty.");
                }

                var order = JsonConvert.DeserializeObject<Order>(topicMessage);

                var transaction = await InsertPaymentTransaction(order);
                var paymentSucceeded = await Task.FromResult<bool>(PaymentSucceeded());
                var message = new Message();

                if (paymentSucceeded)
                {
                    await UpdatePaymentTransaction(transaction, "Verified");

                    order.Status = "Payment Verified";
                    order.UpdatedDate = DateTime.UtcNow;
                    message.MessageId = Guid.NewGuid().ToString();
                    message.CorrelationId = "Verified";
                }
                else
                {
                    await UpdatePaymentTransaction(transaction, "Failed");

                    order.Status = "Payment Failed";
                    order.UpdatedDate = DateTime.UtcNow;
                    message.MessageId = Guid.NewGuid().ToString();
                    message.CorrelationId = "Failed";
                }

                message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(order));
                await topicClient.SendAsync(message);

                log.Info($"ProcessOrderPayment function processed message successfully: {topicMessage}");
            }
            catch (Exception ex)
            {
                log.Error($"ProcessOrderPayment Failed. Exception: {ex.Message}");
            }
            finally
            {
                await topicClient.CloseAsync();
            }
        }

        private static bool PaymentSucceeded()
        {
            return true;
        }

        private static async Task<PaymentTransactionEntity> InsertPaymentTransaction(Order order)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TableStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();

            var paymentTransactionEntity = new PaymentTransactionEntity(order.Id.ToString(), Guid.NewGuid().ToString())
            {
                CustomerId = order.Customer.Id.ToString(),
                CreditCardNumber = order.Payment.CreditCardNumber,
                ExpirationDate = order.Payment.ExpirationDate,
                SecurityCode = order.Payment.SecurityCode,
                Status = "Pending"
            };

            var table = tableClient.GetTableReference("PaymentTransaction");
            var insertPaymentTransactionOperation = TableOperation.Insert(paymentTransactionEntity);
            await table.ExecuteAsync(insertPaymentTransactionOperation);

            return paymentTransactionEntity;
        }

        private static async Task UpdatePaymentTransaction(PaymentTransactionEntity transaction, string status)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("TableStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("PaymentTransaction");
            var retrieveOperation = TableOperation.Retrieve<PaymentTransactionEntity>(transaction.PartitionKey, transaction.RowKey);

            var retrievedResult = await table.ExecuteAsync(retrieveOperation);
            var updateEntity = (PaymentTransactionEntity)retrievedResult.Result;

            if (updateEntity != null)
            {
                updateEntity.Status = status;

                var updateOperation = TableOperation.Replace(updateEntity);
                await table.ExecuteAsync(updateOperation);
            }
        }
    }
}
