using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using OnlineShop.Api.OrderApi.Models;

namespace OnlineShop.Api.OrderApi.Controllers
{
    [Produces("application/json")]
    [Route("api/Orders")]
    public class OrdersController : Controller
    {
        private readonly AppSettings _appSettings;

        public OrdersController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        // GET: api/Orders
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Orders/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }
        
        // POST: api/Orders
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Order order)
        {
            bool listenForResponse = Boolean.TryParse(Request.Headers["ImplementAsyncRequestResponseMessaging"], out listenForResponse);

            var topicClient = new TopicClient(_appSettings.ServiceBusConnectionString, _appSettings.OrderTopicName);

            try
            {
                if(order == null)
                {
                    return BadRequest();
                }

                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(order)));
                message.MessageId = Guid.NewGuid().ToString();

                if (listenForResponse)
                {
                    var responseSessionID = order.Id.ToString();
                    var tenantId = _appSettings.TenantId;
                    var clientId = _appSettings.ClientId;
                    var clientSecret = _appSettings.ClientSecret;

                    var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

                    var result = await context.AcquireTokenAsync(
                        "https://management.core.windows.net/",
                        new ClientCredential(clientId, clientSecret));

                    var creds = new TokenCredentials(result.AccessToken);

                    var sbClient = new ServiceBusManagementClient(creds)
                    {
                        SubscriptionId = _appSettings.SubscriptionId
                    };

                    var queueParams = new SBQueue()
                    {
                        DeadLetteringOnMessageExpiration = true,
                        RequiresSession = true,
                        DefaultMessageTimeToLive = TimeSpan.FromMilliseconds(20000)
                    };

                    await sbClient.Queues.CreateOrUpdateAsync(_appSettings.ResourceGroupName, _appSettings.ServiceBusNamespace,
                        responseSessionID, queueParams);

                    var sessionClient = new SessionClient(_appSettings.ServiceBusConnectionString, responseSessionID);
                    var session = await sessionClient.AcceptMessageSessionAsync(responseSessionID);

                    await topicClient.SendAsync(message);

                    var responseMessage = await session.ReceiveAsync();

                    if (responseMessage != null)
                    {
                        var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(Encoding.UTF8.GetString(responseMessage.Body));

                        await session.CompleteAsync(responseMessage.SystemProperties.LockToken);
                        await sbClient.Queues.DeleteAsync(_appSettings.ResourceGroupName, _appSettings.ServiceBusNamespace, responseSessionID);
                        await sessionClient.CloseAsync();

                        return new JsonResult(orderResponse);
                    }
                }

                await topicClient.SendAsync(message);
                return Ok();
            }
            catch(Exception ex)
            {
                return StatusCode(500);
            }
            finally
            {
                await topicClient.CloseAsync();
            }
        }
        
        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
