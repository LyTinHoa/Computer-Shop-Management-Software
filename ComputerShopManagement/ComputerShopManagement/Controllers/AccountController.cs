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
        // Validates credentials against the database and returns a specific status code
        public int Authenticate(string username, string password)
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
                            return 1; // Success
                        }
                        else
                        {
                            return -2; // Username exists, but Password is incorrect
                        }
                    }
                    else
                    {
                        return -1; // Username does not exist
                    }
                }
            }
        }

        // Logs out user
        public void Logout()
        {
            CurrentUser = null;
        }

        // Hashes password for NFR1 compliance
        public string HashPassword(string password)
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // 2. Fetch all staff for the DataGridView
        public System.Data.DataTable GetAllEmployees()
        {
            System.Data.DataTable dt = new System.Data.DataTable();
            using (var conn = new ComputerShopManagement.Models.DatabaseContext().GetConnection())
            {
                string query = "SELECT staffID, fullName AS [Full Name], username AS [Username], role AS [Role] FROM Staff";
                Microsoft.Data.SqlClient.SqlCommand cmd = new Microsoft.Data.SqlClient.SqlCommand(query, conn);
                Microsoft.Data.SqlClient.SqlDataAdapter da = new Microsoft.Data.SqlClient.SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        // 3. Securely add a new employee (Validates Manager's password first)
        public bool AddEmployee(Staff newStaff, string managerPasswordInput, Staff currentManager, out string errorMsg)
        {
            errorMsg = "";
            // Security Layer: Verify the acting manager's password
            if (this.Authenticate(currentManager.Username, managerPasswordInput) != 1)
            {
                errorMsg = "Authorization Failed: Incorrect Manager Password.";
                return false;
            }

            try
            {
                using (var conn = new ComputerShopManagement.Models.DatabaseContext().GetConnection())
                {
                    conn.Open();
                    // Check if username already exists
                    string checkQuery = "SELECT COUNT(1) FROM Staff WHERE username = @user";
                    using (var checkCmd = new Microsoft.Data.SqlClient.SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@user", newStaff.Username);
                        if ((int)checkCmd.ExecuteScalar() > 0)
                        {
                            errorMsg = "Username already exists in the system.";
                            return false;
                        }
                    }

                    // Insert new staff securely
                    string insertQuery = "INSERT INTO Staff (fullName, username, password, role) VALUES (@name, @user, @pass, @role)";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", newStaff.FullName);
                        cmd.Parameters.AddWithValue("@user", newStaff.Username);
                        cmd.Parameters.AddWithValue("@pass", HashPassword(newStaff.Password)); // Hash the new user's password
                        cmd.Parameters.AddWithValue("@role", newStaff.Role);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = "Database Error: " + ex.Message;
                return false;
            }
        }

        // 4. Securely delete an employee (Validates Manager's password first)
        public bool DeleteEmployee(int targetStaffId, string managerPasswordInput, Staff currentManager, out string errorMsg)
        {
            errorMsg = "";
            // Security Layer: Verify the acting manager's password
            if (this.Authenticate(currentManager.Username, managerPasswordInput) != 1)
            {
                errorMsg = "Authorization Failed: Incorrect Manager Password.";
                return false;
            }

            // Safety check: Manager cannot delete themselves
            if (targetStaffId == currentManager.StaffID)
            {
                errorMsg = "Safety Protocol: You cannot delete your own active account.";
                return false;
            }

            try
            {
                using (var conn = new ComputerShopManagement.Models.DatabaseContext().GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM Staff WHERE staffID = @id";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", targetStaffId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                // If they have associated invoices, foreign key constraints will block deletion.
                errorMsg = "Cannot delete employee. They have existing sales records tied to their account.";
                return false;
            }
        }

        public bool ResetEmployeePassword(int targetStaffId, string newPassword, string managerPasswordInput, Staff currentManager, out string errorMsg)
        {
            errorMsg = "";
            // Security Layer: Verify the acting manager's password using the DB
            if (this.Authenticate(currentManager.Username, managerPasswordInput) != 1)
            {
                errorMsg = "Authorization Failed: Incorrect Manager Password.";
                return false;
            }

            try
            {
                using (var conn = new ComputerShopManagement.Models.DatabaseContext().GetConnection())
                {
                    conn.Open();
                    string query = "UPDATE Staff SET password = @pass WHERE staffID = @id";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@pass", HashPassword(newPassword)); 
                        cmd.Parameters.AddWithValue("@id", targetStaffId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMsg = "Database Error: " + ex.Message;
                return false;
            }
        }
    }
}