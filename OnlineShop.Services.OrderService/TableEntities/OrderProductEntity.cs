using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.OrderService.TableEntities
{
    public class OrderProductEntity : TableEntity
    {
        public OrderProductEntity()
        {

        }

        public OrderProductEntity(string orderId, string productId)
        {
            this.PartitionKey = orderId;
            this.RowKey = productId;
        }
    }
}
