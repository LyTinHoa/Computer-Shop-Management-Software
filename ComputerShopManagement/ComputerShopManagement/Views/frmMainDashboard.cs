using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ComputerShopManagement.Views
{
    public partial class frmMainDashboard : Form
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

        private Staff _currentUser;

        // Brand Colors
        private Color techBlue = Color.FromArgb(41, 128, 185);
        private Color deepText = Color.FromArgb(44, 62, 80);
        private Color lightGray = Color.FromArgb(240, 242, 245);
        private Color dangerRed = Color.FromArgb(231, 76, 60);

        private Panel pnlSidebar;
        private Panel pnlTopBar;
        private Panel pnlMainContent;

        private Button btnSales, btnInventory, btnCustomers, btnEmployees, btnReports, btnLogout;

        public frmMainDashboard(Staff loggedInUser)
        {
            InitializeComponent();
            _currentUser = loggedInUser;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupDashboardUI();
            ApplyRoleBasedAccess();
            PopulateCommandCenter();
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

        private void SetupDashboardUI()
        {
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = lightGray;
            this.Padding = new Padding(2);

            int sidebarWidth = 350;
            // FIX: Shrunk the top bar height to 60px for a sleeker dashboard feel
            int topBarHeight = 60;

            // ==========================================
            // 1. TOP BAR & WINDOW CONTROLS
            // ==========================================
            pnlTopBar = new Panel { Dock = DockStyle.Top, Height = topBarHeight, BackColor = Color.White };
            pnlTopBar.Resize += (s, e) => pnlTopBar.Invalidate();
            pnlTopBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            this.Controls.Add(pnlTopBar);

            pnlTopBar.Paint += (s, e) => {
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230), 1), 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            };

            // Flushed Window Controls
            Panel pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 150 };
            pnlTopBar.Controls.Add(pnlWindowControls);
            pnlWindowControls.BringToFront();

            Action<Button, string> SetupWindowBtn = (btn, type) => {
                btn.Size = new Size(50, topBarHeight); // Dynamically fills the new 60px height
                btn.Location = new Point(type == "Min" ? 0 : type == "Max" ? 50 : 100, 0);
                btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.Cursor = Cursors.Hand;
                btn.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(type == "Close" && btn.BackColor == dangerRed ? Color.White : Color.FromArgb(80, 80, 80), 2.5f))
                    {
                        int cx = 25;
                        int cy = topBarHeight / 2; // Perfect mathematical centering
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
            Button btnClose = new Button(); SetupWindowBtn(btnClose, "Close"); btnClose.Click += (s, e) => Application.Exit(); btnClose.MouseEnter += (s, e) => btnClose.Invalidate();

            pnlWindowControls.Controls.Add(btnClose); pnlWindowControls.Controls.Add(btnMax); pnlWindowControls.Controls.Add(btnMin);


            // ==========================================
            // 2. SLEEK, GRADIENT SIDEBAR
            // ==========================================
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = sidebarWidth };
            pnlSidebar.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush brush = new LinearGradientBrush(pnlSidebar.ClientRectangle, Color.FromArgb(15, 32, 39), Color.FromArgb(41, 128, 185), 45f))
                {
                    e.Graphics.FillRectangle(brush, pnlSidebar.ClientRectangle);
                }

                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                e.Graphics.DrawString("BitTekk", new Font("Segoe UI", 42, FontStyle.Bold), Brushes.White, new Point(30, 20));

                // FIX: Renamed and shifted down (Y=105) to provide breathing room from the main logo
                e.Graphics.DrawString("Computer Shop Management", new Font("Segoe UI Semibold", 10, FontStyle.Regular), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new Point(35, 105));
            };
            this.Controls.Add(pnlSidebar);
            pnlSidebar.BringToFront();

            btnSales = CreateNavButton("🛒  Point of Sale", 170);
            btnInventory = CreateNavButton("📦  Inventory Manager", 240);
            btnCustomers = CreateNavButton("👥  Customers", 310);
            btnEmployees = CreateNavButton("👔  Employee Admin", 380);
            btnReports = CreateNavButton("📊  Reports & Analytics", 450);

            // FIX: Shortened text to "Logout"
            btnLogout = CreateNavButton("🚪  Logout", 0);
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.Height = 80;
            btnLogout.BackColor = Color.FromArgb(20, 0, 0, 0);
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = Color.FromArgb(231, 76, 60);
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.FromArgb(20, 0, 0, 0);
            btnLogout.Click += (s, e) => { Application.Restart(); };

            btnSales.Click += (s, e) => {
                SalesController salesCtrl = new SalesController();
                salesCtrl.CurrentInvoice.StaffID = _currentUser.StaffID;
                OpenModule(new frmSales(salesCtrl));
            };
            btnInventory.Click += (s, e) => OpenModule(new frmInventory(_currentUser));


            // ==========================================
            // 3. MAIN CONTENT (THE COMMAND CENTER)
            // ==========================================
            pnlMainContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            this.Controls.Add(pnlMainContent);
            pnlMainContent.BringToFront();
        }

        private void PopulateCommandCenter()
        {
            Label lblGreeting = new Label
            {
                Text = $"Welcome back, {_currentUser.FullName}",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = deepText,
                AutoSize = true,
                Location = new Point(50, 40)
            };
            pnlMainContent.Controls.Add(lblGreeting);

            Label lblRole = new Label
            {
                Text = $"System Role: {_currentUser.Role}   |   Last Login: {DateTime.Now.ToString("MMMM dd, yyyy")}",
                Font = new Font("Segoe UI Semibold", 14),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(55, 95)
            };
            pnlMainContent.Controls.Add(lblRole);

            Panel cardRevenue = CreateKPICard("Today's Revenue", "$4,250.00", "+12% from yesterday", Color.FromArgb(46, 204, 113), Color.FromArgb(39, 174, 96), 50, 180);
            pnlMainContent.Controls.Add(cardRevenue);

            Panel cardOrders = CreateKPICard("Active Orders", "14", "3 awaiting fulfillment", Color.FromArgb(52, 152, 219), techBlue, 420, 180);
            pnlMainContent.Controls.Add(cardOrders);

            Panel cardStock = CreateKPICard("Low Stock Alerts", "2", "Requires immediate review", dangerRed, Color.FromArgb(192, 57, 43), 790, 180);
            pnlMainContent.Controls.Add(cardStock);

            Label lblQuick = new Label
            {
                Text = "Suggested Actions",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = deepText,
                AutoSize = true,
                Location = new Point(50, 420)
            };
            pnlMainContent.Controls.Add(lblQuick);

            Label lblPlaceholder = new Label
            {
                Text = "Quick action modules (e.g., Generate End-of-Day Report, Add New Employee) can be populated here.",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(50, 460)
            };
            pnlMainContent.Controls.Add(lblPlaceholder);
        }

        private Panel CreateKPICard(string title, string value, string subText, Color gradientStart, Color gradientEnd, int x, int y)
        {
            Panel card = new Panel { Size = new Size(340, 180), Location = new Point(x, y), BackColor = Color.Transparent };

            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 20))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(card.ClientRectangle, gradientStart, gradientEnd, 45f))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                e.Graphics.DrawString(title, new Font("Segoe UI Semibold", 14), new SolidBrush(Color.FromArgb(220, 255, 255, 255)), new Point(25, 25));
                e.Graphics.DrawString(value, new Font("Segoe UI", 36, FontStyle.Bold), Brushes.White, new Point(20, 60));
                e.Graphics.DrawString(subText, new Font("Segoe UI", 11), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new Point(25, 130));
            };
            return card;
        }

        private Button CreateNavButton(string text, int yPos)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 14),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(pnlSidebar.Width, 65),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(40, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(50, 255, 255, 255);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;
            pnlSidebar.Controls.Add(btn);
            return btn;
        }

        private void OpenModule(Form module)
        {
            module.StartPosition = FormStartPosition.Manual;

            if (this.WindowState == FormWindowState.Normal)
            {
                module.Location = this.Location;
                module.Size = this.Size;
            }
            module.WindowState = this.WindowState;

            this.Hide();
            module.ShowDialog();

            if (module.WindowState == FormWindowState.Normal)
            {
                this.Location = module.Location;
                this.Size = module.Size;
            }
            this.WindowState = module.WindowState;
            this.Show();
        }

        private void ApplyRoleBasedAccess()
        {
            string role = _currentUser.Role;
            bool showSales = role != "Inventory";
            bool showInventory = role != "Sales";
            bool showCustomers = role != "Inventory";
            bool showEmployees = role == "Manager";
            bool showReports = role == "Manager";

            btnSales.Visible = showSales;
            btnInventory.Visible = showInventory;
            btnCustomers.Visible = showCustomers;
            btnEmployees.Visible = showEmployees;
            btnReports.Visible = showReports;

            int currentY = 170;
            int spacing = 70;

            if (showSales) { btnSales.Location = new Point(0, currentY); currentY += spacing; }
            if (showInventory) { btnInventory.Location = new Point(0, currentY); currentY += spacing; }
            if (showCustomers) { btnCustomers.Location = new Point(0, currentY); currentY += spacing; }
            if (showEmployees) { btnEmployees.Location = new Point(0, currentY); currentY += spacing; }
            if (showReports) { btnReports.Location = new Point(0, currentY); currentY += spacing; }
        }
    }
}
