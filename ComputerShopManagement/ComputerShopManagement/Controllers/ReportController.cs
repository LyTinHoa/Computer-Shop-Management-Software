using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Controllers
{
    public class ReportController
    {
        private readonly DatabaseContext _dbContext;

        public ReportController()
        {
            _dbContext = new DatabaseContext();
        }

        // Calculates total revenue for the current day
        public decimal GetDailyRevenue()
        {
            decimal dailyRevenue = 0;
            using (var connection = _dbContext.GetConnection())
            {
                // Casts orderDate to DATE to match today's date strictly
                string query = "SELECT ISNULL(SUM(totalAmount), 0) FROM Invoice WHERE CAST(orderDate AS DATE) = CAST(GETDATE() AS DATE)";
                SqlCommand cmd = new SqlCommand(query, connection);

                connection.Open();
                dailyRevenue = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            return dailyRevenue;
        }

        // Retrieves the highest selling products based on total quantity sold in invoice details
        public List<Product> GetTopSellingProducts()
        {
            List<Product> topProducts = new List<Product>();
            using (var connection = _dbContext.GetConnection())
            {
                string query = @"
                    SELECT TOP 5 p.productID, p.name, SUM(d.quantity) as totalSold 
                    FROM Products p
                    JOIN InvoiceDetails d ON p.productID = d.productID
                    GROUP BY p.productID, p.name
                    ORDER BY totalSold DESC";

                SqlCommand cmd = new SqlCommand(query, connection);
                connection.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        topProducts.Add(new Product
                        {
                            ProductID = Convert.ToInt32(reader["productID"]),
                            Name = reader["name"].ToString(),
                            // We temporarily use StockQuantity property here to store 'totalSold' for the report view
                            StockQuantity = Convert.ToInt32(reader["totalSold"])
                        });
                    }
                }
            }
            return topProducts;
        }
    }
}
