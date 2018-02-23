using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShop.Api.OrderApi.Models
{
    public class OrderResponse
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
    }
}
