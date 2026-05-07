using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerShopManagement.Models
{
    public class InvoiceDetail
    {
        public int DetailID { get; set; }
        public int InvoiceID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
