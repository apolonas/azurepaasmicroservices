using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.OrderService.TableEntities
{
    public class OrderEntity : TableEntity
    {
        public OrderEntity()
        {

        }

        public OrderEntity(string customerId, string orderId)
        {
            this.PartitionKey = customerId;
            this.RowKey = orderId;
        }

        public Guid CustomerId { get; set; }
        public Guid? PaymentId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
