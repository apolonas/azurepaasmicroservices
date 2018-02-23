using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Admin.TableEntities
{
    public class CustomerEntity : TableEntity
    {
        public CustomerEntity()
        {

        }

        public CustomerEntity(string region, string customerId)
        {
            this.PartitionKey = region;
            this.RowKey = customerId;
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
