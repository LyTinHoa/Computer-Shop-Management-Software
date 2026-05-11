using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Views
{
    public partial class frmSales : Form
    {
        // --- NATIVE WINDOWS 11 DWM & DRAGGING API ---
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private SalesController _controller;
        private List<Product> _catalogProducts;

        // Brand Colors
        private Color techBlue = Color.FromArgb(41, 128, 185);
        private Color deepText = Color.FromArgb(44, 62, 80);
        private Color lightGray = Color.FromArgb(240, 242, 245);
        private Color dangerRed = Color.FromArgb(231, 76, 60);

        // UI Components
        private TableLayoutPanel tlpMainLayout;
        private FlowLayoutPanel flpProducts;
        private TableLayoutPanel tlpCartItems;
        private Label lblGrandTotal;
        private Panel btnClear;
        private Panel btnCheckout;

        // Active Toast Tracker for Real-Time Centering
        private Panel _activeToast;

        public frmSales(SalesController controller)
        {
            _controller = controller;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupResponsiveUI();
            LoadCatalog();

            // Real-Time Centering Hook: If window is maximized, the toast slides with it instantly
            this.Resize += (s, e) => {
                if (_activeToast != null && !_activeToast.IsDisposed)
                {
                    _activeToast.Left = (this.Width - _activeToast.Width) / 2;
                }
            };
        }

        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ClassStyle |= 0x00020000; return cp; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int preference = DWMWCP_ROUND;
            DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, control, new object[] { true });
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

        private void SetupResponsiveUI()
        {
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = lightGray;
            this.Padding = new Padding(2);

            // ==========================================
            // 1. TALL HEADER & FLUSH WINDOW CONTROLS
            // ==========================================
            Panel pnlTitleBar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White };
            pnlTitleBar.Resize += (s, e) => pnlTitleBar.Invalidate();
            pnlTitleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            this.Controls.Add(pnlTitleBar);
            pnlTitleBar.Paint += (s, e) => {
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230), 1), 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
            };

            Label lblTitle = new Label { Text = "BitTekk | Point of Sale", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(30, 0, 0, 0), Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = deepText, BackColor = Color.Transparent };
            lblTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            pnlTitleBar.Controls.Add(lblTitle);

            Panel pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 150 };
            pnlTitleBar.Controls.Add(pnlWindowControls);
            pnlWindowControls.BringToFront();

            Action<Button, string> SetupWindowBtn = (btn, type) => {
                btn.Size = new Size(50, 45);
                btn.Location = new Point(type == "Min" ? 0 : type == "Max" ? 50 : 100, 0);
                btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.Cursor = Cursors.Hand;
                btn.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(type == "Close" && btn.BackColor == dangerRed ? Color.White : Color.FromArgb(80, 80, 80), 2.5f))
                    {
                        int cx = 25, cy = 22;
                        if (type == "Min") e.Graphics.DrawLine(pen, cx - 7, cy + 5, cx + 7, cy + 5);
                        else if (type == "Max") e.Graphics.DrawRectangle(pen, cx - 6, cy - 5, 12, 10);
                        else if (type == "Close") { e.Graphics.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6); e.Graphics.DrawLine(pen, cx + 6, cy - 6, cx - 6, cy + 6); }
                    }
                };
                btn.MouseEnter += (s, e) => { btn.BackColor = type == "Close" ? dangerRed : Color.FromArgb(220, 220, 220); };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.White; };
            };

            Button btnMin = new Button(); SetupWindowBtn(btnMin, "Min"); btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            Button btnMax = new Button(); SetupWindowBtn(btnMax, "Max");
            btnMax.Click += (s, e) => {
                if (this.WindowState == FormWindowState.Normal)
                {
                    this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                    this.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                }
            };
            Button btnClose = new Button(); 
            SetupWindowBtn(btnClose, "Close"); 
            btnClose.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to leave?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();

            pnlWindowControls.Controls.Add(btnClose); pnlWindowControls.Controls.Add(btnMax); pnlWindowControls.Controls.Add(btnMin);


            // ==========================================
            // 2. MAIN RESPONSIVE GRID
            // ==========================================
            tlpMainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlpMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            this.Controls.Add(tlpMainLayout);
            tlpMainLayout.BringToFront();


            // ==========================================
            // 3. LEFT PANEL: CATALOG & BACK BUTTON
            // ==========================================
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };
            tlpMainLayout.Controls.Add(pnlLeft, 0, 0);

            Label lblCatalog = new Label { Text = "Product Catalog", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = deepText, Dock = DockStyle.Top, Height = 40 };
            pnlLeft.Controls.Add(lblCatalog);

            Panel pnlLeftBottom = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            pnlLeft.Controls.Add(pnlLeftBottom);

            Panel btnBack = new Panel { Size = new Size(110, 45), Cursor = Cursors.Hand, BackColor = Color.Transparent, Location = new Point(0, 10) };
            EnableDoubleBuffering(btnBack); // Anti-Flicker
            bool isBackHovered = false;
            // State Guards to prevent rapid redrawing
            btnBack.MouseEnter += (s, e) => { if (!isBackHovered) { isBackHovered = true; btnBack.Invalidate(); } };
            btnBack.MouseLeave += (s, e) => { if (isBackHovered) { isBackHovered = false; btnBack.Invalidate(); } };
            btnBack.Click += (s, e) => this.Close();
            btnBack.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btnBack.Width - 1, btnBack.Height - 1), 20))
                {
                    e.Graphics.FillPath(new SolidBrush(isBackHovered ? Color.FromArgb(31, 97, 141) : techBlue), path);
                }
                TextRenderer.DrawText(e.Graphics, "← Back", new Font("Segoe UI Semibold", 12), new Rectangle(0, 0, btnBack.Width, btnBack.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlLeftBottom.Controls.Add(btnBack);

            flpProducts = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            pnlLeft.Controls.Add(flpProducts);
            flpProducts.BringToFront();

            flpProducts.Resize += (s, e) => {
                int cardWidthWithMargins = 280;
                int maxColumns = flpProducts.ClientSize.Width / cardWidthWithMargins;
                if (maxColumns > 0)
                {
                    int usedSpace = maxColumns * cardWidthWithMargins;
                    int leftoverSpace = flpProducts.ClientSize.Width - usedSpace;
                    flpProducts.Padding = new Padding(leftoverSpace / 2, 0, 0, 0);
                }
            };


            // ==========================================
            // 4. RIGHT PANEL: CART & CHECKOUT ZONE
            // ==========================================
            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30), BackColor = Color.White };
            tlpMainLayout.Controls.Add(pnlRight, 1, 0);

            Label lblCart = new Label { Text = "Current Order", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = deepText, Dock = DockStyle.Top, Height = 40 };
            pnlRight.Controls.Add(lblCart);

            TableLayoutPanel tlpCartHeaders = new TableLayoutPanel { Dock = DockStyle.Top, Height = 35, ColumnCount = 5 };
            tlpCartHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlpCartHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            tlpCartHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            tlpCartHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            tlpCartHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));

            Font headerFont = new Font("Segoe UI Semibold", 10);
            Color headerColor = Color.Gray;
            tlpCartHeaders.Controls.Add(new Label { Text = "Product", Font = headerFont, ForeColor = headerColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
            tlpCartHeaders.Controls.Add(new Label { Text = "Price", Font = headerFont, ForeColor = headerColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 1, 0);
            tlpCartHeaders.Controls.Add(new Label { Text = "Quantity", Font = headerFont, ForeColor = headerColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 2, 0);
            tlpCartHeaders.Controls.Add(new Label { Text = "Total", Font = headerFont, ForeColor = headerColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 3, 0);

            pnlRight.Controls.Add(tlpCartHeaders);
            tlpCartHeaders.BringToFront();

            Panel pnlDivider = new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Color.LightGray, Margin = new Padding(0, 0, 0, 10) };
            pnlRight.Controls.Add(pnlDivider);
            pnlDivider.BringToFront();

            tlpCartItems = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoScroll = true, Padding = new Padding(0, 10, 0, 0) };
            pnlRight.Controls.Add(tlpCartItems);
            tlpCartItems.BringToFront();

            // ==========================================
            // KIOSK CHECKOUT FOOTER
            // ==========================================
            Panel pnlCheckout = new Panel { Dock = DockStyle.Bottom, Height = 130, BackColor = Color.Transparent };
            pnlRight.Controls.Add(pnlCheckout);

            lblGrandTotal = new Label { Text = "Total: $0.00", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = techBlue, AutoSize = true, BackColor = Color.White };
            pnlCheckout.Controls.Add(lblGrandTotal);

            btnClear = new Panel { Cursor = Cursors.Hand, BackColor = Color.White };
            EnableDoubleBuffering(btnClear); // Anti-Flicker
            bool isClearHovered = false;
            // State Guards
            btnClear.MouseEnter += (s, e) => { if (!isClearHovered) { isClearHovered = true; btnClear.Invalidate(); } };
            btnClear.MouseLeave += (s, e) => { if (isClearHovered) { isClearHovered = false; btnClear.Invalidate(); } };
            btnClear.Click += (s, e) => { _controller.CurrentInvoice.Details.Clear(); RefreshCartUI(); };
            btnClear.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btnClear.Width - 1, btnClear.Height - 1), 15))
                {
                    e.Graphics.FillPath(new SolidBrush(isClearHovered ? dangerRed : Color.White), path);
                    e.Graphics.DrawPath(new Pen(dangerRed, 2f), path);
                }
                TextRenderer.DrawText(e.Graphics, "CLEAR", new Font("Segoe UI Semibold", 12), new Rectangle(0, 0, btnClear.Width, btnClear.Height), isClearHovered ? Color.White : dangerRed, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlCheckout.Controls.Add(btnClear);

            btnCheckout = new Panel { Cursor = Cursors.Hand, BackColor = Color.White };
            EnableDoubleBuffering(btnCheckout); // Anti-Flicker
            bool isCheckHovered = false;
            // State Guards
            btnCheckout.MouseEnter += (s, e) => { if (!isCheckHovered) { isCheckHovered = true; btnCheckout.Invalidate(); } };
            btnCheckout.MouseLeave += (s, e) => { if (isCheckHovered) { isCheckHovered = false; btnCheckout.Invalidate(); } };
            btnCheckout.Click += BtnCheckout_Click;
            btnCheckout.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btnCheckout.Width - 1, btnCheckout.Height - 1), 15))
                {
                    e.Graphics.FillPath(new SolidBrush(isCheckHovered ? Color.FromArgb(31, 97, 141) : techBlue), path);
                }
                TextRenderer.DrawText(e.Graphics, "CHECKOUT", new Font("Segoe UI Semibold", 14), new Rectangle(0, 0, btnCheckout.Width, btnCheckout.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlCheckout.Controls.Add(btnCheckout);

            pnlCheckout.Resize += (s, e) => {
                int padding = 20;
                int btnHeight = 55;
                int clearWidth = 130;

                btnClear.Size = new Size(clearWidth, btnHeight);
                btnClear.Location = new Point(padding, pnlCheckout.Height - btnHeight - 20);

                btnCheckout.Size = new Size(pnlCheckout.Width - clearWidth - (padding * 3), btnHeight);
                btnCheckout.Location = new Point(btnClear.Right + padding, btnClear.Top);

                lblGrandTotal.Location = new Point(btnClear.Left, btnClear.Top - lblGrandTotal.Height - 10);

                btnClear.Invalidate();
                btnCheckout.Invalidate();
            };
        }

        // ==========================================
        // ANIMATED, AUTO-SIZING TOAST NOTIFICATION
        // ==========================================
        private void ShowModernAlert(string message)
        {
            Font font = new Font("Segoe UI Semibold", 12);
            Size textSize = TextRenderer.MeasureText(message, font);
            int toastWidth = textSize.Width + 80;
            int toastHeight = 60;

            Panel toast = new Panel { Size = new Size(toastWidth, toastHeight), BackColor = Color.Transparent };
            EnableDoubleBuffering(toast);

            int targetY = 30;
            int currentY = -toastHeight;
            toast.Location = new Point((this.Width - toast.Width) / 2, currentY);

            toast.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, toast.Width - 1, toast.Height - 1), 15))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(220, 220, 220), 1), path);
                }

                using (GraphicsPath accent = GetRoundedPath(new Rectangle(0, 0, 10, toast.Height - 1), 15))
                {
                    e.Graphics.FillPath(new SolidBrush(dangerRed), accent);
                }
                e.Graphics.FillRectangle(new SolidBrush(dangerRed), 5, 0, 5, toast.Height - 1);

                int iconSize = 24;
                int iconX = 20;
                int iconY = (toast.Height - iconSize) / 2;
                e.Graphics.FillEllipse(new SolidBrush(dangerRed), iconX, iconY, iconSize, iconSize);

                using (Font iconFont = new Font("Segoe UI", 12, FontStyle.Bold))
                {
                    StringFormat iconSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("!", iconFont, Brushes.White, new Rectangle(iconX, iconY, iconSize, iconSize), iconSf);
                }

                StringFormat textSf = new StringFormat { LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(message, font, new SolidBrush(deepText), new Rectangle(55, 0, toast.Width - 65, toast.Height), textSf);
            };

            this.Controls.Add(toast);
            toast.BringToFront();

            // Assign to class-level tracker so Resize event can keep it perfectly centered
            _activeToast = toast;

            System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            int state = 0;
            int waitTime = 0;

            animTimer.Tick += (s, e) => {
                if (state == 0)
                {
                    currentY += 8;
                    toast.Top = currentY;
                    if (currentY >= targetY) { toast.Top = targetY; state = 1; }
                }
                else if (state == 1)
                {
                    waitTime += 16;
                    if (waitTime > 3000) state = 2;
                }
                else if (state == 2)
                {
                    currentY -= 8;
                    toast.Top = currentY;
                    if (currentY <= -toastHeight)
                    {
                        animTimer.Stop();
                        animTimer.Dispose();
                        this.Controls.Remove(toast);
                        toast.Dispose();
                        if (_activeToast == toast) _activeToast = null;
                    }
                }
            };
            animTimer.Start();
        }

        private void LoadCatalog()
        {
            flpProducts.Controls.Clear();
            _catalogProducts = _controller.Inventory.GetAvailableProducts();
            foreach (var p in _catalogProducts)
            {
                Panel card = new Panel { Size = new Size(240, 160), BackColor = Color.Transparent, Margin = new Padding(20) };
                EnableDoubleBuffering(card);
                card.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 15))
                    {
                        e.Graphics.FillPath(Brushes.White, path);
                        e.Graphics.DrawPath(new Pen(Color.LightGray, 1), path);
                    }

                    Rectangle textRect = new Rectangle(15, 15, 210, 45);
                    TextRenderer.DrawText(e.Graphics, p.Name, new Font("Segoe UI Semibold", 11), textRect, deepText, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(e.Graphics, $"${p.Price:F2}", new Font("Segoe UI", 12), new Point(15, 75), techBlue);
                    TextRenderer.DrawText(e.Graphics, $"Stock: {p.StockQuantity}", new Font("Segoe UI", 9), new Point(15, 105), Color.Gray);
                };

                Panel btnAdd = new Panel { Size = new Size(85, 36), Location = new Point(140, 105), Cursor = Cursors.Hand, BackColor = Color.Transparent };
                EnableDoubleBuffering(btnAdd);
                bool isAddHovered = false;
                btnAdd.MouseEnter += (s, e) => { if (!isAddHovered) { isAddHovered = true; btnAdd.Invalidate(); } };
                btnAdd.MouseLeave += (s, e) => { if (isAddHovered) { isAddHovered = false; btnAdd.Invalidate(); } };
                btnAdd.Click += (s, e) => AddItemToCart(p);
                btnAdd.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btnAdd.Width - 1, btnAdd.Height - 1), 16))
                    {
                        e.Graphics.FillPath(new SolidBrush(isAddHovered ? techBlue : Color.FromArgb(236, 240, 241)), path);
                    }
                    TextRenderer.DrawText(e.Graphics, "Add", new Font("Segoe UI Semibold", 10), new Rectangle(0, 0, btnAdd.Width, btnAdd.Height), isAddHovered ? Color.White : deepText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
                card.Controls.Add(btnAdd);
                flpProducts.Controls.Add(card);
            }
        }

        private void AddItemToCart(Product p)
        {
            var existingItem = _controller.CurrentInvoice.Details.FirstOrDefault(d => d.ProductID == p.ProductID);
            if (existingItem != null)
            {
                if (existingItem.Quantity < p.StockQuantity) existingItem.Quantity++;
                else ShowModernAlert($"Only {p.StockQuantity} in stock.");
            }
            else
            {
                if (p.StockQuantity > 0) _controller.CurrentInvoice.Details.Add(new InvoiceDetail { ProductID = p.ProductID, Quantity = 1, UnitPrice = p.Price });
            }
            RefreshCartUI();
        }

        private void RefreshCartUI()
        {
            tlpCartItems.Controls.Clear();
            tlpCartItems.RowCount = 0;
            decimal grandTotal = 0;

            foreach (var item in _controller.CurrentInvoice.Details.ToList())
            {
                Product p = _catalogProducts.FirstOrDefault(prod => prod.ProductID == item.ProductID);
                if (p == null) continue;

                TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Top, Height = 85, ColumnCount = 5, Margin = new Padding(0, 0, 0, 5) };
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));

                Panel pnlNameContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                EnableDoubleBuffering(pnlNameContainer);
                pnlNameContainer.Resize += (s, e) => pnlNameContainer.Invalidate();
                pnlNameContainer.Paint += (s, e) => {
                    e.Graphics.Clear(Color.White);
                    e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    TextRenderer.DrawText(e.Graphics, p.Name, new Font("Segoe UI Semibold", 11), new Rectangle(5, 0, pnlNameContainer.Width - 10, pnlNameContainer.Height), deepText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
                };

                Label lblPrice = new Label { Text = $"${item.UnitPrice:F2}", Font = new Font("Segoe UI", 11), ForeColor = Color.Gray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                Label lblSubTotal = new Label { Text = $"${(item.Quantity * item.UnitPrice):F2}", Font = new Font("Segoe UI Semibold", 11), ForeColor = techBlue, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

                Panel pnlQtyAnchor = new Panel { Dock = DockStyle.Fill };
                Panel pnlQtyBox = new Panel { Size = new Size(110, 38), BackColor = Color.Transparent };
                EnableDoubleBuffering(pnlQtyBox);
                pnlQtyAnchor.Layout += (s, e) => { pnlQtyBox.Location = new Point((pnlQtyAnchor.Width - pnlQtyBox.Width) / 2, (pnlQtyAnchor.Height - pnlQtyBox.Height) / 2); };
                pnlQtyAnchor.Controls.Add(pnlQtyBox);

                pnlQtyBox.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pnlQtyBox.Width - 1, pnlQtyBox.Height - 1), 18))
                    {
                        e.Graphics.FillPath(Brushes.White, path);
                        e.Graphics.DrawPath(new Pen(Color.LightGray, 1), path);
                    }
                };

                Label lblDec = new Label { Text = "−", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.Gray, Size = new Size(30, 38), Location = new Point(0, 0), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, BackColor = Color.Transparent };

                TextBox txtQty = new TextBox { Text = item.Quantity.ToString(), Width = 50, BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center, Font = new Font("Segoe UI Semibold", 12), ForeColor = deepText, BackColor = Color.White };
                txtQty.Location = new Point(30, (38 - txtQty.Height) / 2);

                Label lblInc = new Label { Text = "+", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.Gray, Size = new Size(30, 38), Location = new Point(80, 0), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, BackColor = Color.Transparent };

                lblDec.MouseEnter += (s, e) => lblDec.ForeColor = techBlue; lblDec.MouseLeave += (s, e) => lblDec.ForeColor = Color.Gray;
                lblInc.MouseEnter += (s, e) => lblInc.ForeColor = techBlue; lblInc.MouseLeave += (s, e) => lblInc.ForeColor = Color.Gray;

                lblDec.Click += (s, e) => { if (item.Quantity > 1) item.Quantity--; else _controller.CurrentInvoice.Details.Remove(item); RefreshCartUI(); };
                lblInc.Click += (s, e) => { if (item.Quantity < p.StockQuantity) item.Quantity++; else ShowModernAlert("Maximum stock reached."); RefreshCartUI(); };

                txtQty.KeyPress += (s, e) => { e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar); };
                txtQty.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; tlpCartItems.Focus(); } };
                txtQty.Leave += (s, e) => {
                    if (int.TryParse(txtQty.Text, out int newQty))
                    {
                        if (newQty <= 0) _controller.CurrentInvoice.Details.Remove(item);
                        else if (newQty > p.StockQuantity) { ShowModernAlert($"Only {p.StockQuantity} in stock."); item.Quantity = p.StockQuantity; }
                        else item.Quantity = newQty;
                    }
                    RefreshCartUI();
                };

                pnlQtyBox.Controls.Add(lblDec); pnlQtyBox.Controls.Add(txtQty); pnlQtyBox.Controls.Add(lblInc);

                Panel pnlActionAnchor = new Panel { Dock = DockStyle.Fill };
                Panel btnTrash = new Panel { Size = new Size(36, 36), Cursor = Cursors.Hand };
                EnableDoubleBuffering(btnTrash);
                pnlActionAnchor.Layout += (s, e) => { btnTrash.Location = new Point((pnlActionAnchor.Width - btnTrash.Width) / 2, (pnlActionAnchor.Height - btnTrash.Height) / 2); };
                pnlActionAnchor.Controls.Add(btnTrash);

                bool isHovered = false;
                btnTrash.MouseEnter += (s, e) => { if (!isHovered) { isHovered = true; btnTrash.Invalidate(); } };
                btnTrash.MouseLeave += (s, e) => { if (isHovered) { isHovered = false; btnTrash.Invalidate(); } };
                btnTrash.Click += (s, e) => { _controller.CurrentInvoice.Details.Remove(item); RefreshCartUI(); };
                btnTrash.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(isHovered ? dangerRed : Color.Silver, 2f))
                    {
                        int cx = 18, cy = 18;
                        e.Graphics.DrawLine(pen, cx - 8, cy - 6, cx + 8, cy - 6); e.Graphics.DrawLine(pen, cx - 3, cy - 6, cx - 3, cy - 9);
                        e.Graphics.DrawLine(pen, cx + 3, cy - 6, cx + 3, cy - 9); e.Graphics.DrawLine(pen, cx - 3, cy - 9, cx + 3, cy - 9);
                        e.Graphics.DrawLine(pen, cx - 6, cy - 4, cx - 4, cy + 8); e.Graphics.DrawLine(pen, cx + 6, cy - 4, cx + 4, cy + 8);
                        e.Graphics.DrawLine(pen, cx - 4, cy + 8, cx + 4, cy + 8); e.Graphics.DrawLine(pen, cx - 1, cy - 2, cx - 1, cy + 5);
                        e.Graphics.DrawLine(pen, cx + 2, cy - 2, cx + 2, cy + 5);
                    }
                };

                row.Controls.Add(pnlNameContainer, 0, 0);
                row.Controls.Add(lblPrice, 1, 0);
                row.Controls.Add(pnlQtyAnchor, 2, 0);
                row.Controls.Add(lblSubTotal, 3, 0);
                row.Controls.Add(pnlActionAnchor, 4, 0);

                tlpCartItems.Controls.Add(row);
                grandTotal += (item.Quantity * item.UnitPrice);
            }

            lblGrandTotal.Text = $"Total: ${grandTotal:F2}";

            if (btnClear != null)
            {
                lblGrandTotal.Location = new Point(btnClear.Left, btnClear.Top - lblGrandTotal.Height - 10);
            }
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (_controller.CurrentInvoice.Details.Count == 0)
            {
                ShowModernAlert("Your cart is empty. Please add items before checking out.");
                return;
            }

            if (_controller.ProcessPayment())
            {
                MessageBox.Show("Payment processed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _controller.CurrentInvoice.Details.Clear(); RefreshCartUI();
                LoadCatalog();
                RefreshCartUI();
            }
            else
            {
                MessageBox.Show("Transaction failed. Check database connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
