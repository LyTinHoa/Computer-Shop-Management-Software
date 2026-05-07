using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerShopManagement.Models
{
    public class Staff
    {
        public int StaffID { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
