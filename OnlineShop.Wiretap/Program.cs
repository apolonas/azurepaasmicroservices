using Microsoft.Azure;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineShop.Wiretap
{
    class Program
    {
        private static ISubscriptionClient _createOrderSubscriptionClient;
        private static ISubscriptionClient _checkInventorySubscriptionClient;
        private static ISubscriptionClient _inventoryCheckedSubscriptionClient;
        private static ISubscriptionClient _paymentProcessedSubscriptionClient;

        private static string _subscriptionId = CloudConfigurationManager.GetSetting("SubscriptionId");
        private static string _resourceGroupName = CloudConfigurationManager.GetSetting("ResourceGroupName");
        private static string _serviceBusNamespace = CloudConfigurationManager.GetSetting("ServiceBusNamespace");
        private static string _serviceBusConnectionString = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
        private static string _tenantId = CloudConfigurationManager.GetSetting("TenantId");
        private static string _clientId = CloudConfigurationManager.GetSetting("ClientId");
        private static string _clientSecret = CloudConfigurationManager.GetSetting("ClientSecret");
        private static string _createOrderTopic = CloudConfigurationManager.GetSetting("CreateOrderTopic");
        private static string _checkInventoryTopic = CloudConfigurationManager.GetSetting("CheckInventoryTopic");
        private static string _inventoryCheckedTopic = CloudConfigurationManager.GetSetting("InventoryCheckedTopic");
        private static string _paymentProcessedTopic = CloudConfigurationManager.GetSetting("PaymentProcessedTopic");

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            try
            {
                var wiretapId = Guid.NewGuid().ToString();

                var context = new AuthenticationContext($"https://login.microsoftonline.com/{_tenantId}");

                var result = await context.AcquireTokenAsync(
                    "https://management.core.windows.net/",
                    new ClientCredential(_clientId, _clientSecret)
                );

                var creds = new TokenCredentials(result.AccessToken);
                var sbClient = new ServiceBusManagementClient(creds)
                {
                    SubscriptionId = _subscriptionId
                };

                var subscriptionParameters = new SBSubscription
                {
                    DeadLetteringOnMessageExpiration = true,
                    DefaultMessageTimeToLive = TimeSpan.FromSeconds(30),
                    AutoDeleteOnIdle = TimeSpan.FromMinutes(5)
                };

                await sbClient.Subscriptions.CreateOrUpdateAsync(_resourceGroupName, _serviceBusNamespace, _createOrderTopic, $"WireTap-{wiretapId}", subscriptionParameters);
                await sbClient.Subscriptions.CreateOrUpdateAsync(_resourceGroupName, _serviceBusNamespace, _checkInventoryTopic, $"WireTap-{wiretapId}", subscriptionParameters);
                await sbClient.Subscriptions.CreateOrUpdateAsync(_resourceGroupName, _serviceBusNamespace, _inventoryCheckedTopic, $"WireTap-{wiretapId}", subscriptionParameters);
                await sbClient.Subscriptions.CreateOrUpdateAsync(_resourceGroupName, _serviceBusNamespace, _paymentProcessedTopic, $"WireTap-{wiretapId}", subscriptionParameters);

                _createOrderSubscriptionClient = new SubscriptionClient(_serviceBusConnectionString, _createOrderTopic, $"WireTap-{wiretapId}");
                _checkInventorySubscriptionClient = new SubscriptionClient(_serviceBusConnectionString, _checkInventoryTopic, $"WireTap-{wiretapId}");
                _inventoryCheckedSubscriptionClient = new SubscriptionClient(_serviceBusConnectionString, _inventoryCheckedTopic, $"WireTap-{wiretapId}");
                _paymentProcessedSubscriptionClient = new SubscriptionClient(_serviceBusConnectionString, _paymentProcessedTopic, $"WireTap-{wiretapId}");

                RegisterCreateOrderOnMessageHandlerAndReceiveMessages();
                RegisterCheckInventoryOnMessageHandlerAndReceiveMessages();
                RegisterInventoryCheckedOnMessageHandlerAndReceiveMessages();
                RegisterPaymentProcessedOnMessageHandlerAndReceiveMessages();

                Console.WriteLine($"Wiretap Created. Listening for messages...");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
            }

            Console.Read();
        }

        static async Task ProcessCreateOrderMessagesAsync(Message message, CancellationToken token)
        {
            dynamic order = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body));

            Console.WriteLine($"{DateTime.UtcNow.ToString("HH:mm:ss.fff")} - Topic: \"CreateOrderTopic\" received Order with Id: {order.Id} and Status: {order.Status}");

            await _createOrderSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        static async Task ProcessCheckInventoryMessagesAsync(Message message, CancellationToken token)
        {
            dynamic order = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body));

            Console.WriteLine($"{DateTime.UtcNow.ToString("HH:mm:ss.fff")} - Topic: \"CheckInventoryTopic\" received Order with Id: {order.Id} and Status: {order.Status}");

            await _checkInventorySubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        static async Task ProcessInventoryCheckedMessagesAsync(Message message, CancellationToken token)
        {
            dynamic order = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body));

            Console.WriteLine($"{DateTime.UtcNow.ToString("HH:mm:ss.fff")} - Topic: \"InventoryCheckedTopic\" received Order with Id: {order.Id} and Status: {order.Status}");

            await _inventoryCheckedSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        static async Task ProcessPaymentProcessedMessagesAsync(Message message, CancellationToken token)
        {
            dynamic order = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body));

            Console.WriteLine($"{DateTime.UtcNow.ToString("HH:mm:ss.fff")} - Topic: \"PaymentProcessedTopic\" received Order with Id: {order.Id} and Status: {order.Status}");

            await _paymentProcessedSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }


        static void RegisterCreateOrderOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            _createOrderSubscriptionClient.RegisterMessageHandler(ProcessCreateOrderMessagesAsync, messageHandlerOptions);
        }     

        static void RegisterCheckInventoryOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            _checkInventorySubscriptionClient.RegisterMessageHandler(ProcessCheckInventoryMessagesAsync, messageHandlerOptions);
        }

        static void RegisterInventoryCheckedOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            _inventoryCheckedSubscriptionClient.RegisterMessageHandler(ProcessInventoryCheckedMessagesAsync, messageHandlerOptions);
        }

        static void RegisterPaymentProcessedOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            _paymentProcessedSubscriptionClient.RegisterMessageHandler(ProcessPaymentProcessedMessagesAsync, messageHandlerOptions);
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            Console.WriteLine();

            return Task.CompletedTask;
        }
    }
}
