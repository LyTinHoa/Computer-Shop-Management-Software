using System;
using System.IO;
using System.Text;
using System.Diagnostics;
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

        // Processes the checkout and triggers text export on success
        public bool ProcessPayment(Customer customer)
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
                        // 1. Insert Invoice record
                        string invQuery = "INSERT INTO Invoice (orderDate, totalAmount, staffID, customerID) OUTPUT INSERTED.invoiceID VALUES (@date, @total, @staff, @customer)";
                        SqlCommand invCmd = new SqlCommand(invQuery, connection, transaction);
                        invCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        invCmd.Parameters.AddWithValue("@total", CurrentInvoice.TotalAmount);
                        invCmd.Parameters.AddWithValue("@staff", CurrentInvoice.StaffID);
                        invCmd.Parameters.AddWithValue("@customer", customer.CustomerID);

                        int generatedInvoiceId = (int)invCmd.ExecuteScalar();
                        CurrentInvoice.InvoiceID = generatedInvoiceId;

                        // 2. Insert Details and Update Stock
                        foreach (var item in CurrentInvoice.Details)
                        {
                            string detQuery = "INSERT INTO InvoiceDetails (invoiceID, productID, quantity, unitPrice) VALUES (@invId, @prodId, @qty, @price)";
                            SqlCommand detCmd = new SqlCommand(detQuery, connection, transaction);
                            detCmd.Parameters.AddWithValue("@invId", generatedInvoiceId);
                            detCmd.Parameters.AddWithValue("@prodId", item.ProductID);
                            detCmd.Parameters.AddWithValue("@qty", item.Quantity);
                            detCmd.Parameters.AddWithValue("@price", item.UnitPrice);
                            detCmd.ExecuteNonQuery();

                            string stockQuery = "UPDATE Products SET stockQuantity = stockQuantity - @qty WHERE productID = @prodId";
                            SqlCommand stockCmd = new SqlCommand(stockQuery, connection, transaction);
                            stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                            stockCmd.Parameters.AddWithValue("@prodId", item.ProductID);
                            stockCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        // 3. Trigger immediate export and launch [Requirement FR2/FR6]
                        ExportInvoiceToText(customer);
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        // Formats and saves the invoice as a sleek text file
        // Formats and saves the invoice as a sleek text file in the designated folder
        private void ExportInvoiceToText(Customer customer)
        {
            StringBuilder sb = new StringBuilder();
            string line = new string('-', 50);

            // Header [Sleek modern design]
            sb.AppendLine("==================================================");
            sb.AppendLine("           BITTEKK COMPUTER SYSTEMS              ");
            sb.AppendLine("         Premium Hardware & Solutions            ");
            sb.AppendLine("==================================================");
            sb.AppendLine($"Invoice ID: {CurrentInvoice.InvoiceID}");
            // Explicitly formatting to 12-hour clock with AM/PM
            sb.AppendLine($"Date:       {DateTime.Now.ToString("MMMM dd, yyyy  hh:mm tt")}");
            sb.AppendLine(line);

            // Customer Info
            sb.AppendLine("CUSTOMER DETAILS");
            sb.AppendLine($"Name:  {customer.FullName}");
            sb.AppendLine($"Phone: {customer.Phone}");
            if (!string.IsNullOrEmpty(customer.Email)) sb.AppendLine($"Email: {customer.Email}");
            sb.AppendLine(line);

            // Itemized List Header
            sb.AppendLine(string.Format("{0,-30} {1,5} {2,12}", "Item", "Qty", "Price"));
            sb.AppendLine(line);

            // Items
            var availableProducts = Inventory.GetAvailableProducts();
            foreach (var detail in CurrentInvoice.Details)
            {
                var prod = availableProducts.Find(p => p.ProductID == detail.ProductID);
                string prodName = prod != null ? prod.Name : $"Product #{detail.ProductID}";

                // :C will automatically use the '$' sign because we set the global culture in Program.cs
                sb.AppendLine(string.Format("{0,-30} {1,5} {2,12:C}",
                    prodName.Length > 28 ? prodName.Substring(0, 27) + ".." : prodName,
                    detail.Quantity,
                    detail.UnitPrice * detail.Quantity));
            }

            // Total Amount
            sb.AppendLine(line);
            sb.AppendLine(string.Format("{0,-30} {1,18:C}", "TOTAL AMOUNT:", CurrentInvoice.TotalAmount));
            sb.AppendLine("==================================================");
            sb.AppendLine("      Thank you for shopping at BitTekk!         ");
            sb.AppendLine("    Please keep this receipt for warranty.       ");
            sb.AppendLine("==================================================");

            // --- DIRECTORY MANAGEMENT ---
            // Navigates up from bin\Debug\net10.0-windows to the main project folder
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\InvoiceOutput"));

            // Failsafe: If app is published (not running in VS), create it next to the .exe instead
            if (!Directory.Exists(targetDir) && !baseDir.Contains("bin"))
            {
                targetDir = Path.Combine(baseDir, "InvoiceOutput");
            }

            // Create the directory if you haven't manually created it yet
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // File IO Operations
            string fileName = $"Invoice_{CurrentInvoice.InvoiceID}.txt";
            string filePath = Path.Combine(targetDir, fileName);

            File.WriteAllText(filePath, sb.ToString());

            // Launch automatically in Notepad
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
    }
}