using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerShopManagement.Models
{
    public class HardwareSpec
    {
        public int SpecID { get; set; }
        public int ProductID { get; set; }
        public string Cpu { get; set; }
        public string Ram { get; set; }
        public string Storage { get; set; }
        public string Gpu { get; set; }
    }
}
