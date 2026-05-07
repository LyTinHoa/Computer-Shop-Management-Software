using System;
using Microsoft.Data.SqlClient;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Controllers
{
    public class SalesController
    {
        private readonly DatabaseContext _dbContext;
        public Invoice CurrentInvoice { get; private set; }
        public InventoryController Inventory { get; private set; }

        public SalesController()
        {
            _dbContext = new DatabaseContext();
            CurrentInvoice = new Invoice();
            Inventory = new InventoryController();
        }

        // Adds selected items to the current invoice details list
        public void AddItemsToInvoice(Product p, int qty)
        {
            InvoiceDetail detail = new InvoiceDetail
            {
                ProductID = p.ProductID,
                Quantity = qty,
                UnitPrice = p.Price
            };
            CurrentInvoice.Details.Add(detail);
        }

        // Calculates the sum of all items in the current invoice
        public decimal CalculateFinalTotal()
        {
            decimal total = 0;
            foreach (var item in CurrentInvoice.Details)
            {
                total += item.UnitPrice * item.Quantity;
            }
            CurrentInvoice.TotalAmount = total;
            return total;
        }

        // Processes the checkout: Saves the invoice and updates stock securely
        public bool ProcessPayment()
        {
            if (CurrentInvoice.Details.Count == 0) return false;

            CalculateFinalTotal();

            using (var connection = _dbContext.GetConnection())
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert the main Invoice record
                        string invQuery = "INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) OUTPUT INSERTED.invoiceID VALUES (@date, @total, @staff, @customer)";
                        SqlCommand invCmd = new SqlCommand(invQuery, connection, transaction);
                        invCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        invCmd.Parameters.AddWithValue("@total", CurrentInvoice.TotalAmount);
                        invCmd.Parameters.AddWithValue("@staff", CurrentInvoice.StaffID);
                        invCmd.Parameters.AddWithValue("@customer", CurrentInvoice.CustomerID);

                        int generatedInvoiceId = (int)invCmd.ExecuteScalar();
                        CurrentInvoice.InvoiceID = generatedInvoiceId;

                        // 2. Insert Invoice Details and Update Stock Levels (FR6)
                        foreach (var item in CurrentInvoice.Details)
                        {
                            // Save Detail
                            string detQuery = "INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (@invId, @prodId, @qty, @price)";
                            SqlCommand detCmd = new SqlCommand(detQuery, connection, transaction);
                            detCmd.Parameters.AddWithValue("@invId", generatedInvoiceId);
                            detCmd.Parameters.AddWithValue("@prodId", item.ProductID);
                            detCmd.Parameters.AddWithValue("@qty", item.Quantity);
                            detCmd.Parameters.AddWithValue("@price", item.UnitPrice);
                            detCmd.ExecuteNonQuery();

                            // Update Stock
                            string stockQuery = "UPDATE Products SET stockQuantity = stockQuantity - @qty WHERE productID = @prodId";
                            SqlCommand stockCmd = new SqlCommand(stockQuery, connection, transaction);
                            stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                            stockCmd.Parameters.AddWithValue("@prodId", item.ProductID);
                            stockCmd.ExecuteNonQuery();
                        }

                        // Commit all changes safely
                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        // Roll back everything if any error occurs (e.g., insufficient stock constraints)
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
    }
}
