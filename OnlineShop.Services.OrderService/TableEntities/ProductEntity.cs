using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.OrderService.TableEntities
{
    public class ProductEntity : TableEntity
    {
        public ProductEntity()
        {

        }

        public ProductEntity(string SKU, string productId)
        {
            this.PartitionKey = SKU;
            this.RowKey = productId;
        }

        public string Category { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }
}
