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

        // ==========================================
        // MASTER ROUTING METHOD FOR REPORTS
        // ==========================================
        public DataTable GetReportData(string reportType, DateTime startDate, DateTime endDate)
        {
            DataTable dt = new DataTable();

            if (reportType == "Inventory Valuation")
            {
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
            }
            else
            {
                // Handles Daily, Weekly, and Monthly Sales seamlessly
                using (var connection = _dbContext.GetConnection())
                {
                    string query = @"
                        SELECT 
                            CAST(i.orderDate AS DATE) AS 'Date',
                            'INV-' + CAST(i.invoiceID AS VARCHAR) AS 'Invoice ID',
                            c.fullName AS 'Customer Name',
                            SUM(d.quantity) AS 'Items Purchased',
                            i.totalAmount AS 'Total Amount'
                        FROM Invoice i
                        JOIN Customer c ON i.customerID = c.customerID
                        JOIN InvoiceDetails d ON i.invoiceID = d.invoiceID
                        WHERE CAST(i.orderDate AS DATE) >= @start AND CAST(i.orderDate AS DATE) <= @end
                        GROUP BY CAST(i.orderDate AS DATE), i.invoiceID, c.fullName, i.totalAmount
                        ORDER BY CAST(i.orderDate AS DATE) DESC";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@start", startDate);
                        cmd.Parameters.AddWithValue("@end", endDate);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            return dt;
        }

        // Keep existing legacy methods for fallback compatibility
        public decimal GetDailyRevenue()
        {
            decimal dailyRevenue = 0;
            using (var connection = _dbContext.GetConnection())
            {
                string query = "SELECT ISNULL(SUM(totalAmount), 0) FROM Invoice WHERE CAST(orderDate AS DATE) = CAST(GETDATE() AS DATE)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    connection.Open();
                    dailyRevenue = Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
            return dailyRevenue;
        }

        // Gets the total number of invoices generated today
        public int GetDailyTransactionCount()
        {
            using (var connection = _dbContext.GetConnection())
            {
                string query = "SELECT COUNT(invoiceID) FROM Invoice WHERE CAST(orderDate AS DATE) = CAST(GETDATE() AS DATE)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    connection.Open();
                    object result = cmd.ExecuteScalar();
                    return result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }

        // Gets the total number of products that have dropped to 5 units or below
        public int GetLowStockCount(int threshold = 5)
        {
            using (var connection = _dbContext.GetConnection())
            {
                string query = "SELECT COUNT(productID) FROM Products WHERE stockQuantity <= @threshold";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@threshold", threshold);
                    connection.Open();
                    object result = cmd.ExecuteScalar();
                    return result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }

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

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            topProducts.Add(new Product
                            {
                                ProductID = Convert.ToInt32(reader["productID"]),
                                Name = reader["name"].ToString()
                            });
                        }
                    }
                }
            }
            return topProducts;
        }
    }
}
