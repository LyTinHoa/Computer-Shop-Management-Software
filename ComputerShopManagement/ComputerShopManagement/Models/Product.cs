using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerShopManagement.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        // Links detailed specs to the product
        public HardwareSpec Specs { get; set; }
    }
}
