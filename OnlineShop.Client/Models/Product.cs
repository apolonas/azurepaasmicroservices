using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Client.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        public Guid SKU { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }
}
