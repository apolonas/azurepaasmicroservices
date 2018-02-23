using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.InventoryService.Models
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Customer Customer { get; set; }
        public string CreditCardNumber { get; set; }
        public string ExpirationDate { get; set; }
        public string SecurityCode { get; set; }
        public string Status { get; set; }
    }
}
