using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ComputerShopManagement.Views
{
    public partial class frmCheckout : Form
    {
        // UI Controls
        private TextBox txtPhone;
        private TextBox txtFullName;
        private TextBox txtEmail;
        private Button btnConfirm;
        private Button btnExit;

        // Public properties for controller access
        public string CustomerPhone => txtPhone?.Text.Trim();
        public string CustomerName => txtFullName?.Text.Trim();
        public string CustomerEmail => txtEmail?.Text.Trim();

        public frmCheckout()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            // Define the slight off-white color for the modal background
            Color modalBackground = Color.WhiteSmoke;

            // 1. Form styling
            this.Size = new Size(480, 430);
            this.BackColor = modalBackground;
            this.StartPosition = FormStartPosition.CenterParent;

            // Apply rounded corners to the form
            this.Region = new Region(GetRoundedPath(new Rectangle(0, 0, this.Width, this.Height), 15));

            // Title Label
            Label lblTitle = new Label { Text = "Customer Information", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(25, 25), AutoSize = true };

            // Exit Button [x] with Hover Effect
            btnExit = new Button { Text = "X", Location = new Point(435, 10), Size = new Size(35, 35), FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, BackColor = modalBackground, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += BtnExit_Click; // Linked to the new warning method
            btnExit.MouseEnter += (s, e) => { btnExit.BackColor = Color.Red; btnExit.ForeColor = Color.White; };
            btnExit.MouseLeave += (s, e) => { btnExit.BackColor = modalBackground; btnExit.ForeColor = Color.Gray; };

            // Phone Field
            Label lblPhone = new Label { Text = "Phone Number (*):", Location = new Point(25, 80), AutoSize = true, Font = new Font("Segoe UI", 11) };
            txtPhone = new TextBox { Location = new Point(25, 110), Width = 430, Font = new Font("Segoe UI", 12) };

            // Full Name Field
            Label lblName = new Label { Text = "Full Name:", Location = new Point(25, 160), AutoSize = true, Font = new Font("Segoe UI", 11) };
            txtFullName = new TextBox { Location = new Point(25, 190), Width = 430, Font = new Font("Segoe UI", 12) };

            // Email Field
            Label lblEmail = new Label { Text = "Email:", Location = new Point(25, 240), AutoSize = true, Font = new Font("Segoe UI", 11) };
            txtEmail = new TextBox { Location = new Point(25, 270), Width = 430, Font = new Font("Segoe UI", 12) };

            // Confirm Button
            btnConfirm = new Button { Text = "Confirm Order", Location = new Point(25, 340), Size = new Size(430, 50), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            // Apply rounded corners to the button
            btnConfirm.Region = new Region(GetRoundedPath(new Rectangle(0, 0, btnConfirm.Width, btnConfirm.Height), 8));

            // Add all controls to form
            this.Controls.AddRange(new Control[] { lblTitle, btnExit, lblPhone, txtPhone, lblName, txtFullName, lblEmail, txtEmail, btnConfirm });
        }

        // Helper method to draw rounded rectangles
        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Exit action with warning check
        private void BtnExit_Click(object sender, EventArgs e)
        {
            // Check if any of the textboxes contain text
            bool hasInput = !string.IsNullOrWhiteSpace(txtPhone.Text) ||
                            !string.IsNullOrWhiteSpace(txtFullName.Text) ||
                            !string.IsNullOrWhiteSpace(txtEmail.Text);

            if (hasInput)
            {
                DialogResult warningResult = MessageBox.Show(
                    "You have entered some information. Are you sure you want to close without confirming the order?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (warningResult == DialogResult.No)
                {
                    return; // Stop the exit process and return to the modal
                }
            }

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // Validate and confirm action
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CustomerPhone))
            {
                MessageBox.Show("Phone number is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhone.Focus();
                return;
            }

            string email = CustomerEmail;
            if (!string.IsNullOrEmpty(email))
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(email, emailPattern))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}