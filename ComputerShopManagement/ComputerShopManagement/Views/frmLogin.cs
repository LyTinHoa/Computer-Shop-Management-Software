using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ComputerShopManagement.Controllers;

namespace ComputerShopManagement.Views
{
    public partial class frmLogin : Form
    {
        // --- NATIVE WINDOWS 11 DWM API INJECTION ---
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private AccountController _controller;
        private int _loginAttempts;

        private Panel pnlLeft;
        private Panel pnlRight;
        private Panel pnlUserField;
        private TextBox txtUsername;
        private Panel pnlPassField;
        private TextBox txtPassword;

        private Panel btnSubmit;
        private Panel btnCloseApp;
        private Label lblPassPlaceholder;
        private Label lblUserPlaceholder;
        private Button btnShowHide;
        private Label lblError;
        private Label lblAttempts;

        private bool isSubmitHovered = false;
        private bool isCloseHovered = false;

        public frmLogin()
        {
            InitializeComponent();
            _controller = new AccountController();
            _loginAttempts = 0;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            SetupUltimateModernUI();
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

        private void SetupUltimateModernUI()
        {
            Color deepText = Color.FromArgb(44, 62, 80);

            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Text = "BitTekk Secure Login";

            pnlLeft = new Panel
            {
                Size = new Size(450, 650),
                Location = new Point(0, 0),
                Dock = DockStyle.Left
            };

            pnlLeft.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                // FIX: True anti-aliasing forces smooth blending over gradients
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (LinearGradientBrush brush = new LinearGradientBrush(pnlLeft.ClientRectangle, Color.FromArgb(15, 32, 39), Color.FromArgb(41, 128, 185), 45f))
                {
                    e.Graphics.FillRectangle(brush, pnlLeft.ClientRectangle);
                }

                using (Font titleFont = new Font("Segoe UI", 56, FontStyle.Bold))
                {
                    e.Graphics.DrawString("BitTekk", titleFont, new SolidBrush(Color.FromArgb(100, 0, 0, 0)), new PointF(52, 192));
                    e.Graphics.DrawString("BitTekk", titleFont, Brushes.White, new PointF(50, 190));
                }

                using (Font subFont = new Font("Segoe UI Light", 15, FontStyle.Regular))
                {
                    // FIX: Realigned to specific project requirements
                    e.Graphics.DrawString("Computer Shop Management", subFont, new SolidBrush(Color.FromArgb(220, 255, 255, 255)), new PointF(60, 290));
                }

                using (Font copyFont = new Font("Segoe UI", 9, FontStyle.Regular))
                {
                    e.Graphics.DrawString("© 2026 BitTekk Systems.", copyFont, new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new PointF(60, 600));
                }
            };
            this.Controls.Add(pnlLeft);

            pnlRight = new Panel
            {
                Size = new Size(550, 650),
                Location = new Point(450, 0),
                BackColor = Color.White,
                Dock = DockStyle.Right
            };

            pnlRight.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // FIX: Ensure headers also get the premium smoothing treatment
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                using (Font headerFont = new Font("Segoe UI Semibold", 28, FontStyle.Regular))
                {
                    e.Graphics.DrawString("Sign In", headerFont, new SolidBrush(deepText), new PointF(70, 130));
                }
            };
            this.Controls.Add(pnlRight);

            int startX = 75;
            int controlWidth = 400;

