using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.OrderService.Models
{
    public class OrderResponse
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; }
    }
}
