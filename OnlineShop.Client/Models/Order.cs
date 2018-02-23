using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Client.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public Customer Customer { get; set; }
        public List<Product> Products { get; set; }
        public Payment Payment { get; set; }
        public string Status { get; set; }
        public bool RequiresResponse { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
