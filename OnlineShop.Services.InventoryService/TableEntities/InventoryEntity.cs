using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.InventoryService.TableEntities
{
    public class InventoryEntity : TableEntity
    {
        public InventoryEntity()
        {

        }

        public InventoryEntity(string productCategory, string SKU)
        {
            this.PartitionKey = productCategory;
            this.RowKey = SKU;
        }

        public int Quantity { get; set; }
    }
}
