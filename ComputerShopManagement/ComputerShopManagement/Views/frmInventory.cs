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
    public partial class frmInventory : Form
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_NCHITTEST = 0x0084;
        private const int RESIZE_HANDLE_SIZE = 10;

        private InventoryController _controller;
        private Staff _currentUser;

        private Panel pnlTopBar;
        private DataGridView dgvInventory;
        private TextBox txtName, txtCategory, txtPrice, txtStock;
        private TextBox txtCpu, txtRam, txtStorage, txtGpu;
        private Panel btnAddProduct, btnClearForm;
        private Button btnMaximize;
        private bool isAddHovered = false, isClearHovered = false;

        public frmInventory(Staff currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _controller = new InventoryController();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupModernInventoryUI();
            LoadInventoryData();
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

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if (this.WindowState == FormWindowState.Normal)
                {
                    Point cursor = this.PointToClient(Cursor.Position);
                    if (cursor.X <= RESIZE_HANDLE_SIZE && cursor.Y <= RESIZE_HANDLE_SIZE) m.Result = (IntPtr)13;
                    else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE && cursor.Y <= RESIZE_HANDLE_SIZE) m.Result = (IntPtr)14;
                    else if (cursor.X <= RESIZE_HANDLE_SIZE && cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE) m.Result = (IntPtr)16;
                    else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE && cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE) m.Result = (IntPtr)17;
                    else if (cursor.X <= RESIZE_HANDLE_SIZE) m.Result = (IntPtr)10;
                    else if (cursor.X >= this.ClientSize.Width - RESIZE_HANDLE_SIZE) m.Result = (IntPtr)11;
                    else if (cursor.Y <= RESIZE_HANDLE_SIZE) m.Result = (IntPtr)12;
                    else if (cursor.Y >= this.ClientSize.Height - RESIZE_HANDLE_SIZE) m.Result = (IntPtr)15;
                }
                return;
            }
            base.WndProc(ref m);
        }

        private void SetupModernInventoryUI()
        {
            this.Size = new Size(1200, 750);
            this.MinimumSize = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(41, 128, 185);

            Color deepText = Color.FromArgb(44, 62, 80);

            int pad = 2; // 2px invisible grip padding

            // --- ABSOLUTE GEOMETRY ---
            pnlTopBar = new Panel { Location = new Point(pad, pad), Size = new Size(this.ClientSize.Width - (pad * 2), 60), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlTopBar.MouseDown += DragWindow_MouseDown;
            this.Controls.Add(pnlTopBar);

            Panel pnlBackground = new Panel { Location = new Point(pad, pad + 60), Size = new Size(this.ClientSize.Width - (pad * 2), this.ClientSize.Height - (pad * 2) - 60), BackColor = Color.FromArgb(245, 246, 250), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.Add(pnlBackground);

            // --- TOP BAR CONTENTS ---
            Label lblTitle = new Label { Text = "BitTekk | Inventory Manager", Font = new Font("Segoe UI Semibold", 16), ForeColor = deepText, AutoSize = true, Location = new Point(20, 15) };
            lblTitle.MouseDown += DragWindow_MouseDown;
            pnlTopBar.Controls.Add(lblTitle);

            Button btnClose = new Button { Text = "×", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat, Size = new Size(40, 40), Location = new Point(pnlTopBar.Width - 45, 10), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(2, 0, 0, 0) };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.Red; };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.Gray; btnClose.BackColor = Color.White; };
            btnClose.Click += (s, e) => this.Close();
            pnlTopBar.Controls.Add(btnClose);

            btnMaximize = new Button { Text = "☐", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat, Size = new Size(40, 40), Location = new Point(pnlTopBar.Width - 85, 10), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Padding = new Padding(2, 0, 0, 0) };
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.MouseEnter += (s, e) => { btnMaximize.BackColor = Color.FromArgb(235, 235, 235); };
            btnMaximize.MouseLeave += (s, e) => { btnMaximize.BackColor = Color.White; };
            btnMaximize.Click += (s, e) => ToggleMaximize();
            pnlTopBar.Controls.Add(btnMaximize);
            pnlTopBar.DoubleClick += (s, e) => ToggleMaximize();

            // --- BACKGROUND CONTENTS ---
            Panel pnlLeft = new Panel { Location = new Point(20, 20), Size = new Size(pnlBackground.Width - 480, pnlBackground.Height - 40), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            pnlBackground.Controls.Add(pnlLeft);

            // FIX: Removed the low stock warning text
            Label lblCatalog = new Label { Text = "Product Catalog", Font = new Font("Segoe UI Semibold", 14), ForeColor = deepText, AutoSize = true, Location = new Point(20, 20) };
            pnlLeft.Controls.Add(lblCatalog);

            dgvInventory = CreateModernGrid();
            dgvInventory.Location = new Point(20, 60);
            dgvInventory.Size = new Size(pnlLeft.Width - 40, pnlLeft.Height - 80);
            dgvInventory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvInventory.CellClick += DgvInventory_CellClick;
            pnlLeft.Controls.Add(dgvInventory);

            Panel pnlRight = new Panel { Location = new Point(pnlBackground.Width - 440, 20), Size = new Size(420, pnlBackground.Height - 40), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right };
            pnlBackground.Controls.Add(pnlRight);

            Label lblFormTitle = new Label { Text = "Add / View Product", Font = new Font("Segoe UI Semibold", 14), ForeColor = deepText, AutoSize = true, Location = new Point(20, 20) };
            pnlRight.Controls.Add(lblFormTitle);

            int startY = 70;
            int spacing = 65;

            txtName = CreateLabeledInput(pnlRight, "Product Name:", startY, 380);
            txtCategory = CreateLabeledInput(pnlRight, "Category (e.g., GPU, CPU):", startY + spacing, 380);
            txtPrice = CreateLabeledInput(pnlRight, "Price ($):", startY + (spacing * 2), 180);
            txtStock = CreateLabeledInput(pnlRight, "Initial Stock:", startY + (spacing * 2), 180, 200);

            Label lblSpecs = new Label { Text = "Hardware Specifications (Optional)", Font = new Font("Segoe UI Semibold", 12), ForeColor = Color.Gray, AutoSize = true, Location = new Point(20, startY + (spacing * 3) + 10) };
            pnlRight.Controls.Add(lblSpecs);

            int specY = startY + (spacing * 3) + 40;
            txtCpu = CreateLabeledInput(pnlRight, "Processor (CPU):", specY, 180);
            txtRam = CreateLabeledInput(pnlRight, "Memory (RAM):", specY, 180, 200);
            txtStorage = CreateLabeledInput(pnlRight, "Storage Space:", specY + spacing, 180);
            txtGpu = CreateLabeledInput(pnlRight, "Graphics (GPU):", specY + spacing, 180, 200);

            btnAddProduct = CreateCustomButton(pnlRight, "ADD NEW PRODUCT", Color.FromArgb(41, 128, 185), Color.FromArgb(52, 152, 219), new Point(20, pnlRight.Height - 150), ref isAddHovered);
            btnAddProduct.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnAddProduct.Click += BtnAddProduct_Click;

            btnClearForm = CreateCustomButton(pnlRight, "CLEAR FORM", Color.FromArgb(149, 165, 166), Color.FromArgb(189, 195, 199), new Point(20, pnlRight.Height - 75), ref isClearHovered);
            btnClearForm.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnClearForm.Click += (s, e) => ClearForm();
        }

        private TextBox CreateLabeledInput(Panel parent, string labelText, int y, int width, int xOffset = 0)
        {
            Label lbl = new Label { Text = labelText, Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, AutoSize = true, Location = new Point(20 + xOffset, y) };
            parent.Controls.Add(lbl);
            TextBox txt = new TextBox { Font = new Font("Segoe UI", 12), Location = new Point(20 + xOffset, y + 25), Size = new Size(width, 30), BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
            return txt;
        }

        private Panel CreateCustomButton(Panel parent, string text, Color normalColor, Color hoverColor, Point location, ref bool hoverFlag)
        {
            Panel btn = new Panel { Size = new Size(380, 55), Location = location, Cursor = Cursors.Hand };
            bool localHover = false;
            btn.MouseEnter += (s, e) => { localHover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { localHover = false; btn.Invalidate(); };
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                int radius = (int)(btn.Height * 0.20);
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90); path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90); path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90); path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90); path.CloseFigure();
                    using (SolidBrush brush = new SolidBrush(localHover ? hoverColor : normalColor)) { e.Graphics.FillPath(brush, path); }
                }
                using (Font btnFont = new Font("Segoe UI Semibold", 14)) { StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; e.Graphics.DrawString(text, btnFont, Brushes.White, rect, sf); }
            };
            parent.Controls.Add(btn);
            return btn;
        }

        private DataGridView CreateModernGrid()
        {
            DataGridView grid = new DataGridView { BackgroundColor = Color.White, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, EnableHeadersVisualStyles = false, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToResizeColumns = false, AllowUserToResizeRows = false, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, ColumnHeadersHeight = 45, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True; grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250); grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(44, 62, 80); grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 11);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11); grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 245, 251); grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80); grid.DefaultCellStyle.Padding = new Padding(5, 10, 5, 10);
            return grid;
        }

        private void LoadInventoryData()
        {
            DatabaseContext db = new DatabaseContext(); System.Data.DataTable dt = new System.Data.DataTable();
            using (var conn = db.GetConnection())
            {
                string query = @"SELECT p.productID, p.name AS Name, p.category AS Category, p.price AS Price, p.stockQuantity AS Stock, h.cpu, h.ram, h.storage, h.gpu FROM Products p LEFT JOIN HardwareSpecs h ON p.productID = h.productID";
                SqlCommand cmd = new SqlCommand(query, conn); SqlDataAdapter adapter = new SqlDataAdapter(cmd); adapter.Fill(dt);
            }
            dgvInventory.DataSource = dt;

            dgvInventory.Columns["productID"].Visible = false; dgvInventory.Columns["cpu"].Visible = false; dgvInventory.Columns["ram"].Visible = false; dgvInventory.Columns["storage"].Visible = false; dgvInventory.Columns["gpu"].Visible = false;
            dgvInventory.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; dgvInventory.Columns["Category"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells; dgvInventory.Columns["Price"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells; dgvInventory.Columns["Stock"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dgvInventory.Columns["Price"].DefaultCellStyle.Format = "C2";

            // Loop for red highlighting has been fully removed.
        }

        private void DgvInventory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvInventory.Rows[e.RowIndex];
                txtName.Text = row.Cells["Name"].Value.ToString(); txtCategory.Text = row.Cells["Category"].Value.ToString(); txtPrice.Text = Convert.ToDecimal(row.Cells["Price"].Value).ToString("0.00"); txtStock.Text = row.Cells["Stock"].Value.ToString();
                txtCpu.Text = row.Cells["cpu"].Value?.ToString() ?? ""; txtRam.Text = row.Cells["ram"].Value?.ToString() ?? ""; txtStorage.Text = row.Cells["storage"].Value?.ToString() ?? ""; txtGpu.Text = row.Cells["gpu"].Value?.ToString() ?? "";
            }
        }

        private void BtnAddProduct_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPrice.Text) || string.IsNullOrWhiteSpace(txtStock.Text)) { MessageBox.Show("Please fill out Name, Price, and Stock.", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                Product newProd = new Product { Name = txtName.Text.Trim(), Category = txtCategory.Text.Trim(), Price = decimal.Parse(txtPrice.Text.Trim()), StockQuantity = int.Parse(txtStock.Text.Trim()), Specs = new HardwareSpec { Cpu = string.IsNullOrWhiteSpace(txtCpu.Text) ? null : txtCpu.Text.Trim(), Ram = string.IsNullOrWhiteSpace(txtRam.Text) ? null : txtRam.Text.Trim(), Storage = string.IsNullOrWhiteSpace(txtStorage.Text) ? null : txtStorage.Text.Trim(), Gpu = string.IsNullOrWhiteSpace(txtGpu.Text) ? null : txtGpu.Text.Trim() } };
                if (_controller.AddNewProduct(newProd)) { MessageBox.Show("Product added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); LoadInventoryData(); ClearForm(); }
            }
            catch (FormatException) { MessageBox.Show("Price and Stock must be numbers.", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ClearForm() { txtName.Clear(); txtCategory.Clear(); txtPrice.Clear(); txtStock.Clear(); txtCpu.Clear(); txtRam.Clear(); txtStorage.Clear(); txtGpu.Clear(); txtName.Focus(); }
        private void DragWindow_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } }
        private void ToggleMaximize() { if (this.WindowState == FormWindowState.Normal) { this.WindowState = FormWindowState.Maximized; btnMaximize.Text = "❐"; } else { this.WindowState = FormWindowState.Normal; btnMaximize.Text = "☐"; } }
    }
}