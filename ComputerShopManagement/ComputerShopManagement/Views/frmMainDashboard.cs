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

        // UI Elements for Resizing
        private Label lblGreeting, lblRole, lblClock, lblShift;
        private Panel cardRevenue, cardOrders, cardStock;

        public frmMainDashboard(Staff loggedInUser)
        {
            InitializeComponent();
            _currentUser = loggedInUser;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupDashboardUI();
            ApplyRoleBasedAccess();
            PopulateCommandCenter();

            // Trigger an initial layout refresh to ensure elements scale correctly on startup
            this.OnResize(EventArgs.Empty);
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
            this.MinimumSize = new Size(1200, 700); // Safety net for scaling math
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = lightGray;

            int sidebarWidth = 350;
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

            Panel pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 150 };
            pnlTopBar.Controls.Add(pnlWindowControls);
            pnlWindowControls.BringToFront();

            Action<Button, string> SetupWindowBtn = (btn, type) => {
                btn.Size = new Size(50, topBarHeight);
                btn.Location = new Point(type == "Min" ? 0 : type == "Max" ? 50 : 100, 0);
                btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.Cursor = Cursors.Hand;
                btn.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(type == "Close" && btn.BackColor == dangerRed ? Color.White : Color.FromArgb(80, 80, 80), 2.5f))
                    {
                        int cx = 25;
                        int cy = topBarHeight / 2;
                        if (type == "Min") e.Graphics.DrawLine(pen, cx - 7, cy + 5, cx + 7, cy + 5);
                        else if (type == "Max") e.Graphics.DrawRectangle(pen, cx - 6, cy - 5, 12, 10);
                        else if (type == "Close") { e.Graphics.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6); e.Graphics.DrawLine(pen, cx + 6, cy - 6, cx - 6, cy + 6); }
                    }
                };
                btn.MouseEnter += (s, e) => { btn.BackColor = type == "Close" ? dangerRed : Color.FromArgb(220, 220, 220); };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.White; };
            };

            Button btnMin = new Button(); SetupWindowBtn(btnMin, "Min");
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

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

            Button btnClose = new Button(); SetupWindowBtn(btnClose, "Close");
            btnClose.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to leave?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();

            pnlWindowControls.Controls.Add(btnClose);
            pnlWindowControls.Controls.Add(btnMax);
            pnlWindowControls.Controls.Add(btnMin);


            // ==========================================
            // 2. SLEEK, GRADIENT SIDEBAR (Scalable)
            // ==========================================
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = sidebarWidth };
            pnlSidebar.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush brush = new LinearGradientBrush(pnlSidebar.ClientRectangle, Color.FromArgb(15, 32, 39), Color.FromArgb(41, 128, 185), 45f))
                {
                    e.Graphics.FillRectangle(brush, pnlSidebar.ClientRectangle);
                }

                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // Dynamically scale logo fonts based on current width
                float titleSize = Math.Max(20f, pnlSidebar.Width * 0.12f);
                float subSize = Math.Max(8f, pnlSidebar.Width * 0.028f);

                e.Graphics.DrawString("BitTekk", new Font("Segoe UI", titleSize, FontStyle.Bold), Brushes.White, new Point((int)(pnlSidebar.Width * 0.08f), 20));
                e.Graphics.DrawString("Computer Shop Management", new Font("Segoe UI Semibold", subSize, FontStyle.Regular), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new Point((int)(pnlSidebar.Width * 0.1f), (int)(20 + titleSize * 1.5f + 10)));
            };
            this.Controls.Add(pnlSidebar);
            pnlSidebar.BringToFront();

            btnSales = CreateNavButton("🛒  Point of Sale", 170);
            btnInventory = CreateNavButton("📦  Inventory Manager", 240);
            btnCustomers = CreateNavButton("👥  Customers", 310);
            btnEmployees = CreateNavButton("👔  Employee Admin", 380);
            btnReports = CreateNavButton("📊  Reports & Analytics", 450);

            // FIX: Removed Dock=Bottom. Manually positioned to keep it a perfect rectangle
            btnLogout = CreateNavButton("🚪  Logout", 0);
            btnLogout.Height = 80;
            btnLogout.BackColor = Color.FromArgb(20, 0, 0, 0);
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = dangerRed;
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.FromArgb(20, 0, 0, 0);
            btnLogout.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to log out?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Restart();
                }
            };

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

            // ==========================================
            // 4. MASTER RESPONSIVE RESIZE EVENT
            // ==========================================
            this.Resize += (s, e) => {
                // Calculate Scale Multipliers
                float scaleX = this.ClientSize.Width / 1600f;
                float scaleY = this.ClientSize.Height / 900f;
                float minScale = Math.Min(scaleX, scaleY); // Prevents distortion

                // A. Scale Sidebar
                pnlSidebar.Width = (int)(350 * scaleX);

                // B. Scale Navigation Buttons
                int currentY = (int)(170 * scaleY);
                int spacing = (int)(70 * scaleY);
                float btnFontSize = Math.Max(10f, 14 * minScale);

                foreach (Control ctrl in pnlSidebar.Controls)
                {
                    if (ctrl is Button btn)
                    {
                        btn.Font = new Font("Segoe UI Semibold", btnFontSize);

                        // Force Logout to stay a rectangle at the bottom
                        if (btn == btnLogout)
                        {
                            btn.Size = new Size(pnlSidebar.Width, (int)(80 * scaleY));
                            btn.Location = new Point(0, pnlSidebar.Height - btn.Height);
                        }
                        else if (btn.Visible)
                        {
                            btn.Size = new Size(pnlSidebar.Width, (int)(65 * scaleY));
                            btn.Location = new Point(0, currentY);
                            currentY += spacing;
                        }
                    }
                }
                pnlSidebar.Invalidate(); // Repaint gradients and logos

                // C. Scale Main Dashboard Elements (If loaded)
                if (lblGreeting != null)
                {
                    // Scale Fonts
                    lblGreeting.Font = new Font("Segoe UI", Math.Max(16f, 32 * minScale), FontStyle.Bold);
                    lblRole.Font = new Font("Segoe UI Semibold", Math.Max(10f, 14 * minScale));
                    lblClock.Font = new Font("Segoe UI", Math.Max(14f, 24 * minScale), FontStyle.Bold);
                    lblShift.Font = new Font("Segoe UI Semibold", Math.Max(10f, 14 * minScale));

                    // Scale Cards (Grow by ~30% natively via scaleX/scaleY)
                    int newCardWidth = (int)(340 * scaleX);
                    int newCardHeight = (int)(180 * scaleY);

                    int totalCardsWidth = newCardWidth * 3;
                    int gap = (pnlMainContent.Width - totalCardsWidth) / 4;
                    if (gap < 20) gap = 20;

                    // Position Header Texts
                    int startY = (int)(40 * scaleY);
                    lblGreeting.Location = new Point(gap, startY);
                    lblRole.Location = new Point(gap + 5, startY + lblGreeting.Height + 10);

                    // Position KPI Cards
                    int cardY = lblRole.Bottom + (int)(40 * scaleY);
                    cardRevenue.Size = new Size(newCardWidth, newCardHeight);
                    cardOrders.Size = new Size(newCardWidth, newCardHeight);
                    cardStock.Size = new Size(newCardWidth, newCardHeight);

                    cardRevenue.Location = new Point(gap, cardY);
                    cardOrders.Location = new Point(gap * 2 + newCardWidth, cardY);
                    cardStock.Location = new Point(gap * 3 + newCardWidth * 2, cardY);

                    // Position Clock explicitly BELOW the cards to prevent overlap
                    int timeY = cardRevenue.Bottom + (int)(60 * scaleY);
                    lblClock.Location = new Point(gap, timeY);
                    lblShift.Location = new Point(gap + 5, lblClock.Bottom + 10);
                }
            };
        }

        private void PopulateCommandCenter()
        {
            lblGreeting = new Label { Text = $"Welcome back, {_currentUser.FullName}", ForeColor = deepText, AutoSize = true };
            pnlMainContent.Controls.Add(lblGreeting);

            lblRole = new Label { Text = $"System Role: {_currentUser.Role}   |   Last Login: {DateTime.Now.ToString("dd-MM-yyyy")}", ForeColor = Color.Gray, AutoSize = true };
            pnlMainContent.Controls.Add(lblRole);

            cardRevenue = CreateKPICard("Today's Revenue", "$4,250.00", "+12% from yesterday", Color.FromArgb(46, 204, 113), Color.FromArgb(39, 174, 96));
            pnlMainContent.Controls.Add(cardRevenue);

            cardOrders = CreateKPICard("Transactions Today", "24", "Avg. Value: $175.00", Color.FromArgb(52, 152, 219), techBlue); pnlMainContent.Controls.Add(cardOrders);

            cardStock = CreateKPICard("Low Stock Alerts", "2", "Requires immediate review", dangerRed, Color.FromArgb(192, 57, 43));
            pnlMainContent.Controls.Add(cardStock);

            lblClock = new Label { ForeColor = techBlue, AutoSize = true };
            pnlMainContent.Controls.Add(lblClock);

            lblShift = new Label { ForeColor = deepText, AutoSize = true };
            pnlMainContent.Controls.Add(lblShift);

            // Update time and shift every second
            System.Windows.Forms.Timer clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
            {
                DateTime now = DateTime.Now;
                var englishCulture = new System.Globalization.CultureInfo("en-US");
                lblClock.Text = now.ToString("dddd, MMMM dd, yyyy  |  hh:mm:ss tt", englishCulture);

                int hour = now.Hour;
                string shiftName = "Off Hours";

                if (hour >= 8 && hour < 13) shiftName = "Shift 1 (08:00 - 12:00)";
                else if (hour >= 13 && hour < 18) shiftName = "Shift 2 (13:00 - 18:00)";
                else if (hour >= 18 && hour < 23) shiftName = "Shift 3 (18:00 - 23:00)";

                lblShift.Text = $"Current Shift: {shiftName}";
            };
            clockTimer.Start();
        }

        private Panel CreateKPICard(string title, string value, string subText, Color gradientStart, Color gradientEnd)
        {
            Panel card = new Panel { BackColor = Color.Transparent };

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

                // Dynamic fonts based on card height for flawless scaling
                float titleSize = Math.Max(10f, card.Height * 0.08f);
                float valueSize = Math.Max(18f, card.Height * 0.20f);
                float subSize = Math.Max(8f, card.Height * 0.06f);

                e.Graphics.DrawString(title, new Font("Segoe UI Semibold", titleSize), new SolidBrush(Color.FromArgb(220, 255, 255, 255)), new Point(25, (int)(card.Height * 0.13f)));
                e.Graphics.DrawString(value, new Font("Segoe UI", valueSize, FontStyle.Bold), Brushes.White, new Point(20, (int)(card.Height * 0.33f)));
                e.Graphics.DrawString(subText, new Font("Segoe UI", subSize), new SolidBrush(Color.FromArgb(200, 255, 255, 255)), new Point(25, (int)(card.Height * 0.72f)));
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
        }
    }
}