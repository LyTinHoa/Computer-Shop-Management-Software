using System;
using System.Data;
using Microsoft.Data.SqlClient;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Controllers
{
    public class CustomerController
    {
        private readonly DatabaseContext _dbContext;

        public CustomerController()
        {
            _dbContext = new DatabaseContext();
        }

        // Retrieves all customers for the DataGridView
        // Retrieves all customers for the DataGridView, including their associated Invoice IDs
        public DataTable GetAllCustomers()
        {
            DataTable dt = new DataTable();
            using (var conn = _dbContext.GetConnection())
            {
                // Groups invoice IDs into a comma-separated string, or returns 'None' if they haven't bought anything
                string query = @"
            SELECT 
                c.customerID, 
                c.fullName AS [Full Name], 
                c.email AS [Email], 
                c.phone AS [Phone Number],
                ISNULL(STRING_AGG(CAST(i.invoiceID AS VARCHAR), ', '), 'None') AS [Invoices]
            FROM Customer c
            LEFT JOIN Invoice i ON c.customerID = i.customerID
            GROUP BY c.customerID, c.fullName, c.email, c.phone";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }
        // Adds a new customer to the database
        public bool AddCustomer(Customer customer, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                using (var conn = _dbContext.GetConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO Customer (fullName, email, phone) VALUES (@name, @email, @phone)";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", customer.FullName);
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(customer.Email) ? (object)DBNull.Value : customer.Email);
                        cmd.Parameters.AddWithValue("@phone", customer.Phone);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (SqlException ex)
            {
                // Error 2627 is a Unique Constraint Violation (Phone number is unique in DB)
                if (ex.Number == 2627)
                {
                    errorMsg = "A customer with this phone number already exists.";
                }
                else
                {
                    errorMsg = "Database Error: " + ex.Message;
                }
                return false;
            }
        }

        // Deletes a customer, protecting against foreign key constraints
        public bool DeleteCustomer(int customerId, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                using (var conn = _dbContext.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM Customer WHERE customerID = @id";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", customerId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (SqlException ex)
            {
                // Error 547 is a Foreign Key Constraint Violation
                if (ex.Number == 547)
                {
                    errorMsg = "Cannot delete this customer because they have existing sales records (Invoices).";
                }
                else
                {
                    errorMsg = "Database Error: " + ex.Message;
                }
                return false;
            }
        }
    }
}