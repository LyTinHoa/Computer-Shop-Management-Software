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
                cp.ClassStyle |= 0x00020000;
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

            // --- TOP BAR ---
            pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            this.Controls.Add(pnlTopBar);

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

            Label lblLogo = new Label { Text = "BitTekk", Font = new Font("Segoe UI", 45, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(40, 30), BackColor = Color.Transparent };
            pnlSidebar.Controls.Add(lblLogo);

            // --- MAIN CONTENT AREA ---
            pnlMainContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            this.Controls.Add(pnlMainContent);

            // --- NAVIGATION BUTTONS ---
            // We temporarily set their Y to 0. They will be stacked automatically by the Role logic.
            btnSales = CreateNavButton("🛒  Point of Sale", 0);
            btnInventory = CreateNavButton("📦  Inventory Manager", 0);
            btnCustomers = CreateNavButton("👥  Customers", 0);
            btnEmployees = CreateNavButton("👔  Employee Admin", 0);
            btnReports = CreateNavButton("📊  Reports & Analytics", 0);

            // LOGOUT BUTTON (Customized)
            btnLogout = CreateNavButton("🚪  Logout", 675);
            // Re-size and center it slightly so the white border doesn't clip against the edge of the window
            btnLogout.Size = new Size(310, 60);
            btnLogout.Location = new Point(20, 675);
            btnLogout.FlatAppearance.BorderSize = 2; // 2px Border
            btnLogout.FlatAppearance.BorderColor = Color.White; // White Border

            btnLogout.Click += (s, e) => { Application.Restart(); };

            // Wire up the main functions
            btnSales.Click += (s, e) => { frmSales pos = new frmSales(_currentUser); this.Hide(); pos.ShowDialog(); this.Show(); };
            //btnInventory.Click += (s, e) => { frmInventory inv = new frmInventory(_currentUser); this.Hide(); inv.ShowDialog(); this.Show(); };
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
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(52, 152, 219);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;

            pnlSidebar.Controls.Add(btn);
            return btn;
        }

        private void ApplyRoleBasedAccess()
        {
            string role = _currentUser.Role;

            // 1. Explicitly define what each role is allowed to see
            bool showSales = role != "Inventory";
            bool showInventory = role != "Sales";
            bool showCustomers = role != "Inventory";
            bool showEmployees = role == "Manager";
            bool showReports = role == "Manager";

            // 2. Apply visibility to the buttons
            btnSales.Visible = showSales;
            btnInventory.Visible = showInventory;
            btnCustomers.Visible = showCustomers;
            btnEmployees.Visible = showEmployees;
            btnReports.Visible = showReports;

            // 3. DYNAMICALLY STACK THE BUTTONS 
            // By checking our boolean flags instead of btn.Visible, it works perfectly before the form even renders
            int currentY = 160; // Starting position directly under the BitTekk logo
            int spacing = 75;   // Button height (65) + 10px of gap space

            if (showSales)
            {
                btnSales.Location = new Point(0, currentY);
                currentY += spacing;
            }

            if (showInventory)
            {
                btnInventory.Location = new Point(0, currentY);
                currentY += spacing;
            }

            if (showCustomers)
            {
                btnCustomers.Location = new Point(0, currentY);
                currentY += spacing;
            }

            if (showEmployees)
            {
                btnEmployees.Location = new Point(0, currentY);
                currentY += spacing;
            }

            if (showReports)
            {
                btnReports.Location = new Point(0, currentY);
                currentY += spacing;
            }
        }
    }
}