using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerShopManagement.Models
{
    public class Invoice
    {
        public int InvoiceID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int StaffID { get; set; }
        public int CustomerID { get; set; }

        // Holds all items purchased in this invoice
        public List<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
    }
}
