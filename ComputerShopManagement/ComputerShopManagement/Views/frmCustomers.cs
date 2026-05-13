using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Views
{
    public partial class frmCustomers : Form
    {
        // --- NATIVE WINDOWS 11 DWM API ---
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

        private CustomerController _controller;

        // UI Layout Elements
        private Panel pnlTopBar, pnlBackground, pnlLeft, pnlRight;
        private Label lblListTitle, lblFormTitle;
        private DataGridView dgvCustomers;
        private TextBox txtFullName, txtEmail, txtPhone;
        private Panel btnAddCustomer, btnClearForm, btnBack;
        private Button btnMaximize;
        private bool isAddHovered = false, isClearHovered = false;

        // Search Bar Elements
        private Panel pnlSearchBox;
        private TextBox txtSearch;
        private Label lblSearchPlaceholder;

        // Responsive Scale Trackers
        private float _btnBackScale = 1.0f;
        private float _searchScale = 1.0f;

        // SILENT AUTO-LOOKUP ENGINE UI
        private Panel pnlStatusBadge;
        private string _badgeText = "";
        private Color _badgeColor = Color.Gray;
        private bool _wasAutoFilled = false;

        private class InputLayout
        {
            public Label Lbl;
            public TextBox Txt;
            public int BaseY;
            public int BaseWidth;
        }
        private List<InputLayout> rightInputs = new List<InputLayout>();

        public frmCustomers()
        {
            _controller = new CustomerController();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupModernCustomerUI();
            LoadCustomerData();

            // Trigger initial layout math
            this.OnResize(EventArgs.Empty);
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

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, control, new object[] { true });
        }

        private void SetupModernCustomerUI()
        {
            this.Size = new Size(1200, 750);
            this.MinimumSize = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(41, 128, 185);

            Color techBlue = Color.FromArgb(41, 128, 185);
            Color deepText = Color.FromArgb(44, 62, 80);
            int pad = 2;

            // --- TOP BAR ---
            pnlTopBar = new Panel { Location = new Point(pad, pad), Size = new Size(this.ClientSize.Width - (pad * 2), 80), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlTopBar.MouseDown += DragWindow_MouseDown;
            this.Controls.Add(pnlTopBar);

            pnlBackground = new Panel { Location = new Point(pad, pad + 80), Size = new Size(this.ClientSize.Width - (pad * 2), this.ClientSize.Height - (pad * 2) - 80), BackColor = Color.FromArgb(245, 246, 250), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.Add(pnlBackground);

            pnlTopBar.Paint += (s, e) => {
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230), 1), 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            };

            Label lblTitle = new Label { Text = "BitTekk | Customer Management", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0), Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = deepText, BackColor = Color.Transparent };
            lblTitle.MouseDown += DragWindow_MouseDown;
            pnlTopBar.Controls.Add(lblTitle);

            Panel pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 150 };
            pnlTopBar.Controls.Add(pnlWindowControls);
            pnlWindowControls.BringToFront();

            Action<Button, string> SetupWindowBtn = (btn, type) => {
                btn.Size = new Size(50, 45);
                btn.Location = new Point(type == "Min" ? 0 : type == "Max" ? 50 : 100, 0);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Cursor = Cursors.Hand;
                btn.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(type == "Close" && btn.BackColor == Color.FromArgb(231, 76, 60) ? Color.White : Color.FromArgb(80, 80, 80), 2.5f))
                    {
                        int cx = 25, cy = 22;
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
            pnlLeft = new Panel { BackColor = Color.White };
            pnlBackground.Controls.Add(pnlLeft);

            lblListTitle = new Label { Text = "Customer Directory", ForeColor = deepText, AutoSize = true };
            pnlLeft.Controls.Add(lblListTitle);

            // --- UNIFIED SEARCH BAR ---
            pnlSearchBox = new Panel { BackColor = Color.White, Cursor = Cursors.IBeam };
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, pnlSearchBox, new object[] { true });

            lblSearchPlaceholder = new Label { Text = "Search by Phone or Invoice...", ForeColor = Color.Gray, AutoSize = true, Cursor = Cursors.IBeam };
            txtSearch = new TextBox { BorderStyle = BorderStyle.None, ForeColor = deepText };

            lblSearchPlaceholder.Click += (s, e) => txtSearch.Focus();
            txtSearch.Enter += (s, e) => { lblSearchPlaceholder.Visible = false; pnlSearchBox.Invalidate(); };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) lblSearchPlaceholder.Visible = true; pnlSearchBox.Invalidate(); };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            pnlSearchBox.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pnlSearchBox.Width - 1, pnlSearchBox.Height - 1), 15))
                {
                    e.Graphics.DrawPath(new Pen(txtSearch.Focused ? techBlue : Color.FromArgb(200, 200, 200), txtSearch.Focused ? 2f : 1f), path);
                }
            };

            pnlSearchBox.Controls.Add(lblSearchPlaceholder);
            pnlSearchBox.Controls.Add(txtSearch);
            pnlLeft.Controls.Add(pnlSearchBox);

            dgvCustomers = CreateModernGrid();

            // 1. Create a wrapper panel that handles the smooth scrolling
            Panel pnlGridWrapper = new Panel
            {
                Name = "GridWrapper",
                BackColor = Color.White,
                AutoScroll = true // This enables pixel-smooth scrolling!
            };

            // 2. Add the grid to the wrapper, and the wrapper to the left panel
            pnlGridWrapper.Controls.Add(dgvCustomers);
            pnlLeft.Controls.Add(pnlGridWrapper);

            btnBack = new Panel { Cursor = Cursors.Hand, BackColor = Color.Transparent };
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, btnBack, new object[] { true });

            bool isBackHovered = false;
            btnBack.MouseEnter += (s, e) => { if (!isBackHovered) { isBackHovered = true; btnBack.Invalidate(); } };
            btnBack.MouseLeave += (s, e) => { if (isBackHovered) { isBackHovered = false; btnBack.Invalidate(); } };
            btnBack.Click += (s, e) => this.Close();

            btnBack.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int radius = 20;
                Rectangle rect = new Rectangle(0, 0, btnBack.Width - 1, btnBack.Height - 1);
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();
                    e.Graphics.FillPath(new SolidBrush(isBackHovered ? Color.FromArgb(31, 97, 141) : techBlue), path);
                }
                float fontSz = 12 * _btnBackScale;
                TextRenderer.DrawText(e.Graphics, "← Back", new Font("Segoe UI Semibold", Math.Max(9f, fontSz)), rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            pnlLeft.Controls.Add(btnBack);

            // --- RIGHT PANEL ---
            pnlRight = new Panel { BackColor = Color.White };
            pnlBackground.Controls.Add(pnlRight);

            lblFormTitle = new Label { Text = "Add New Customer", ForeColor = deepText, AutoSize = true };
            pnlRight.Controls.Add(lblFormTitle);

            // SILENT AUTO-LOOKUP UI
            pnlStatusBadge = new Panel
            {
                Size = new Size(120, 24),
                BackColor = Color.White,
                Visible = false
            };
            EnableDoubleBuffering(pnlStatusBadge);
            pnlStatusBadge.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(Color.White);
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pnlStatusBadge.Width - 1, pnlStatusBadge.Height - 1), 12))
                {
                    e.Graphics.FillPath(new SolidBrush(_badgeColor), path);
                }
                TextRenderer.DrawText(e.Graphics, _badgeText, new Font("Segoe UI Semibold", 9), new Rectangle(0, 0, pnlStatusBadge.Width, pnlStatusBadge.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlRight.Controls.Add(pnlStatusBadge);

            // Use the original helper, but inject the MaxLength and KeyPress events into txtPhone
            txtFullName = CreateLabeledInput(pnlRight, "Full Name *:", 70, 380);
            txtPhone = CreateLabeledInput(pnlRight, "Phone Number *:", 135, 380);
            txtEmail = CreateLabeledInput(pnlRight, "Email Address (Optional):", 200, 380);

            // Apply specific configurations to txtPhone
            txtPhone.MaxLength = 11;
            txtPhone.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) { e.Handled = true; }
            };
            txtPhone.TextChanged += TxtPhone_TextChanged;

            btnAddCustomer = CreateCustomButton(pnlRight, "ADD CUSTOMER", techBlue, Color.FromArgb(52, 152, 219), ref isAddHovered);
            btnAddCustomer.Click += BtnAddCustomer_Click;

            btnClearForm = CreateCustomButton(pnlRight, "CLEAR FORM", Color.FromArgb(149, 165, 166), Color.FromArgb(189, 195, 199), ref isClearHovered);
            btnClearForm.Click += (s, e) => ClearForm();

            // ==========================================
            // RESPONSIVE RESIZE ENGINE
            // ==========================================
            this.Resize += (s, e) => {
                if (pnlBackground == null || pnlLeft == null || pnlRight == null) return;

                float rawScaleX = this.ClientSize.Width / 1200f;
                float rawScaleY = this.ClientSize.Height / 750f;
                float minRawScale = Math.Min(rawScaleX, rawScaleY);

                float scaleLeft = Math.Min(1.10f, Math.Max(1.0f, minRawScale));
                float scaleRight = Math.Min(1.20f, Math.Max(1.0f, minRawScale));

                _btnBackScale = scaleLeft;
                _searchScale = scaleLeft;

                int newRightWidth = (int)(420 * scaleRight);
                int gap = 40;
                int padEdge = 20;

                pnlRight.Width = newRightWidth;
                pnlRight.Height = pnlBackground.Height - 40;
                pnlRight.Location = new Point(pnlBackground.Width - newRightWidth - padEdge, padEdge);

                lblFormTitle.Font = new Font("Segoe UI Semibold", Math.Max(10f, 14 * scaleRight));
                lblFormTitle.Location = new Point((int)(20 * scaleRight), (int)(20 * scaleRight));

                foreach (var input in rightInputs)
                {
                    input.Lbl.Font = new Font("Segoe UI", Math.Max(8f, 10 * scaleRight));
                    input.Lbl.Location = new Point((int)(20 * scaleRight), (int)(input.BaseY * scaleRight));

                    input.Txt.Font = new Font("Segoe UI", Math.Max(9f, 12 * scaleRight));
                    input.Txt.Size = new Size((int)(input.BaseWidth * scaleRight), (int)(30 * scaleRight));
                    input.Txt.Location = new Point((int)(20 * scaleRight), (int)(input.BaseY * scaleRight) + (int)(25 * scaleRight));

                    // Dynamically position the status badge relative to the phone number label
                    if (input.Txt == txtPhone)
                    {
                        pnlStatusBadge.Location = new Point(input.Lbl.Right + 10, input.Lbl.Top - 2);
                    }
                }

                if (btnAddCustomer != null)
                {
                    btnAddCustomer.Size = new Size((int)(380 * scaleRight), (int)(55 * scaleRight));
                    btnAddCustomer.Location = new Point((int)(20 * scaleRight), pnlRight.Height - (int)(150 * scaleRight));
                }

                if (btnClearForm != null)
                {
                    btnClearForm.Size = new Size((int)(380 * scaleRight), (int)(55 * scaleRight));
                    btnClearForm.Location = new Point((int)(20 * scaleRight), pnlRight.Height - (int)(75 * scaleRight));
                }

                pnlLeft.Width = pnlRight.Left - padEdge - gap;
                pnlLeft.Height = pnlBackground.Height - 40;
                pnlLeft.Location = new Point(padEdge, padEdge);

                lblListTitle.Font = new Font("Segoe UI Semibold", Math.Max(10f, 14 * scaleLeft));
                lblListTitle.Location = new Point((int)(20 * scaleLeft), (int)(20 * scaleLeft));

                // Scale and Position the Search Bar
                if (pnlSearchBox != null)
                {
                    pnlSearchBox.Size = new Size((int)(260 * scaleLeft), (int)(38 * scaleLeft));
                    pnlSearchBox.Location = new Point(pnlLeft.Width - pnlSearchBox.Width - (int)(20 * scaleLeft), (int)(15 * scaleLeft));

                    txtSearch.Font = new Font("Segoe UI", Math.Max(9f, 11 * scaleLeft));
                    txtSearch.Size = new Size(pnlSearchBox.Width - (int)(30 * scaleLeft), (int)(25 * scaleLeft));
                    txtSearch.Location = new Point((int)(15 * scaleLeft), (pnlSearchBox.Height - txtSearch.Height) / 2);

                    lblSearchPlaceholder.Font = new Font("Segoe UI", Math.Max(8f, 10 * scaleLeft));
                    lblSearchPlaceholder.Location = new Point((int)(15 * scaleLeft), (pnlSearchBox.Height - lblSearchPlaceholder.Height) / 2);
                }

                if (btnBack != null)
                {
                    btnBack.Size = new Size((int)(110 * scaleLeft), (int)(45 * scaleLeft));
                    btnBack.Location = new Point((int)(20 * scaleLeft), pnlLeft.Height - btnBack.Height - (int)(20 * scaleLeft));
                }

                Panel wrapper = (Panel)pnlLeft.Controls["GridWrapper"];
                if (wrapper != null && dgvCustomers != null)
                {
                    int newWidth = pnlLeft.Width - (int)(40 * scaleLeft);
                    int newHeight = Math.Max(100, btnBack.Top - wrapper.Top - (int)(20 * scaleLeft));

                    wrapper.Location = new Point((int)(20 * scaleLeft), (int)(65 * scaleLeft));
                    wrapper.Size = new Size(newWidth, newHeight);

                    // --- ANTI-GLITCH SCROLL FIX ---
                    // Save current scroll and snap to top before resizing child controls
                    int currentScroll = wrapper.VerticalScroll.Value;
                    wrapper.AutoScrollPosition = new Point(0, 0);

                    dgvCustomers.Location = new Point(0, 0);
                    dgvCustomers.Width = wrapper.ClientSize.Width;

                    // Fonts
                    dgvCustomers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", Math.Max(9f, 11 * scaleLeft));
                    dgvCustomers.DefaultCellStyle.Font = new Font("Segoe UI", Math.Max(9f, 11 * scaleLeft));
                    dgvCustomers.ColumnHeadersHeight = (int)(45 * scaleLeft);

                    // --- RECALCULATE HEIGHT DYNAMICALLY ---
                    // Now that the width and fonts have changed, the text wrapping will change. 
                    // We must recalculate the new perfect height.
                    RecalculateGridHeight();

                    // Restore the scroll position safely
                    wrapper.AutoScrollPosition = new Point(0, currentScroll);
                }
                pnlLeft.Invalidate();
                pnlRight.Invalidate();
            };
        }

        // Search Filter Logic
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            lblSearchPlaceholder.Visible = string.IsNullOrWhiteSpace(txtSearch.Text) && !txtSearch.Focused;
            ApplyCustomerSearchFilter();
        }

        private void ApplyCustomerSearchFilter()
        {
            if (dgvCustomers.DataSource is DataTable dt)
            {
                string query = txtSearch.Text.Trim().Replace("'", "''"); // Prevents SQL syntax crashes

                if (string.IsNullOrWhiteSpace(query))
                {
                    dt.DefaultView.RowFilter = "";
                }
                else
                {
                    // This single line searches both Phone Numbers AND Invoice IDs simultaneously
                    dt.DefaultView.RowFilter = $"[Phone Number] LIKE '%{query}%' OR [Invoices] LIKE '%{query}%'";
                }
            }
        }

        private TextBox CreateLabeledInput(Panel parent, string labelText, int y, int width)
        {
            Label lbl = new Label { Text = labelText, ForeColor = Color.Gray, AutoSize = true };
            parent.Controls.Add(lbl);
            TextBox txt = new TextBox { BorderStyle = BorderStyle.FixedSingle };
            parent.Controls.Add(txt);
            rightInputs.Add(new InputLayout { Lbl = lbl, Txt = txt, BaseY = y, BaseWidth = width });
            return txt;
        }

        private Panel CreateCustomButton(Panel parent, string text, Color normalColor, Color hoverColor, ref bool hoverFlag)
        {
            Panel btn = new Panel { Cursor = Cursors.Hand };
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, btn, new object[] { true });

            bool localHover = false;
            btn.MouseEnter += (s, e) => { localHover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { localHover = false; btn.Invalidate(); };
            btn.Resize += (s, e) => btn.Invalidate();

            btn.Paint += (s, e) =>
            {
                e.Graphics.Clear(Color.White);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                int radius = (int)(btn.Height * 0.20);

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    using (SolidBrush brush = new SolidBrush(localHover ? hoverColor : normalColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                float btnFontSize = Math.Max(10f, btn.Height * 0.25f);
                using (Font btnFont = new Font("Segoe UI Semibold", btnFontSize))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString(text, btnFont, Brushes.White, rect, sf);
                }
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
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // --- SCROLLING PERFORMANCE FIXES ---
            grid.ScrollBars = ScrollBars.None; // Kill the native row-snapping scrollbar
            // Enable advanced Double Buffering directly on the DataGridView to stop flickering
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, grid, new object[] { true });

            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // CHANGE: Only calculate heights for the rows actually drawn on the screen
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;

            // -----------------------------------

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(44, 62, 80);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 246, 250);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);

            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 245, 251);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);
            grid.DefaultCellStyle.Padding = new Padding(5, 10, 5, 10);

            grid.CellMouseDown += (s, e) => {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0) grid.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.FromArgb(225, 230, 235);
            };

            grid.CellMouseUp += (s, e) => {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0) grid.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.FromArgb(245, 246, 250);
            };

            grid.CellMouseLeave += (s, e) => {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0) grid.Columns[e.ColumnIndex].HeaderCell.Style.BackColor = Color.FromArgb(245, 246, 250);
            };

            grid.CellPainting += DgvCustomers_CellPainting;
            grid.CellClick += DgvCustomers_DeleteClick;

            return grid;
        }

        private void LoadCustomerData()
        {
            dgvCustomers.DataSource = _controller.GetAllCustomers();
            dgvCustomers.Columns["customerID"].Visible = false;

            dgvCustomers.Columns["Full Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCustomers.Columns["Email"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCustomers.Columns["Phone Number"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            // Format the Invoices column
            dgvCustomers.Columns["Invoices"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCustomers.Columns["Invoices"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            if (!dgvCustomers.Columns.Contains("DeleteAction"))
            {
                DataGridViewTextBoxColumn btnDelete = new DataGridViewTextBoxColumn();
                btnDelete.Name = "DeleteAction";
                btnDelete.HeaderText = "Action";
                btnDelete.Width = 80;
                btnDelete.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvCustomers.Columns.Add(btnDelete);
            }
            // Calculate the exact pixel height required to show ALL rows without an internal scrollbar
            dgvCustomers.DataBindingComplete += (s, e) =>
            {
                RecalculateGridHeight();
            };
        }

        // ==========================================
        // SILENT AUTO-LOOKUP ENGINE
        // ==========================================
        private void TxtPhone_TextChanged(object sender, EventArgs e)
        {
            string phone = txtPhone.Text.Trim();

            if (phone.Length >= 10)
            {
                Customer customer = _controller.GetCustomerByPhone(phone);
                if (customer != null)
                {
                    txtFullName.Text = customer.FullName;
                    txtEmail.Text = customer.Email;
                    _wasAutoFilled = true;
                    ShowBadge("Customer Found", Color.FromArgb(46, 204, 113));
                }
                else
                {
                    if (_wasAutoFilled) { txtFullName.Clear(); txtEmail.Clear(); _wasAutoFilled = false; }
                    ShowBadge("New Customer", Color.FromArgb(41, 128, 185)); // techBlue
                }
            }
            else
            {
                if (_wasAutoFilled) { txtFullName.Clear(); txtEmail.Clear(); _wasAutoFilled = false; }
                pnlStatusBadge.Visible = false;
            }
        }

        private void ShowBadge(string text, Color color)
        {
            _badgeText = text;
            _badgeColor = color;
            pnlStatusBadge.Visible = true;
            pnlStatusBadge.Invalidate();
        }

        private void BtnAddCustomer_Click(object sender, EventArgs e)
        {
            string phone = txtPhone.Text.Trim();
            string name = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();

            // 1. Strict Phone Validation
            if (string.IsNullOrEmpty(phone) || phone.Length < 10)
            {
                MessageBox.Show("Please enter a valid phone number containing at least 10 digits.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPhone.Focus();
                return;
            }

            // 2. Strict Name Validation
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Full Name is mandatory to register a customer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFullName.Focus();
                return;
            }

            // 3. Email Validation
            if (!string.IsNullOrEmpty(email))
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(email, emailPattern))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
            }

            Customer newCustomer = new Customer
            {
                FullName = name,
                Email = email,
                Phone = phone
            };

            string errorMsg;
            if (_controller.AddCustomer(newCustomer, out errorMsg))
            {
                MessageBox.Show("Customer added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCustomerData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(errorMsg, "Creation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            txtFullName.Clear();
            txtEmail.Clear();
            txtPhone.Clear();
            pnlStatusBadge.Visible = false;
            txtPhone.Focus();
        }

        private void DragWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void DgvCustomers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvCustomers.Columns[e.ColumnIndex].Name == "DeleteAction")
            {
                // Paint the background of the cell normally
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                int cellH = e.CellBounds.Height;
                int paddingX = 15;

                // --- RESPONSIVE BUTTON SIZING LOGIC ---
                int targetHeight = cellH - 16;
                int responsiveHeight = Math.Max((int)(cellH * 0.40), 28);
                targetHeight = Math.Min(targetHeight, responsiveHeight);
                targetHeight = Math.Min(targetHeight, 200);

                int btnWidth = e.CellBounds.Width - (paddingX * 2);
                int btnHeight = targetHeight;
                int btnX = e.CellBounds.X + paddingX;
                int btnY = e.CellBounds.Y + (cellH - btnHeight) / 2;

                Rectangle rect = new Rectangle(btnX, btnY, btnWidth, btnHeight);

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

                // Draw the Trash Icon
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

        private void RecalculateGridHeight()
        {
            if (dgvCustomers == null) return;

            int headerHeight = dgvCustomers.ColumnHeadersVisible ? dgvCustomers.ColumnHeadersHeight : 0;
            int rowsHeight = dgvCustomers.Rows.Count > 0 ? dgvCustomers.Rows.GetRowsHeight(DataGridViewElementStates.None) : 0;

            dgvCustomers.Height = headerHeight + rowsHeight + 2;
        }

        private void DgvCustomers_DeleteClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvCustomers.Columns[e.ColumnIndex].Name == "DeleteAction")
            {
                if (MessageBox.Show("Are you sure you want to permanently delete this customer?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    int customerId = Convert.ToInt32(dgvCustomers.Rows[e.RowIndex].Cells["customerID"].Value);
                    string errorMsg;
                    if (_controller.DeleteCustomer(customerId, out errorMsg))
                    {
                        LoadCustomerData();
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
