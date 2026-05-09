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
        // Native Windows 11 DWM API for rounded corners and shadows
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private Staff _currentUser;
        private Panel pnlSidebar;
        private Panel pnlTopBar;
        private Panel pnlMainContent;

        private Button btnSales;
        private Button btnInventory;
        private Button btnCustomers;
        private Button btnEmployees;
        private Button btnReports;
        private Button btnLogout;

        public frmMainDashboard(Staff loggedInUser)
        {
            InitializeComponent();
            _currentUser = loggedInUser;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            SetupDashboardUI();
            ApplyRoleBasedAccess();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // Drop Shadow
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int preference = DWMWCP_ROUND;
            DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        private void SetupDashboardUI()
        {
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.Text = "BitTekk Dashboard";

            // --- TOP BAR (Proportions balanced for Size 16 text) ---
            pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            this.Controls.Add(pnlTopBar);

            // Welcome Text size perfectly matched to Menu Buttons (Size 16)
            Label lblWelcome = new Label { Text = $"Welcome, {_currentUser.FullName}  |  Role: {_currentUser.Role}", Font = new Font("Segoe UI Semibold", 16), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(20, 15) };
            pnlTopBar.Controls.Add(lblWelcome);

            Button btnClose = new Button { Text = "×", Font = new Font("Segoe UI", 16), ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat, Size = new Size(40, 40), Location = new Point(1230, 10), Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; btnClose.BackColor = Color.Red; };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.Gray; btnClose.BackColor = Color.White; };
            btnClose.Click += (s, e) => Application.Exit();
            pnlTopBar.Controls.Add(btnClose);

            // --- SIDEBAR ---
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 350 };
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

            // Logo remains 50% larger
            Label lblLogo = new Label { Text = "BitTekk", Font = new Font("Segoe UI", 45, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(40, 30), BackColor = Color.Transparent };
            pnlSidebar.Controls.Add(lblLogo);

            // --- MAIN CONTENT AREA ---
            pnlMainContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            this.Controls.Add(pnlMainContent);

            // --- NAVIGATION BUTTONS ---
            btnSales = CreateNavButton("🛒  Point of Sale", 160);
            btnSales.Click += (s, e) =>
            {
                // Opens the POS form and passes the current user down the chain
                frmSales salesForm = new frmSales(_currentUser);
                this.Hide(); // Hides the dashboard
                salesForm.ShowDialog(); // Opens POS as a modal
                this.Show(); // Re-shows the dashboard when POS is closed
            };
            btnInventory = CreateNavButton("📦  Inventory Manager", 235);
            btnCustomers = CreateNavButton("👥  Customers", 310);
            btnEmployees = CreateNavButton("👔  Employee Admin", 385);
            btnReports = CreateNavButton("📊  Reports & Analytics", 460);

            btnLogout = CreateNavButton("🚪  Logout", 700);
            btnLogout.Click += (s, e) => { Application.Restart(); };
        }

        private Button CreateNavButton(string text, int yPos)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 16),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(350, 65),
                Location = new Point(0, yPos),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(30, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            // FIX: Restored the vibrant Light Tech Blue hover effect
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(52, 152, 219);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;

            pnlSidebar.Controls.Add(btn);
            return btn;
        }

        private void ApplyRoleBasedAccess()
        {
            string role = _currentUser.Role;

            btnEmployees.Visible = false;
            btnReports.Visible = false;

            if (role == "Sales")
            {
                btnInventory.Visible = false;
            }
            else if (role == "Inventory")
            {
                btnSales.Visible = false;
                btnCustomers.Visible = false;
            }
            else if (role == "Manager")
            {
                btnEmployees.Visible = true;
                btnReports.Visible = true;
            }
        }
    }
}