            btnCloseApp = new Panel { Size = new Size(36, 36), Location = new Point(490, 15), Cursor = Cursors.Hand, BackColor = Color.White };
            btnCloseApp.MouseEnter += (s, e) => { isCloseHovered = true; btnCloseApp.Invalidate(); };
            btnCloseApp.MouseLeave += (s, e) => { isCloseHovered = false; btnCloseApp.Invalidate(); };
            btnCloseApp.Click += (s, e) => { Application.Exit(); };
            btnCloseApp.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (isCloseHovered)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(231, 76, 60)), 0, 0, btnCloseApp.Width - 1, btnCloseApp.Height - 1);
                }
                using (Pen pen = new Pen(isCloseHovered ? Color.White : Color.Gray, 2f))
                {
                    int padding = 12;
                    e.Graphics.DrawLine(pen, padding, padding, btnCloseApp.Width - padding, btnCloseApp.Height - padding);
                    e.Graphics.DrawLine(pen, btnCloseApp.Width - padding, padding, padding, btnCloseApp.Height - padding);
                }
            };
            pnlRight.Controls.Add(btnCloseApp);

            pnlUserField = new Panel { Location = new Point(startX, 240), Size = new Size(controlWidth, 50), BackColor = Color.White, Cursor = Cursors.IBeam };
            lblUserPlaceholder = new Label { Text = "Username", Font = new Font("Segoe UI", 12), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(5, 10) };
            txtUsername = new TextBox { Location = new Point(5, 10), Size = new Size(controlWidth - 10, 35), Font = new Font("Segoe UI", 14), BorderStyle = BorderStyle.None, ForeColor = deepText };
            pnlUserField.Controls.Add(lblUserPlaceholder);
            pnlUserField.Controls.Add(txtUsername);
            pnlRight.Controls.Add(pnlUserField);

            pnlPassField = new Panel { Location = new Point(startX, 330), Size = new Size(controlWidth, 50), BackColor = Color.White, Cursor = Cursors.IBeam };
            lblPassPlaceholder = new Label { Text = "Password", Font = new Font("Segoe UI", 12), ForeColor = Color.LightGray, AutoSize = true, Location = new Point(5, 10) };
            txtPassword = new TextBox { Location = new Point(5, 10), Size = new Size(controlWidth - 75, 35), Font = new Font("Segoe UI", 14), BorderStyle = BorderStyle.None, PasswordChar = '•', ForeColor = deepText };

            btnShowHide = new Button { Text = "Show", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(41, 128, 185), FlatStyle = FlatStyle.Flat, Size = new Size(65, 30), Location = new Point(controlWidth - 70, 8), Cursor = Cursors.Hand, BackColor = Color.White };
            btnShowHide.FlatAppearance.BorderSize = 0;
            btnShowHide.Click += (s, e) =>
            {
                txtPassword.PasswordChar = txtPassword.PasswordChar == '•' ? '\0' : '•';
                btnShowHide.Text = txtPassword.PasswordChar == '•' ? "Show" : "Hide";
                txtPassword.Focus();
            };

            pnlPassField.Controls.Add(lblPassPlaceholder);
            pnlPassField.Controls.Add(txtPassword);
            pnlPassField.Controls.Add(btnShowHide);
            pnlRight.Controls.Add(pnlPassField);

            Action<TextBox, Label, Panel> setupInputEvents = (txt, lbl, pnl) =>
            {
                lbl.Click += (s, e) => txt.Focus();
                txt.Enter += (s, e) => { lbl.Visible = false; pnl.Invalidate(); };
                txt.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) lbl.Visible = true; pnl.Invalidate(); };
                txt.TextChanged += (s, e) => { lbl.Visible = string.IsNullOrWhiteSpace(txt.Text) && !txt.Focused; };
            };
            setupInputEvents(txtUsername, lblUserPlaceholder, pnlUserField);
            setupInputEvents(txtPassword, lblPassPlaceholder, pnlPassField);

            pnlUserField.Paint += CustomPanel_PaintBorder;
            pnlPassField.Paint += CustomPanel_PaintBorder;

            btnSubmit = new Panel { Location = new Point(startX, 430), Size = new Size(controlWidth, 55), Cursor = Cursors.Hand };
            btnSubmit.MouseEnter += (s, e) => { isSubmitHovered = true; btnSubmit.Invalidate(); };
            btnSubmit.MouseLeave += (s, e) => { isSubmitHovered = false; btnSubmit.Invalidate(); };
            btnSubmit.Click += btnSubmit_Click;
            btnSubmit.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                // FIX: High fidelity blending on the button text
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                Rectangle rect = new Rectangle(0, 0, btnSubmit.Width - 1, btnSubmit.Height - 1);
                int radius = 25;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    Color c1 = isSubmitHovered ? Color.FromArgb(52, 152, 219) : Color.FromArgb(41, 128, 185);
                    Color c2 = isSubmitHovered ? Color.FromArgb(41, 128, 185) : Color.FromArgb(31, 97, 141);

                    using (LinearGradientBrush brush = new LinearGradientBrush(rect, c1, c2, 90f))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                using (Font btnFont = new Font("Segoe UI Semibold", 15, FontStyle.Regular))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("CONTINUE", btnFont, Brushes.White, rect, sf);
                }
            };
            pnlRight.Controls.Add(btnSubmit);

            lblError = new Label { Location = new Point(startX, 500), Size = new Size(controlWidth, 20), Font = new Font("Segoe UI", 9), ForeColor = Color.Red, Visible = false, TextAlign = ContentAlignment.MiddleCenter };
            pnlRight.Controls.Add(lblError);

            lblAttempts = new Label { Location = new Point(startX, 525), Size = new Size(controlWidth, 20), Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, Visible = false, TextAlign = ContentAlignment.MiddleCenter };
            pnlRight.Controls.Add(lblAttempts);
        }

        private void CustomPanel_PaintBorder(object sender, PaintEventArgs e)
        {
            Panel p = sender as Panel;
            bool isFocused = (p == pnlUserField && txtUsername.Focused) || (p == pnlPassField && txtPassword.Focused);
            Color borderColor = isFocused ? Color.FromArgb(41, 128, 185) : Color.FromArgb(220, 220, 220);

            using (Pen borderPen = new Pen(borderColor, isFocused ? 2f : 1f))
            {
                e.Graphics.DrawLine(borderPen, 0, p.Height - 1, p.Width, p.Height - 1);
            }
        }
        // --- Allows pressing the "Enter" key to submit the login form ---
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                // Triggers the login button logic automatically
                btnSubmit_Click(this, EventArgs.Empty);
                return true; // Tells Windows we handled the key press
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            lblAttempts.Visible = false;
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter both username and password.";
                lblError.Visible = true;
                return;
            }

            // Calls the controller which now returns an integer status code
            int authStatus = _controller.Authenticate(username, password);

            if (authStatus == 1) // 1 = Success
            {
                // Create the dashboard and pass the authenticated user data to it
                frmMainDashboard dashboard = new frmMainDashboard(_controller.CurrentUser);
                dashboard.Show();
                this.Hide();
            }
            else
            {
                _loginAttempts++;
                lblError.Visible = true;

                if (_loginAttempts > 0)
                {
                    lblAttempts.Text = $"Login attempt {_loginAttempts} of 3.";
                    lblAttempts.Visible = true;
                }

                // Determine error message based on the specific failure
                if (authStatus == -1)
                {
                    lblError.Text = "Username not found. Please try again.";
                }
                else if (authStatus == -2)
                {
                    lblError.Text = "Incorrect password. Please try again.";
                }

                // Clear and focus the exact fields based on the error
                clearFields(authStatus);

                if (_loginAttempts >= 3)
                {
                    MessageBox.Show("Maximum login attempts reached. Application will close for security.", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
            }
        }

        // Handles the targeted clearing and focusing based on the exact error
        public void clearFields(int status)
        {
            if (status == -2)
            {
                // Password was wrong: Clear only password, focus on password
                txtPassword.Clear();
                txtPassword.Focus();
            }
            else
            {
                // Username was wrong (or any other error): Clear both, focus on username
                txtUsername.Clear();
                txtPassword.Clear();
                txtUsername.Focus();
            }
        }
    }
}
