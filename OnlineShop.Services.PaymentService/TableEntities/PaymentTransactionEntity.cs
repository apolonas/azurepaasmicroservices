using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShop.Services.PaymentService.TableEntities
{
    public class PaymentTransactionEntity : TableEntity
    {
        public PaymentTransactionEntity()
        {

        }

        public PaymentTransactionEntity(string orderId, string paymentTransactionId)
        {
            this.PartitionKey = orderId;
            this.RowKey = paymentTransactionId;
        }

        public string CustomerId { get; set; }
        public string CreditCardNumber { get; set; }
        public string ExpirationDate { get; set; }
        public string SecurityCode { get; set; }
        public string Status { get; set; }
    }
}
