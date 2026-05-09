using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Views
{
    public partial class frmMainDashboard : Form
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

        private Staff _currentUser;
        private Panel pnlSidebar;
        private Panel pnlTopBar;
        private Panel pnlMainContent;

        private Button btnSales, btnInventory, btnCustomers, btnEmployees, btnReports, btnLogout;
        private Button btnMaximize;

        public frmMainDashboard(Staff loggedInUser)
        {
            InitializeComponent();
            _currentUser = loggedInUser;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupDashboardUI();
            ApplyRoleBasedAccess();
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

        private void SetupDashboardUI()
        {
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(41, 128, 185); // Acts as the 2px resize border

            int pad = 2; // 2px invisible grip padding

            // --- ABSOLUTE GEOMETRY (Prevents any overlap bugs) ---
            pnlSidebar = new Panel { Location = new Point(pad, pad), Size = new Size(350, this.ClientSize.Height - (pad * 2)), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };
            pnlSidebar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                using (LinearGradientBrush brush = new LinearGradientBrush(pnlSidebar.ClientRectangle, Color.FromArgb(15, 32, 39), Color.FromArgb(41, 128, 185), 45f))
                {
                    e.Graphics.FillRectangle(brush, pnlSidebar.ClientRectangle);
                }
            };
            this.Controls.Add(pnlSidebar);

            pnlTopBar = new Panel { Location = new Point(pad + 350, pad), Size = new Size(this.ClientSize.Width - (pad * 2) - 350, 60), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlTopBar.MouseDown += DragWindow_MouseDown;
            this.Controls.Add(pnlTopBar);

            pnlMainContent = new Panel { Location = new Point(pad + 350, pad + 60), Size = new Size(this.ClientSize.Width - (pad * 2) - 350, this.ClientSize.Height - (pad * 2) - 60), BackColor = Color.FromArgb(245, 246, 250), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.Add(pnlMainContent);

            // --- TOP BAR CONTENTS ---
            Label lblWelcome = new Label { Text = $"Welcome, {_currentUser.FullName}  |  Role: {_currentUser.Role}", Font = new Font("Segoe UI Semibold", 16), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(20, 15) };
            lblWelcome.MouseDown += DragWindow_MouseDown;
            pnlTopBar.Controls.Add(lblWelcome);

            // Exactly 5px margin from the right edge!
            Button btnClose = new Button { Text = "×", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat, Size = new Size(40, 40), Location = new Point(pnlTopBar.Width - 45, 10), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Padding = new Padding(2, 0, 0, 0) };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.Red; };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.Gray; btnClose.BackColor = Color.White; };
            btnClose.Click += (s, e) => Application.Exit();
            pnlTopBar.Controls.Add(btnClose);

            btnMaximize = new Button { Text = "☐", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat, Size = new Size(40, 40), Location = new Point(pnlTopBar.Width - 85, 10), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Padding = new Padding(2, 0, 0, 0) };
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.MouseEnter += (s, e) => { btnMaximize.BackColor = Color.FromArgb(235, 235, 235); };
            btnMaximize.MouseLeave += (s, e) => { btnMaximize.BackColor = Color.White; };
            btnMaximize.Click += (s, e) => ToggleMaximize();
            pnlTopBar.Controls.Add(btnMaximize);
            pnlTopBar.DoubleClick += (s, e) => ToggleMaximize();

            // --- SIDEBAR CONTENTS ---
            Label lblLogo = new Label { Text = "BitTekk", Font = new Font("Segoe UI", 45, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(40, 30), BackColor = Color.Transparent };
            pnlSidebar.Controls.Add(lblLogo);

            btnSales = CreateNavButton("🛒  Point of Sale", 0);
            btnInventory = CreateNavButton("📦  Inventory Manager", 0);
            btnCustomers = CreateNavButton("👥  Customers", 0);
            btnEmployees = CreateNavButton("👔  Employee Admin", 0);
            btnReports = CreateNavButton("📊  Reports & Analytics", 0);

            btnLogout = CreateNavButton("🚪  Logout", 0);
            btnLogout.Size = new Size(310, 60);
            btnLogout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnLogout.Location = new Point(20, pnlSidebar.Height - 80);
            btnLogout.FlatAppearance.BorderSize = 2;
            btnLogout.FlatAppearance.BorderColor = Color.White;
            btnLogout.Click += (s, e) => { Application.Restart(); };

            btnSales.Click += (s, e) => OpenModule(new frmSales(_currentUser));
            btnInventory.Click += (s, e) => OpenModule(new frmInventory(_currentUser));
        }

        private void OpenModule(Form module)
        {
            module.StartPosition = FormStartPosition.Manual;
            module.Location = this.Location;
            module.Size = this.Size;
            module.WindowState = this.WindowState;

            this.Hide();
            module.ShowDialog();

            this.Location = module.Location;
            this.Size = module.Size;
            this.WindowState = module.WindowState;
            this.Show();
        }

        private Button CreateNavButton(string text, int yPos)
        {
            Button btn = new Button { Text = text, Font = new Font("Segoe UI Semibold", 16), ForeColor = Color.White, BackColor = Color.Transparent, FlatStyle = FlatStyle.Flat, Size = new Size(350, 65), Location = new Point(0, yPos), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(30, 0, 0, 0), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(52, 152, 219);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;
            pnlSidebar.Controls.Add(btn);
            return btn;
        }

        private void ApplyRoleBasedAccess()
        {
            string role = _currentUser.Role;
            bool showSales = role != "Inventory";
            bool showInventory = role != "Sales";
            bool showCustomers = role != "Inventory";
            bool showEmployees = role == "Manager";
            bool showReports = role == "Manager";

            btnSales.Visible = showSales; btnInventory.Visible = showInventory; btnCustomers.Visible = showCustomers;
            btnEmployees.Visible = showEmployees; btnReports.Visible = showReports;

            int currentY = 160;
            int spacing = 75;

            if (showSales) { btnSales.Location = new Point(0, currentY); currentY += spacing; }
            if (showInventory) { btnInventory.Location = new Point(0, currentY); currentY += spacing; }
            if (showCustomers) { btnCustomers.Location = new Point(0, currentY); currentY += spacing; }
            if (showEmployees) { btnEmployees.Location = new Point(0, currentY); currentY += spacing; }
            if (showReports) { btnReports.Location = new Point(0, currentY); currentY += spacing; }
        }

        private void DragWindow_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } }
        private void ToggleMaximize() { if (this.WindowState == FormWindowState.Normal) { this.WindowState = FormWindowState.Maximized; btnMaximize.Text = "❐"; } else { this.WindowState = FormWindowState.Normal; btnMaximize.Text = "☐"; } }
    }
}