using System;
using System.Collections.Generic;
using System.Data;
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

        // Calculate today's total revenue
        public decimal GetDailyRevenue()
        {
            decimal dailyRevenue = 0;
            using (var connection = _dbContext.GetConnection())
            {
                string query = "SELECT ISNULL(SUM(totalAmount), 0) FROM Invoice WHERE CAST(orderDate AS DATE) = CAST(GETDATE() AS DATE)";
                SqlCommand cmd = new SqlCommand(query, connection);

                connection.Open();
                dailyRevenue = Convert.ToDecimal(cmd.ExecuteScalar());
            }
            return dailyRevenue;
        }

        // Get top 5 selling products
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
                            StockQuantity = Convert.ToInt32(reader["totalSold"])
                        });
                    }
                }
            }
            return topProducts;
        }

        // Get detailed sales using JOIN for 4 tables
        public DataTable GetDetailedSalesReport()
        {
            DataTable dt = new DataTable();
            using (var connection = _dbContext.GetConnection())
            {
                // Added p.category so we can calculate the top performer
                string query = @"
                    SELECT 
                        i.invoiceID AS 'Invoice ID',
                        i.orderDate AS 'Date',
                        c.fullName AS 'Customer Name',
                        p.category AS 'Category', 
                        p.name AS 'Product Name',
                        d.quantity AS 'Product Qty',
                        d.unitPrice AS 'Unit Price',
                        (d.quantity * d.unitPrice) AS 'Total'
                    FROM Invoice i
                    JOIN Customer c ON i.customerID = c.customerID 
                    JOIN InvoiceDetails d ON i.invoiceID = d.invoiceID
                    JOIN Products p ON d.productID = p.productID
                    ORDER BY i.invoiceID ASC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // Get real-time inventory valuation
        public DataTable GetInventoryValuationReport()
        {
            DataTable dt = new DataTable();
            using (var connection = _dbContext.GetConnection())
            {
                string query = @"
                    SELECT 
                        category AS 'Category',
                        name AS 'Product Name',
                        stockQuantity AS 'Current Stock',
                        price AS 'Unit Cost',
                        (stockQuantity * price) AS 'Total Asset Value'
                    FROM Products
                    ORDER BY category, name";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
    }
}