using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Controllers
{
    public class AccountController
    {
        private readonly DatabaseContext _dbContext;

        // Stores the currently logged-in user
        public Staff CurrentUser { get; private set; }

        public AccountController()
        {
            _dbContext = new DatabaseContext();
        }

        // Validates credentials against the database
        public bool Authenticate(string username, string password)
        {
            using (var connection = _dbContext.GetConnection())
            {
                string query = "SELECT staffID, fullName, username, role, password FROM Staff WHERE username = @user";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@user", username);

                connection.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string storedHash = reader["password"].ToString();
                        string inputHash = HashPassword(password);

                        // Checks if hashed passwords match
                        if (storedHash == inputHash)
                        {
                            CurrentUser = new Staff
                            {
                                StaffID = Convert.ToInt32(reader["staffID"]),
                                FullName = reader["fullName"].ToString(),
                                Username = reader["username"].ToString(),
                                Role = reader["role"].ToString()
                            };
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Logs out user
        public void Logout()
        {
            CurrentUser = null;
        }

        // Hashes password for NFR1 compliance
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}