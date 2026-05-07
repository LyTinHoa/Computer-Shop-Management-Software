using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace ComputerShopManagement.Models
{
    public class DatabaseContext
    {
        private readonly string connectionString = @"Server=.\SQLEXPRESS;Database=ComputerShopDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}