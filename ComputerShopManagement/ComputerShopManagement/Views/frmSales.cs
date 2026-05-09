using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Views
{
    public partial class frmSales : Form
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private SalesController _controller;
        private Staff _currentUser;

        // UI Elements
        private Panel pnlTopBar;
        private DataGridView dgvProducts;
        private DataGridView dgvCart;
        private Label lblTotalAmount;
        private TextBox txtCustomerPhone;

        // Upgraded to Panel for custom anti-aliased drawing
        private Panel btnCheckout;
        private bool isCheckoutHovered = false;

        private List<dynamic> _cartBindingList;

        public frmSales(Staff currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _controller = new SalesController();

            _controller.CurrentInvoice.StaffID = _currentUser.StaffID;
            _cartBindingList = new List<dynamic>();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            SetupModernPOSUI();
            LoadProductsFromDatabase();
        }

        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ClassStyle |= 0x00020000; return cp; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int pref = DWMWCP_ROUND;
            DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
        }

        private void SetupModernPOSUI()
        {
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(245, 246, 250);

            Color techBlue = Color.FromArgb(41, 128, 185);
            Color deepText = Color.FromArgb(44, 62, 80);

            // --- TOP BAR ---
            pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            this.Controls.Add(pnlTopBar);

            Label lblTitle = new Label { Text = "BitTekk | Point of Sale", Font = new Font("Segoe UI Semibold", 16), ForeColor = deepText, AutoSize = true, Location = new Point(20, 15) };
            pnlTopBar.Controls.Add(lblTitle);

            // --- UPDATED CLOSE BUTTON (Shifted Left & Visually Centered) ---
            Button btnClose = new Button
            {
                Text = "×",
                Font = new Font("Segoe UI", 16),
                ForeColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(1135, 10), // Shifted 5px further left
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(2, 0, 0, 0) // Nudges the "×" 2px right to perfectly center it visually
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.Red; };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.Gray; btnClose.BackColor = Color.White; };
            btnClose.Click += (s, e) => this.Close();
            pnlTopBar.Controls.Add(btnClose);
            // --- LEFT PANEL: PRODUCT CATALOG ---
            Panel pnlLeft = new Panel { Location = new Point(20, 80), Size = new Size(650, 650), BackColor = Color.White };
            this.Controls.Add(pnlLeft);

            Label lblCatalog = new Label { Text = "Available Products (Double Click to Add)", Font = new Font("Segoe UI Semibold", 14), ForeColor = deepText, AutoSize = true, Location = new Point(20, 20) };
            pnlLeft.Controls.Add(lblCatalog);

            dgvProducts = CreateModernGrid();
            dgvProducts.Location = new Point(20, 60);
            dgvProducts.Size = new Size(610, 570);
            dgvProducts.CellDoubleClick += DgvProducts_CellDoubleClick;
            pnlLeft.Controls.Add(dgvProducts);

            // --- RIGHT PANEL: SHOPPING CART & CHECKOUT ---
            Panel pnlRight = new Panel { Location = new Point(690, 80), Size = new Size(490, 650), BackColor = Color.White };
            this.Controls.Add(pnlRight);

            Label lblCart = new Label { Text = "Current Cart", Font = new Font("Segoe UI Semibold", 14), ForeColor = deepText, AutoSize = true, Location = new Point(20, 20) };
            pnlRight.Controls.Add(lblCart);

            dgvCart = CreateModernGrid();
            dgvCart.Location = new Point(20, 60);
            dgvCart.Size = new Size(450, 310);
            pnlRight.Controls.Add(dgvCart);

            Label lblPhone = new Label { Text = "Customer Phone:", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, AutoSize = true, Location = new Point(20, 390) };
            pnlRight.Controls.Add(lblPhone);

            txtCustomerPhone = new TextBox { Font = new Font("Segoe UI", 16), Location = new Point(20, 425), Size = new Size(450, 35), BorderStyle = BorderStyle.FixedSingle };
            pnlRight.Controls.Add(txtCustomerPhone);

            Label lblTotalText = new Label { Text = "Total Amount:", Font = new Font("Segoe UI Semibold", 16), ForeColor = deepText, AutoSize = true, Location = new Point(20, 490) };
            pnlRight.Controls.Add(lblTotalText);

            lblTotalAmount = new Label { Text = "$0.00", Font = new Font("Segoe UI Semibold", 16), ForeColor = techBlue, AutoSize = true, Location = new Point(300, 490) };
            pnlRight.Controls.Add(lblTotalAmount);

            // --- CUSTOM PROCESS PAYMENT BUTTON (Fixed Margins & 20% Radius) ---
            btnCheckout = new Panel
            {
                Size = new Size(450, 60),
                Location = new Point(20, 555), // Shifted UP to prevent sticking to the bottom
                Cursor = Cursors.Hand
            };

            // Hover logic for the custom button
            btnCheckout.MouseEnter += (s, e) => { isCheckoutHovered = true; btnCheckout.Invalidate(); };
            btnCheckout.MouseLeave += (s, e) => { isCheckoutHovered = false; btnCheckout.Invalidate(); };
            btnCheckout.Click += BtnCheckout_Click;

            // Custom painting for smooth rounded corners and dark green color
            btnCheckout.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                Rectangle rect = new Rectangle(0, 0, btnCheckout.Width - 1, btnCheckout.Height - 1);

                // Calculates border radius dynamically at exactly 20% of the height (12px)
                int radius = (int)(btnCheckout.Height * 0.20);

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    // Darker Emerald Green standard state, slightly lighter on hover
                    Color cNormal = Color.FromArgb(34, 153, 84);
                    Color cHover = Color.FromArgb(46, 204, 113);

                    using (SolidBrush brush = new SolidBrush(isCheckoutHovered ? cHover : cNormal))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                // Perfectly centered light bold text
                using (Font btnFont = new Font("Segoe UI Semibold", 16))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("PROCESS PAYMENT", btnFont, Brushes.White, rect, sf);
                }
            };
            pnlRight.Controls.Add(btnCheckout);
        }

        private DataGridView CreateModernGrid()
        {
            DataGridView grid = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 45,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(44, 62, 80);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 11);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;

            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 245, 251);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);
            grid.DefaultCellStyle.Padding = new Padding(5, 10, 5, 10);

            return grid;
        }

        private void LoadProductsFromDatabase()
        {
            DatabaseContext db = new DatabaseContext();
            List<Product> products = new List<Product>();
            using (var conn = db.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT productID, name, price, stockQuantity FROM Products", conn);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
                        {
                            ProductID = Convert.ToInt32(reader["productID"]),
                            Name = reader["name"].ToString(),
                            Price = Convert.ToDecimal(reader["price"]),
                            StockQuantity = Convert.ToInt32(reader["stockQuantity"])
                        });
                    }
                }
            }
            dgvProducts.DataSource = products;
            dgvProducts.Columns["ProductID"].Visible = false;
            dgvProducts.Columns["Category"].Visible = false;
            dgvProducts.Columns["Specs"].Visible = false;

            dgvProducts.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvProducts.Columns["Price"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dgvProducts.Columns["StockQuantity"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            dgvProducts.Columns["Price"].DefaultCellStyle.Format = "C2";
        }

        private void DgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvProducts.Rows[e.RowIndex];
                Product selectedProduct = new Product
                {
                    ProductID = Convert.ToInt32(row.Cells["ProductID"].Value),
                    Name = row.Cells["Name"].Value.ToString(),
                    Price = Convert.ToDecimal(row.Cells["Price"].Value),
                    StockQuantity = Convert.ToInt32(row.Cells["StockQuantity"].Value)
                };

                _controller.AddItemsToInvoice(selectedProduct, 1);
                _cartBindingList.Add(new { Product = selectedProduct.Name, Qty = 1, Price = selectedProduct.Price, Subtotal = selectedProduct.Price });

                RefreshCartUI();
            }
        }

        private void RefreshCartUI()
        {
            dgvCart.DataSource = null;
            dgvCart.DataSource = _cartBindingList;

            dgvCart.Columns["Product"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCart.Columns["Qty"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dgvCart.Columns["Price"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dgvCart.Columns["Subtotal"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            dgvCart.Columns["Price"].DefaultCellStyle.Format = "C2";
            dgvCart.Columns["Subtotal"].DefaultCellStyle.Format = "C2";

            decimal total = _controller.CalculateFinalTotal();
            lblTotalAmount.Text = total.ToString("C2");
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (_controller.CurrentInvoice.Details.Count == 0)
            {
                MessageBox.Show("The cart is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _controller.CurrentInvoice.CustomerID = string.IsNullOrEmpty(txtCustomerPhone.Text) ? 1 : 2;

            bool success = _controller.ProcessPayment();

            if (success)
            {
                MessageBox.Show("Payment processed successfully! Stock levels have been updated automatically.", "Transaction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Transaction failed. Please check stock availability.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}