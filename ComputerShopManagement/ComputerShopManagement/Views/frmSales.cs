using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Views
{
    // Strongly-typed model for the Cart UI
    public class CartItem
    {
        public int ProductID { get; set; }
        public string Product { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get { return Qty * Price; } }
    }

    public partial class frmSales : Form
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private SalesController _controller;
        private Staff _currentUser;

        private Panel pnlTopBar;
        private DataGridView dgvProducts;
        private DataGridView dgvCart;
        private Label lblTotalAmount;
        private TextBox txtCustomerPhone;
        private Panel btnCheckout;
        private bool isCheckoutHovered = false;

        // Upgraded to a BindingList to support dynamic updates (Aggregation/Deletion)
        private BindingList<CartItem> _cartItems;

        public frmSales(Staff currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _controller = new SalesController();

            _controller.CurrentInvoice.StaffID = _currentUser.StaffID;
            _cartItems = new BindingList<CartItem>();

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

            Button btnClose = new Button { Text = "×", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat, Size = new Size(40, 40), Location = new Point(1135, 10), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(2, 0, 0, 0) };
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

            Label lblCart = new Label { Text = "Current Cart (Double Click to Remove by 1)", Font = new Font("Segoe UI Semibold", 14), ForeColor = deepText, AutoSize = true, Location = new Point(20, 20) };
            pnlRight.Controls.Add(lblCart);

            dgvCart = CreateModernGrid();
            dgvCart.Location = new Point(20, 60);
            dgvCart.Size = new Size(450, 310);
            dgvCart.DataSource = _cartItems; // Bind directly
            dgvCart.CellDoubleClick += DgvCart_CellDoubleClick; // Hook up the removal event
            pnlRight.Controls.Add(dgvCart);

            Label lblPhone = new Label { Text = "Customer Phone:", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, AutoSize = true, Location = new Point(20, 390) };
            pnlRight.Controls.Add(lblPhone);

            txtCustomerPhone = new TextBox { Font = new Font("Segoe UI", 16), Location = new Point(20, 425), Size = new Size(450, 35), BorderStyle = BorderStyle.FixedSingle };
            pnlRight.Controls.Add(txtCustomerPhone);

            Label lblTotalText = new Label { Text = "Total Amount:", Font = new Font("Segoe UI Semibold", 16), ForeColor = deepText, AutoSize = true, Location = new Point(20, 490) };
            pnlRight.Controls.Add(lblTotalText);

            lblTotalAmount = new Label { Text = "$0.00", Font = new Font("Segoe UI Semibold", 16), ForeColor = techBlue, AutoSize = true, Location = new Point(300, 490) };
            pnlRight.Controls.Add(lblTotalAmount);

            btnCheckout = new Panel { Size = new Size(450, 60), Location = new Point(20, 555), Cursor = Cursors.Hand };
            btnCheckout.MouseEnter += (s, e) => { isCheckoutHovered = true; btnCheckout.Invalidate(); };
            btnCheckout.MouseLeave += (s, e) => { isCheckoutHovered = false; btnCheckout.Invalidate(); };
            btnCheckout.Click += BtnCheckout_Click;
            btnCheckout.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                Rectangle rect = new Rectangle(0, 0, btnCheckout.Width - 1, btnCheckout.Height - 1);
                int radius = (int)(btnCheckout.Height * 0.20);

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    Color cNormal = Color.FromArgb(34, 153, 84);
                    Color cHover = Color.FromArgb(46, 204, 113);

                    using (SolidBrush brush = new SolidBrush(isCheckoutHovered ? cHover : cNormal))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                using (Font btnFont = new Font("Segoe UI Semibold", 16))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("PROCESS PAYMENT", btnFont, Brushes.White, rect, sf);
                }
            };
            pnlRight.Controls.Add(btnCheckout);

            FormatCartGrid();
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
                        products.Add(new Product { ProductID = Convert.ToInt32(reader["productID"]), Name = reader["name"].ToString(), Price = Convert.ToDecimal(reader["price"]), StockQuantity = Convert.ToInt32(reader["stockQuantity"]) });
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

        private void FormatCartGrid()
        {
            // 1. Prevent WinForms from guessing the columns and crashing
            dgvCart.AutoGenerateColumns = false;
            dgvCart.Columns.Clear();

            // 2. Explicitly define the columns and map them to our CartItem properties
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", DataPropertyName = "ProductID", Visible = false });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Product", DataPropertyName = "Product", HeaderText = "Product", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", DataPropertyName = "Qty", HeaderText = "Qty", AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", DataPropertyName = "Price", HeaderText = "Price", AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", DataPropertyName = "Subtotal", HeaderText = "Subtotal", AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells });

            // 3. Apply the currency formatting safely
            dgvCart.Columns["Price"].DefaultCellStyle.Format = "C2";
            dgvCart.Columns["Subtotal"].DefaultCellStyle.Format = "C2";
        }

        // ADD TO CART (With Quantity Aggregation)
        private void DgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvProducts.Rows[e.RowIndex];
                int pId = Convert.ToInt32(row.Cells["ProductID"].Value);
                string pName = row.Cells["Name"].Value.ToString();
                decimal pPrice = Convert.ToDecimal(row.Cells["Price"].Value);
                int stock = Convert.ToInt32(row.Cells["StockQuantity"].Value);

                var existingItem = _cartItems.FirstOrDefault(c => c.ProductID == pId);

                if (existingItem != null)
                {
                    if (existingItem.Qty >= stock)
                    {
                        MessageBox.Show("Cannot add more. Exceeds available stock.", "Stock Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    existingItem.Qty++;
                }
                else
                {
                    if (stock <= 0)
                    {
                        MessageBox.Show("Item is out of stock.", "Stock Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _cartItems.Add(new CartItem { ProductID = pId, Product = pName, Qty = 1, Price = pPrice });
                }

                _cartItems.ResetBindings(); // Forces the DataGrid to recalculate the subtotal visually
                UpdateTotalAmount();
            }
        }

        // REMOVE FROM CART (Decrease Quantity)
        private void DgvCart_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var item = _cartItems[e.RowIndex];
                item.Qty--;

                if (item.Qty <= 0)
                {
                    _cartItems.Remove(item);
                }

                _cartItems.ResetBindings();
                UpdateTotalAmount();
            }
        }

        private void UpdateTotalAmount()
        {
            decimal total = _cartItems.Sum(item => item.Subtotal);
            lblTotalAmount.Text = total.ToString("C2");
        }

        // Smart Database Lookup for Customer ID
        private int GetOrCreateCustomer(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return 1; // ID 1 is our Walk-in Customer

            using (var conn = new DatabaseContext().GetConnection())
            {
                conn.Open();
                // Check if customer exists
                SqlCommand checkCmd = new SqlCommand("SELECT customerID FROM Customer WHERE phone = @phone", conn);
                checkCmd.Parameters.AddWithValue("@phone", phone);
                object result = checkCmd.ExecuteScalar();

                if (result != null) return Convert.ToInt32(result);

                // If not, silently create a new profile for them so the Foreign Key doesn't crash
                SqlCommand insertCmd = new SqlCommand("INSERT INTO Customer (fullName, email, phone) OUTPUT INSERTED.customerID VALUES ('New Customer', 'none', @phone)", conn);
                insertCmd.Parameters.AddWithValue("@phone", phone);
                return (int)insertCmd.ExecuteScalar();
            }
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("The cart is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Rebuild the Controller's Invoice Details directly from our aggregated Cart
            _controller.CurrentInvoice.Details.Clear();
            foreach (var item in _cartItems)
            {
                Product p = new Product { ProductID = item.ProductID, Price = item.Price };
                _controller.AddItemsToInvoice(p, item.Qty);
            }

            // 2. Fetch valid Customer ID to prevent Foreign Key crashes
            _controller.CurrentInvoice.CustomerID = GetOrCreateCustomer(txtCustomerPhone.Text.Trim());

            // 3. Execute Transaction
            bool success = _controller.ProcessPayment();

            if (success)
            {
                MessageBox.Show("Payment processed successfully! Stock levels have been updated automatically.", "Transaction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Transaction failed. An error occurred in the database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}