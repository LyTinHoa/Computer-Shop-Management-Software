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
            pnlTopBar = new Panel { Location = new Point(pad, pad), Size = new Size(this.ClientSize.Width - (pad * 2), 80), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlTopBar.MouseDown += DragWindow_MouseDown;
            this.Controls.Add(pnlTopBar);

            Panel pnlBackground = new Panel { Location = new Point(pad, pad + 80), Size = new Size(this.ClientSize.Width - (pad * 2), this.ClientSize.Height - (pad * 2) - 80), BackColor = Color.FromArgb(245, 246, 250), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right }; this.Controls.Add(pnlBackground);

            // --- TOP BAR CONTENTS ---
            pnlTopBar.Paint += (s, e) => {
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230), 1), 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            };

            Label lblTitle = new Label { Text = "BitTekk | Inventory Manager", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0), Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = deepText, BackColor = Color.Transparent };
            lblTitle.MouseDown += DragWindow_MouseDown;
            pnlTopBar.Controls.Add(lblTitle);

            Panel pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 150 };
            pnlTopBar.Controls.Add(pnlWindowControls);
            pnlWindowControls.BringToFront();

            Action<Button, string> SetupWindowBtn = (btn, type) => {
                btn.Size = new Size(50, 45); // Matched to Sales View
                btn.Location = new Point(type == "Min" ? 0 : type == "Max" ? 50 : 100, 0);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Cursor = Cursors.Hand;
                btn.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(type == "Close" && btn.BackColor == Color.FromArgb(231, 76, 60) ? Color.White : Color.FromArgb(80, 80, 80), 2.5f))
                    {
                        int cx = 25, cy = 22; // Matched to Sales View
                        if (type == "Min") e.Graphics.DrawLine(pen, cx - 7, cy + 5, cx + 7, cy + 5);
                        else if (type == "Max") e.Graphics.DrawRectangle(pen, cx - 6, cy - 5, 12, 10);
                        else if (type == "Close")
                        {
                            e.Graphics.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6);
                            e.Graphics.DrawLine(pen, cx + 6, cy - 6, cx - 6, cy + 6);
                        }
                    }
                };
                btn.MouseEnter += (s, e) => { btn.BackColor = type == "Close" ? Color.FromArgb(231, 76, 60) : Color.FromArgb(220, 220, 220); };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.White; };
            };

            Button btnMin = new Button(); SetupWindowBtn(btnMin, "Min");
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnMaximize = new Button(); SetupWindowBtn(btnMaximize, "Max");
            btnMaximize.Click += (s, e) => ToggleMaximize();

            Button btnClose = new Button(); SetupWindowBtn(btnClose, "Close");
            btnClose.Click += (s, e) => {
                if (MessageBox.Show("Are you sure you want to exit the application?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();

            pnlWindowControls.Controls.Add(btnClose);
            pnlWindowControls.Controls.Add(btnMaximize);
            pnlWindowControls.Controls.Add(btnMin);

            pnlTopBar.DoubleClick += (s, e) => ToggleMaximize();

            // --- BACKGROUND CONTENTS ---
            Panel pnlLeft = new Panel { Location = new Point(20, 20), Size = new Size(pnlBackground.Width - 480, pnlBackground.Height - 40), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            pnlBackground.Controls.Add(pnlLeft);

            Label lblCatalog = new Label { Text = "Product Catalog", Font = new Font("Segoe UI Semibold", 14), ForeColor = deepText, AutoSize = true, Location = new Point(20, 20) };
            pnlLeft.Controls.Add(lblCatalog);

            dgvInventory = CreateModernGrid();
            dgvInventory.Location = new Point(20, 60);
            dgvInventory.Size = new Size(pnlLeft.Width - 40, pnlLeft.Height - 140);
            dgvInventory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvInventory.CellClick += DgvInventory_CellClick;

            // --- BACK BUTTON ---
            Panel btnBack = new Panel
            {
                Size = new Size(110, 45),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Location = new Point(20, pnlLeft.Height - 65),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Enable double buffering dynamically to prevent hover flickering
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, btnBack, new object[] { true });

            bool isBackHovered = false;
            btnBack.MouseEnter += (s, e) => { if (!isBackHovered) { isBackHovered = true; btnBack.Invalidate(); } };
            btnBack.MouseLeave += (s, e) => { if (isBackHovered) { isBackHovered = false; btnBack.Invalidate(); } };
            btnBack.Click += (s, e) => this.Close();

            btnBack.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int radius = 20;
                Rectangle rect = new Rectangle(0, 0, btnBack.Width - 1, btnBack.Height - 1);

                // Draw rounded pill shape
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    e.Graphics.FillPath(new SolidBrush(isBackHovered ? Color.FromArgb(31, 97, 141) : Color.FromArgb(41, 128, 185)), path);
                }

                TextRenderer.DrawText(e.Graphics, "← Back", new Font("Segoe UI Semibold", 12), rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            pnlLeft.Controls.Add(btnBack);

            // --- CUSTOM CATEGORY FILTER DROPDOWN ---
            btnFilterDropdown = new Button
            {
                Text = "", // Removed hardcoded text to manually draw and prevent overlapping
                Font = new Font("Segoe UI Semibold", 12),
                Size = new Size(165, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(44, 62, 80),
                Cursor = Cursors.Hand
            };
            btnFilterDropdown.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnFilterDropdown.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 246, 250);

            // Dynamically draw the icons and text with precise alignment
            btnFilterDropdown.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // 1. Draw Funnel Icon (Aligned to the left)
                int w = 15; // Icon Width
                int h = 15; // Icon Height
                int x = 8; // Padding from left edge
                int y = (btnFilterDropdown.Height - h) / 2; // Center vertically

                using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    PointF[] points = {
                        new PointF(x, y),               // Top-Left
                        new PointF(x + w, y),           // Top-Right
                        new PointF(x + 10, y + 8),      // Right Taper End
                        new PointF(x + 10, y + 12),     // Right Spout Bottom
                        new PointF(x + 6, y + 16),      // Left Spout Bottom
                        new PointF(x + 6, y + 8)        // Left Taper End
                    };

                    using (Pen pen = new Pen(btnFilterDropdown.ForeColor, 1.5f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
                    using (SolidBrush brush = new SolidBrush(btnFilterDropdown.ForeColor))
                    {
                        e.Graphics.FillPolygon(brush, points);
                        e.Graphics.DrawPolygon(pen, points);
                    }
                }

                // 2. Draw "By Category" Text (Centered, with slight padding from the funnel icon)
                string text = "By Category";
                using (SolidBrush textBrush = new SolidBrush(btnFilterDropdown.ForeColor))
                {
                    SizeF textSize = e.Graphics.MeasureString(text, btnFilterDropdown.Font);
                    float textX = x + w + 1; // padding from the funnel icon
                    float textY = (btnFilterDropdown.Height - textSize.Height) / 2;
                    e.Graphics.DrawString(text, btnFilterDropdown.Font, textBrush, textX, textY);
                }

                // 3. Draw Downward Arrow (Aligned to the right, far from the text)
                int arrowWidth = 10;
                int arrowHeight = 6;
                int arrowX = btnFilterDropdown.Width - arrowWidth - 12; // 12px padding from the right edge
                int arrowY = (btnFilterDropdown.Height - arrowHeight) / 2 + 2; // +2 to visually center the triangle

                PointF[] arrowPoints = {
                    new PointF(arrowX, arrowY),                             // Top-Left
                    new PointF(arrowX + arrowWidth, arrowY),                // Top-Right
                    new PointF(arrowX + (arrowWidth / 2f), arrowY + arrowHeight) // Bottom-Center
                };

                using (SolidBrush arrowBrush = new SolidBrush(btnFilterDropdown.ForeColor))
                {
                    e.Graphics.FillPolygon(arrowBrush, arrowPoints);
                }
            };
            pnlLeft.Controls.Add(btnFilterDropdown);

            // Keeps the button anchored nicely on the right side above the grid
            pnlLeft.Resize += (s, evt) => btnFilterDropdown.Location = new Point(pnlLeft.Width - 200, 15);

            pnlFilterPopup = new Panel
            {
                Size = new Size(165, 240),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Panel pnlFilterBottom = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(245, 246, 250) };
            Button btnResetFilter = new Button
            {
                Text = "Reset Filters",
                Font = new Font("Segoe UI Semibold", 10),
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(231, 76, 60),
                Cursor = Cursors.Hand
            };
            btnResetFilter.FlatAppearance.BorderSize = 0;
            btnResetFilter.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 235, 235); // Soft red hover highlight
            btnResetFilter.Click += (s, evt) => {
                for (int i = 0; i < clbCategories.Items.Count; i++) clbCategories.SetItemChecked(i, false);
                ApplyCategoryFilter();
            };
            pnlFilterBottom.Controls.Add(btnResetFilter);

            // Container to push the checkboxes to the right using Padding
            Panel pnlListWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 10, 5, 5),
                BackColor = Color.White
            };

            clbCategories = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(44, 62, 80),
                CheckOnClick = true,
                BackColor = Color.White
            };

            // BeginInvoke ensures the filter applies AFTER the checkmark state actually updates
            clbCategories.ItemCheck += (s, evt) => this.BeginInvoke((MethodInvoker)delegate { ApplyCategoryFilter(); });

            pnlListWrapper.Controls.Add(clbCategories);

            pnlFilterPopup.Controls.Add(pnlListWrapper);
            pnlFilterPopup.Controls.Add(pnlFilterBottom);
            pnlLeft.Controls.Add(pnlFilterPopup);
            pnlFilterPopup.BringToFront();

            btnFilterDropdown.Click += (s, evt) => {
                // Snaps the dropdown under the button seamlessly
                pnlFilterPopup.Location = new Point(btnFilterDropdown.Left, btnFilterDropdown.Bottom - 1);
                pnlFilterPopup.Visible = !pnlFilterPopup.Visible;
                pnlFilterPopup.BringToFront();
            };
            // ---------------------------------------
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

        // Filter UI Components
        private Button btnFilterDropdown;
        private Panel pnlFilterPopup;
        private CheckedListBox clbCategories;

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

            // 1. Set Standard Header Colors
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(44, 62, 80);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 11);

            // 2. Stop header from highlighting blue when a cell's content is clicked
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 246, 250);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);

            // 3. Set Standard Cell Colors
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 245, 251);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);
            grid.DefaultCellStyle.Padding = new Padding(5, 10, 5, 10);

            // 4. Add events to manually highlight the header ONLY when it is physically pressed
            grid.CellMouseDown += (s, e) => {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    grid.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.FromArgb(225, 230, 235); // Apply darker highlight
                }
            };

            grid.CellMouseUp += (s, e) => {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    grid.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.FromArgb(245, 246, 250); // Revert to normal
                }
            };

            grid.CellMouseLeave += (s, e) => {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    grid.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.FromArgb(245, 246, 250); // Revert to normal
                }
            };

            // Attach custom delete button rendering and click logic
            grid.CellPainting += DgvInventory_CellPainting;
            grid.CellClick += DgvInventory_DeleteClick;

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

            // --- POPULATE CATEGORY FILTER LIST ---
            clbCategories.Items.Clear();
            HashSet<string> uniqueCats = new HashSet<string>();
            foreach (System.Data.DataRow row in dt.Rows)
            {
                string cat = row["Category"].ToString();
                if (!string.IsNullOrWhiteSpace(cat)) uniqueCats.Add(cat);
            }
            foreach (string cat in uniqueCats) clbCategories.Items.Add(cat);
            // -------------------------------------

            dgvInventory.Columns["productID"].Visible = false; dgvInventory.Columns["cpu"].Visible = false; dgvInventory.Columns["ram"].Visible = false; dgvInventory.Columns["storage"].Visible = false; dgvInventory.Columns["gpu"].Visible = false;
            dgvInventory.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Use AllCells and disable wrapping to ensure sort arrows don't compress the text
            dgvInventory.Columns["Category"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvInventory.Columns["Category"].DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            dgvInventory.Columns["Price"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvInventory.Columns["Price"].DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            dgvInventory.Columns["Stock"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvInventory.Columns["Stock"].DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            // --- ADD DELETE COLUMN ---
            if (!dgvInventory.Columns.Contains("DeleteAction"))
            {
                // Use a standard Text column instead of a Button column
                DataGridViewTextBoxColumn btnDelete = new DataGridViewTextBoxColumn();
                btnDelete.Name = "DeleteAction";
                btnDelete.HeaderText = "Action";
                btnDelete.Width = 80;
                btnDelete.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvInventory.Columns.Add(btnDelete);
            }
        }

        private void DgvInventory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ensure the row actually still exists
            if (e.RowIndex >= 0 && e.RowIndex < dgvInventory.Rows.Count)
            {
                // Ignore click if it was on the Delete column
                if (e.ColumnIndex >= 0 && dgvInventory.Columns[e.ColumnIndex].Name == "DeleteAction")
                {
                    return;
                }

                DataGridViewRow row = dgvInventory.Rows[e.RowIndex];
                txtName.Text = row.Cells["Name"].Value.ToString();
                txtCategory.Text = row.Cells["Category"].Value.ToString();
                txtPrice.Text = Convert.ToDecimal(row.Cells["Price"].Value).ToString("0.00");
                txtStock.Text = row.Cells["Stock"].Value.ToString();

                txtCpu.Text = row.Cells["cpu"].Value?.ToString() ?? "";
                txtRam.Text = row.Cells["ram"].Value?.ToString() ?? "";
                txtStorage.Text = row.Cells["storage"].Value?.ToString() ?? "";
                txtGpu.Text = row.Cells["gpu"].Value?.ToString() ?? "";
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

        private void ApplyCategoryFilter()
        {
            if (dgvInventory.DataSource is System.Data.DataTable dt)
            {
                if (clbCategories.CheckedItems.Count == 0)
                {
                    dt.DefaultView.RowFilter = ""; // No filters applied
                }
                else
                {
                    List<string> selected = new List<string>();
                    foreach (var item in clbCategories.CheckedItems)
                    {
                        // Safely format the category string for the SQL-like RowFilter syntax
                        selected.Add($"'{item.ToString().Replace("'", "''")}'");
                    }
                    dt.DefaultView.RowFilter = $"Category IN ({string.Join(",", selected)})";
                }
            }
        }

        private void DragWindow_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } }

        private void ToggleMaximize() { if (this.WindowState == FormWindowState.Normal) { this.WindowState = FormWindowState.Maximized; btnMaximize.Text = "❐"; } else { this.WindowState = FormWindowState.Normal; btnMaximize.Text = "☐"; } }

        private void DgvInventory_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Only draw inside the DeleteAction column, ignoring the header row
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvInventory.Columns[e.ColumnIndex].Name == "DeleteAction")
            {
                // Paint background/selection but skip text foreground to hide default button visuals
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                int paddingX = 15;
                int paddingY = 8;
                Rectangle rect = new Rectangle(e.CellBounds.X + paddingX, e.CellBounds.Y + paddingY, e.CellBounds.Width - (paddingX * 2), e.CellBounds.Height - (paddingY * 2));

                // Dynamic border radius: 30% of the box's height
                int radius = (int)(rect.Height * 0.30);
                if (radius <= 0) radius = 1;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(231, 76, 60)), path);
                }

                // Draw trash vector icon
                using (Pen pen = new Pen(Color.White, 2f))
                {
                    int cx = e.CellBounds.X + (e.CellBounds.Width / 2);
                    int cy = e.CellBounds.Y + (e.CellBounds.Height / 2);

                    e.Graphics.DrawLine(pen, cx - 8, cy - 6, cx + 8, cy - 6);
                    e.Graphics.DrawLine(pen, cx - 3, cy - 6, cx - 3, cy - 9);
                    e.Graphics.DrawLine(pen, cx + 3, cy - 6, cx + 3, cy - 9);
                    e.Graphics.DrawLine(pen, cx - 3, cy - 9, cx + 3, cy - 9);
                    e.Graphics.DrawLine(pen, cx - 6, cy - 4, cx - 4, cy + 8);
                    e.Graphics.DrawLine(pen, cx + 6, cy - 4, cx + 4, cy + 8);
                    e.Graphics.DrawLine(pen, cx - 4, cy + 8, cx + 4, cy + 8);
                    e.Graphics.DrawLine(pen, cx - 1, cy - 2, cx - 1, cy + 5);
                    e.Graphics.DrawLine(pen, cx + 2, cy - 2, cx + 2, cy + 5);
                }

                e.Handled = true;
            }
        }

        private void DgvInventory_DeleteClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvInventory.Columns[e.ColumnIndex].Name == "DeleteAction")
            {
                if (MessageBox.Show("Are you sure you want to permanently delete this product?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    // Grab the hidden productID from the clicked row
                    int productId = Convert.ToInt32(dgvInventory.Rows[e.RowIndex].Cells["productID"].Value);

                    // Execute Database Deletion and check for success
                    string errorMsg;
                    bool success = _controller.DeleteProduct(productId, out errorMsg);

                    if (success)
                    {
                        LoadInventoryData();
                    }
                    else
                    {
                        MessageBox.Show(errorMsg, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}