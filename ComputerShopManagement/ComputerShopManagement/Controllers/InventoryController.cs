using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Controllers
{
    public class InventoryController
    {
        private readonly DatabaseContext _dbContext;
        public List<Product> ProductList { get; private set; }

        public InventoryController()
        {
            _dbContext = new DatabaseContext();
            ProductList = new List<Product>();
        }

        // Adds a new product and its 1-to-1 hardware specs to the database
        public bool AddNewProduct(Product p)
        {
            using (var connection = _dbContext.GetConnection())
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert Product
                        string prodQuery = "INSERT INTO Products (name, category, price, stockQuantity) OUTPUT INSERTED.productID VALUES (@name, @cat, @price, @qty)";
                        SqlCommand prodCmd = new SqlCommand(prodQuery, connection, transaction);
                        prodCmd.Parameters.AddWithValue("@name", p.Name);
                        prodCmd.Parameters.AddWithValue("@cat", p.Category);
                        prodCmd.Parameters.AddWithValue("@price", p.Price);
                        prodCmd.Parameters.AddWithValue("@qty", p.StockQuantity);

                        int newProductId = (int)prodCmd.ExecuteScalar();

                        // Insert Hardware Specs if they exist
                        if (p.Specs != null)
                        {
                            string specQuery = "INSERT INTO HardwareSpecs (productID, cpu, ram, storage, gpu) VALUES (@pid, @cpu, @ram, @storage, @gpu)";
                            SqlCommand specCmd = new SqlCommand(specQuery, connection, transaction);
                            specCmd.Parameters.AddWithValue("@pid", newProductId);
                            specCmd.Parameters.AddWithValue("@cpu", p.Specs.Cpu ?? (object)DBNull.Value);
                            specCmd.Parameters.AddWithValue("@ram", p.Specs.Ram ?? (object)DBNull.Value);
                            specCmd.Parameters.AddWithValue("@storage", p.Specs.Storage ?? (object)DBNull.Value);
                            specCmd.Parameters.AddWithValue("@gpu", p.Specs.Gpu ?? (object)DBNull.Value);
                            specCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
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
        // Deletes a product, but protects against deleting products tied to existing invoices
        public bool DeleteProduct(int productId, out string errorMessage)
        {
            errorMessage = string.Empty;
            ComputerShopManagement.Models.DatabaseContext db = new ComputerShopManagement.Models.DatabaseContext();

            using (var connection = db.GetConnection())
            {
                try
                {
                    string query = "DELETE FROM Products WHERE productID = @id";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", productId);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException ex)
                {
                    // Error 547 is a Foreign Key Constraint Violation
                    if (ex.Number == 547)
                    {
                        errorMessage = "Cannot delete this product because it is already linked to existing sales invoices. \n\n(Deleting it would break past financial records).";
                    }
                    else
                    {
                        errorMessage = "A database error occurred: " + ex.Message;
                    }
                    return false;
                }
            }
        }
        // Retrieves products that have a stock quantity below a certain threshold (e.g., 5)
        public List<Product> TrackLowStock()
        {
            List<Product> lowStockItems = new List<Product>();
            using (var connection = _dbContext.GetConnection())
            {
                string query = "SELECT * FROM Products WHERE stockQuantity <= 5";
                SqlCommand cmd = new SqlCommand(query, connection);
                connection.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lowStockItems.Add(new Product
                        {
                            ProductID = Convert.ToInt32(reader["productID"]),
                            Name = reader["name"].ToString(),
                            StockQuantity = Convert.ToInt32(reader["stockQuantity"])
                        });
                    }
                }
            }
            return lowStockItems;
        }

        // Updates specific hardware components based on FR3
        public void UpdateHardwareSpec(int id, HardwareSpec spec)
        {
            using (var connection = _dbContext.GetConnection())
            {
                string query = "UPDATE HardwareSpecs SET cpu=@cpu, ram=@ram, storage=@storage, gpu=@gpu WHERE productID=@pid";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@pid", id);
                cmd.Parameters.AddWithValue("@cpu", spec.Cpu);
                cmd.Parameters.AddWithValue("@ram", spec.Ram);
                cmd.Parameters.AddWithValue("@storage", spec.Storage);
                cmd.Parameters.AddWithValue("@gpu", spec.Gpu);

                connection.Open();
                cmd.ExecuteNonQuery();
            }
        }

        //Retrieve only available products in inventory
        public List<Product> GetAvailableProducts()
        {
            List<Product> availableItems = new List<Product>();
            using (var connection = _dbContext.GetConnection())
            {
                string query = "SELECT productID, name, price, stockQuantity FROM Products WHERE stockQuantity > 0";
                SqlCommand cmd = new SqlCommand(query, connection);
                connection.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        availableItems.Add(new Product
                        {
                            ProductID = Convert.ToInt32(reader["productID"]),
                            Name = reader["name"].ToString(),
                            Price = Convert.ToDecimal(reader["price"]),
                            StockQuantity = Convert.ToInt32(reader["stockQuantity"])
                        });
                    }
                }
            }
            return availableItems;
        }
    }
}
