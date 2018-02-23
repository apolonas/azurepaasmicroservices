using OnlineShop.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Client
{
    public class HttpRequestSender
    {
        private HttpClient _httpClient = null;

        public HttpRequestSender()
        {
            _httpClient = new HttpClient();
        }

        public async Task PlaceOrderAsync(Order order)
        {
            //await _httpClient.PostAsJsonAsync("http://localhost:61068/api/orders", order);
            await _httpClient.PostAsJsonAsync("https://api-onlineshop-order.azurewebsites.net/api/orders", order);
        }

        public async Task<HttpResponseMessage> PlaceOrderAsyncWaitForResponse(Order order)
        {
            _httpClient.DefaultRequestHeaders.Add("ImplementAsyncRequestResponseMessaging", "true");

            //HttpResponseMessage response = await _httpClient.PostAsJsonAsync("http://localhost:61068/api/orders", order);
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("https://api-onlineshop-order.azurewebsites.net/api/orders", order);
            return response;
        }
    }
}
