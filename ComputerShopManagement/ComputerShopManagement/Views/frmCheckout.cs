using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Views
{
    public partial class frmCheckout : Form
    {
        private CustomerController _customerCtrl;

        // UI Controls
        private TextBox txtPhone;
        private TextBox txtFullName;
        private TextBox txtEmail;
        private Panel btnConfirm;
        private Panel btnExit;

        // Status Badge UI
        private Panel pnlStatusBadge;
        private string _badgeText = "";
        private Color _badgeColor = Color.Gray;

        // State Tracker to prevent data contamination
        private bool _wasAutoFilled = false;

        // Premium Brand Colors
        private Color techBlue = Color.FromArgb(41, 128, 185);
        private Color deepText = Color.FromArgb(44, 62, 80);
        private Color dangerRed = Color.FromArgb(231, 76, 60);

        // Public properties for SalesController access
        public string CustomerPhone => txtPhone?.Text.Trim();
        public string CustomerName => txtFullName?.Text.Trim();
        public string CustomerEmail => txtEmail?.Text.Trim();

        public frmCheckout()
        {
            InitializeComponent();
            _customerCtrl = new CustomerController();
            this.DoubleBuffered = true;
            SetupUI();
        }

        private void SetupUI()
        {
            Color modalBackground = Color.FromArgb(245, 246, 250); // Sleek light gray/blue tint

            // 1. Form styling
            this.Size = new Size(500, 520);
            this.BackColor = modalBackground;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;

            this.Region = new Region(GetRoundedPath(new Rectangle(0, 0, this.Width, this.Height), 20));

            // Title Label
            Label lblTitle = new Label { Text = "Customer Information", Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(35, 25), AutoSize = true, ForeColor = deepText };
            this.Controls.Add(lblTitle);

            // Helper to create beautiful rounded input boxes
            Action<TextBox, int> CreateRoundedInput = (txt, yPos) => {
                Panel wrapper = new Panel { Size = new Size(430, 48), Location = new Point(35, yPos), BackColor = Color.Transparent };
                wrapper.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath p = GetRoundedPath(new Rectangle(0, 0, wrapper.Width - 1, wrapper.Height - 1), 15))
                    {
                        e.Graphics.FillPath(Brushes.White, p);
                        e.Graphics.DrawPath(new Pen(Color.LightGray, 1), p);
                    }
                };
                txt.BorderStyle = BorderStyle.None;
                txt.Location = new Point(15, 12);
                txt.Width = 400;
                txt.BackColor = Color.White;
                txt.ForeColor = deepText;
                wrapper.Controls.Add(txt);
                this.Controls.Add(wrapper);
            };

            // Phone Field 
            Label lblPhone = new Label { Text = "Phone Number *", Font = new Font("Segoe UI Semibold", 11), Location = new Point(35, 90), AutoSize = true, ForeColor = Color.Gray };
            this.Controls.Add(lblPhone);

            // Dynamic Status Badge
            pnlStatusBadge = new Panel { Size = new Size(120, 26), Location = new Point(lblPhone.Right + 10, 88), BackColor = modalBackground, Visible = false };
            EnableDoubleBuffering(pnlStatusBadge);
            pnlStatusBadge.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pnlStatusBadge.Width - 1, pnlStatusBadge.Height - 1), 13))
                {
                    e.Graphics.FillPath(new SolidBrush(_badgeColor), path);
                }
                TextRenderer.DrawText(e.Graphics, _badgeText, new Font("Segoe UI Semibold", 9), new Rectangle(0, 0, pnlStatusBadge.Width, pnlStatusBadge.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            this.Controls.Add(pnlStatusBadge);

            txtPhone = new TextBox { Font = new Font("Segoe UI Semibold", 13) };
            txtPhone.MaxLength = 11;
            txtPhone.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) { e.Handled = true; } };
            txtPhone.TextChanged += TxtPhone_TextChanged;
            CreateRoundedInput(txtPhone, 120);

            // Name Field 
            Label lblName = new Label { Text = "Full Name *", Font = new Font("Segoe UI Semibold", 11), Location = new Point(35, 185), AutoSize = true, ForeColor = Color.Gray };
            this.Controls.Add(lblName);
            txtFullName = new TextBox { Font = new Font("Segoe UI Semibold", 13) };
            CreateRoundedInput(txtFullName, 215);

            // Email Field
            Label lblEmail = new Label { Text = "Email Address (Optional)", Font = new Font("Segoe UI Semibold", 11), Location = new Point(35, 280), AutoSize = true, ForeColor = Color.Gray };
            this.Controls.Add(lblEmail);
            txtEmail = new TextBox { Font = new Font("Segoe UI Semibold", 13) };
            CreateRoundedInput(txtEmail, 310);

            // Custom Confirm Button
            btnConfirm = new Panel { Size = new Size(430, 50), Location = new Point(35, 390), Cursor = Cursors.Hand, BackColor = modalBackground };
            EnableDoubleBuffering(btnConfirm);
            bool isConfirmHovered = false;
            btnConfirm.MouseEnter += (s, e) => { isConfirmHovered = true; btnConfirm.Invalidate(); };
            btnConfirm.MouseLeave += (s, e) => { isConfirmHovered = false; btnConfirm.Invalidate(); };
            btnConfirm.Click += BtnConfirm_Click;
            btnConfirm.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath p = GetRoundedPath(new Rectangle(0, 0, btnConfirm.Width - 1, btnConfirm.Height - 1), 15))
                {
                    e.Graphics.FillPath(new SolidBrush(isConfirmHovered ? Color.FromArgb(31, 97, 141) : techBlue), p);
                }
                TextRenderer.DrawText(e.Graphics, "Confirm Order", new Font("Segoe UI Semibold", 13), new Rectangle(0, 0, btnConfirm.Width, btnConfirm.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            this.Controls.Add(btnConfirm);

            // Custom Cancel Button
            btnExit = new Panel { Size = new Size(430, 45), Location = new Point(35, 450), Cursor = Cursors.Hand, BackColor = modalBackground };
            EnableDoubleBuffering(btnExit);
            bool isExitHovered = false;
            btnExit.MouseEnter += (s, e) => { isExitHovered = true; btnExit.Invalidate(); };
            btnExit.MouseLeave += (s, e) => { isExitHovered = false; btnExit.Invalidate(); };
            btnExit.Click += BtnExit_Click;
            btnExit.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath p = GetRoundedPath(new Rectangle(0, 0, btnExit.Width - 1, btnExit.Height - 1), 15))
                {
                    e.Graphics.FillPath(new SolidBrush(isExitHovered ? dangerRed : Color.FromArgb(220, 224, 230)), p);
                }
                TextRenderer.DrawText(e.Graphics, "Cancel", new Font("Segoe UI Semibold", 12), new Rectangle(0, 0, btnExit.Width, btnExit.Height), isExitHovered ? Color.White : deepText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            this.Controls.Add(btnExit);
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, control, new object[] { true });
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(rect); return path; }
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void TxtPhone_TextChanged(object sender, EventArgs e)
        {
            string phone = txtPhone.Text.Trim();
            if (phone.Length >= 10)
            {
                Customer customer = _customerCtrl.GetCustomerByPhone(phone);
                if (customer != null)
                {
                    txtFullName.Text = customer.FullName;
                    txtEmail.Text = customer.Email;
                    _wasAutoFilled = true;
                    ShowBadge("Customer Found", Color.FromArgb(46, 204, 113));
                }
                else
                {
                    if (_wasAutoFilled) { txtFullName.Clear(); txtEmail.Clear(); _wasAutoFilled = false; }
                    ShowBadge("New Customer", techBlue);
                }
            }
            else
            {
                if (_wasAutoFilled) { txtFullName.Clear(); txtEmail.Clear(); _wasAutoFilled = false; }
                pnlStatusBadge.Visible = false;
            }
        }

        private void ShowBadge(string text, Color color)
        {
            _badgeText = text;
            _badgeColor = color;
            pnlStatusBadge.Visible = true;
            pnlStatusBadge.Invalidate();
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPhone.Text) || !string.IsNullOrWhiteSpace(txtFullName.Text) || !string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                if (MessageBox.Show("You have entered some information. Are you sure you want to close without confirming the order?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CustomerPhone) || CustomerPhone.Length < 10)
            {
                MessageBox.Show("Please enter a valid phone number containing at least 10 digits.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhone.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                MessageBox.Show("Full Name is mandatory for checkout.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFullName.Focus(); return;
            }
            string email = CustomerEmail;
            if (!string.IsNullOrEmpty(email))
            {
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus(); return;
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
