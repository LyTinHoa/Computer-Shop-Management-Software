using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Views
{
    public partial class frmEmployeeAdmin : Form
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

        private AccountController _controller;
        private Staff _currentManager;

        // UI Elements
        private Panel pnlTopBar, pnlBackground, pnlLeft, pnlRight;
        private Label lblListTitle, lblFormTitle;
        private DataGridView dgvEmployees;
        private TextBox txtFullName, txtUsername, txtPassword, txtManagerAuth;
        private ComboBox cmbRole;
        private Panel btnAddEmployee, btnClearForm, btnBack;

        // Responsive Scale Trackers
        private class InputLayout
        {
            public Label Lbl;
            public Control Ctrl;
            public int BaseY;
            public int BaseWidth;
        }
        private List<InputLayout> rightInputs = new List<InputLayout>();

        public frmEmployeeAdmin(Staff currentManager)
        {
            _currentManager = currentManager;
            _controller = new AccountController();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupUI();
            LoadEmployeeData();
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

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, control, new object[] { true });
        }

        private void SetupUI()
        {
            this.Size = new Size(1200, 750);
            this.MinimumSize = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(41, 128, 185);

            Color deepText = Color.FromArgb(44, 62, 80);
            int pad = 2;

            // --- TOP BAR ---
            pnlTopBar = new Panel { Location = new Point(pad, pad), Size = new Size(this.ClientSize.Width - (pad * 2), 80), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlTopBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            pnlTopBar.Paint += (s, e) => {
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230), 1), 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            };
            this.Controls.Add(pnlTopBar);

            Label lblTitle = new Label { Text = "BitTekk | Employee Admin", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0), Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = deepText, BackColor = Color.Transparent };
            lblTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            pnlTopBar.Controls.Add(lblTitle);

            // Window Controls
            Panel pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 150 };
            pnlTopBar.Controls.Add(pnlWindowControls);
            pnlWindowControls.BringToFront();

            Action<Button, string> SetupWindowBtn = (btn, type) => {
                btn.Size = new Size(50, 45); btn.Location = new Point(type == "Min" ? 0 : type == "Max" ? 50 : 100, 0); btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.Cursor = Cursors.Hand;
                btn.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(type == "Close" && btn.BackColor == Color.FromArgb(231, 76, 60) ? Color.White : Color.FromArgb(80, 80, 80), 2.5f))
                    {
                        int cx = 25, cy = 22;
                        if (type == "Min") e.Graphics.DrawLine(pen, cx - 7, cy + 5, cx + 7, cy + 5);
                        else if (type == "Max") e.Graphics.DrawRectangle(pen, cx - 6, cy - 5, 12, 10);
                        else if (type == "Close") { e.Graphics.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6); e.Graphics.DrawLine(pen, cx + 6, cy - 6, cx - 6, cy + 6); }
                    }
                };
                btn.MouseEnter += (s, e) => { btn.BackColor = type == "Close" ? Color.FromArgb(231, 76, 60) : Color.FromArgb(220, 220, 220); };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.White; };
            };
            Button btnMin = new Button(); SetupWindowBtn(btnMin, "Min"); btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            Button btnMax = new Button(); SetupWindowBtn(btnMax, "Max"); btnMax.Click += (s, e) => this.WindowState = this.WindowState == FormWindowState.Normal ? FormWindowState.Maximized : FormWindowState.Normal;
            Button btnClose = new Button(); SetupWindowBtn(btnClose, "Close");
            btnClose.Click += (s, e) => {
                if (MessageBox.Show("Are you sure you want to exit the application?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };
            pnlWindowControls.Controls.Add(btnClose); pnlWindowControls.Controls.Add(btnMax); pnlWindowControls.Controls.Add(btnMin);
            pnlTopBar.DoubleClick += (s, e) => btnMax.PerformClick();

            // --- BACKGROUND ---
            pnlBackground = new Panel { Location = new Point(pad, pad + 80), Size = new Size(this.ClientSize.Width - (pad * 2), this.ClientSize.Height - (pad * 2) - 80), BackColor = Color.FromArgb(245, 246, 250), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.Add(pnlBackground);

            // --- LEFT PANEL (Grid) ---
            pnlLeft = new Panel { BackColor = Color.White };
            pnlBackground.Controls.Add(pnlLeft);

            lblListTitle = new Label { Text = "Staff Directory", ForeColor = deepText, AutoSize = true };
            pnlLeft.Controls.Add(lblListTitle);

            dgvEmployees = CreateGrid();
            pnlLeft.Controls.Add(dgvEmployees);

            btnBack = new Panel { Size = new Size(110, 45), Cursor = Cursors.Hand, BackColor = Color.Transparent, Location = new Point(30, 27) };
            EnableDoubleBuffering(btnBack);

            bool isBackHovered = false;
            btnBack.MouseEnter += (s, e) => { isBackHovered = true; btnBack.Invalidate(); };
            btnBack.MouseLeave += (s, e) => { isBackHovered = false; btnBack.Invalidate(); };
            btnBack.Click += (s, e) => this.Close();
            btnBack.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int r = 20; Rectangle rect = new Rectangle(0, 0, btnBack.Width - 1, btnBack.Height - 1);
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90); path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90); path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90); path.CloseFigure();
                    e.Graphics.FillPath(new SolidBrush(isBackHovered ? Color.FromArgb(31, 97, 141) : Color.FromArgb(41, 128, 185)), path);
                }
                float fontSz = 12 * Math.Min(1.10f, Math.Max(1.0f, Math.Min(this.ClientSize.Width / 1200f, this.ClientSize.Height / 750f)));
                TextRenderer.DrawText(e.Graphics, "← Back", new Font("Segoe UI Semibold", Math.Max(9f, fontSz)), rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlLeft.Controls.Add(btnBack);

            // --- RIGHT PANEL (Form) ---
            pnlRight = new Panel { BackColor = Color.White };
            pnlBackground.Controls.Add(pnlRight);

            lblFormTitle = new Label { Text = "Create New Account", ForeColor = deepText, AutoSize = true };
            pnlRight.Controls.Add(lblFormTitle);

            txtFullName = CreateLabeledInput(pnlRight, "Full Name:", 70, 380, false);
            txtUsername = CreateLabeledInput(pnlRight, "Username:", 135, 380, false);
            txtPassword = CreateLabeledInput(pnlRight, "Account Password:", 200, 380, true);

            Label lblRole = new Label { Text = "System Role:", ForeColor = Color.Gray, AutoSize = true };
            cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12) };
            cmbRole.Items.AddRange(new string[] { "Manager", "Sales", "Inventory" });
            pnlRight.Controls.Add(lblRole);
            pnlRight.Controls.Add(cmbRole);
            rightInputs.Add(new InputLayout { Lbl = lblRole, Ctrl = cmbRole, BaseY = 265, BaseWidth = 380 });

            txtManagerAuth = CreateLabeledInput(pnlRight, "Enter Your Manager Password to Confirm", 400, 380, true);

            btnAddEmployee = CreateCustomButton(pnlRight, "AUTHORIZE & CREATE", Color.FromArgb(41, 128, 185), Color.FromArgb(52, 152, 219));
            btnAddEmployee.Click += BtnAddEmployee_Click;

            btnClearForm = CreateCustomButton(pnlRight, "CLEAR INFORMATION", Color.FromArgb(149, 165, 166), Color.FromArgb(189, 195, 199));
            btnClearForm.Click += (s, e) => ClearForm();

            // --- RESIZE ENGINE ---
            this.Resize += (s, e) => {
                float rawScaleX = this.ClientSize.Width / 1200f; float rawScaleY = this.ClientSize.Height / 750f;
                float minRawScale = Math.Min(rawScaleX, rawScaleY);
                float scaleLeft = Math.Min(1.10f, Math.Max(1.0f, minRawScale));
                float scaleRight = Math.Min(1.20f, Math.Max(1.0f, minRawScale));

                int newRightWidth = (int)(420 * scaleRight);
                int gap = 40; int padEdge = 20;

                pnlRight.Width = newRightWidth;
                pnlRight.Height = pnlBackground.Height - 40;
                pnlRight.Location = new Point(pnlBackground.Width - newRightWidth - padEdge, padEdge);

                lblFormTitle.Font = new Font("Segoe UI Semibold", Math.Max(10f, 14 * scaleRight));
                lblFormTitle.Location = new Point((int)(20 * scaleRight), (int)(20 * scaleRight));

                foreach (var input in rightInputs)
                {
                    input.Lbl.Font = new Font("Segoe UI", Math.Max(8f, 10 * scaleRight));
                    input.Lbl.Location = new Point((int)(20 * scaleRight), (int)(input.BaseY * scaleRight));
                    input.Ctrl.Font = new Font("Segoe UI", Math.Max(9f, 12 * scaleRight));
                    input.Ctrl.Size = new Size((int)(input.BaseWidth * scaleRight), (int)(30 * scaleRight));
                    input.Ctrl.Location = new Point((int)(20 * scaleRight), (int)(input.BaseY * scaleRight) + (int)(25 * scaleRight));
                }

                btnAddEmployee.Size = new Size((int)(380 * scaleRight), (int)(55 * scaleRight));
                btnAddEmployee.Location = new Point((int)(20 * scaleRight), pnlRight.Height - (int)(150 * scaleRight));
                btnClearForm.Size = new Size((int)(380 * scaleRight), (int)(55 * scaleRight));
                btnClearForm.Location = new Point((int)(20 * scaleRight), pnlRight.Height - (int)(75 * scaleRight));

                pnlLeft.Width = pnlRight.Left - padEdge - gap;
                pnlLeft.Height = pnlBackground.Height - 40;
                pnlLeft.Location = new Point(padEdge, padEdge);

                lblListTitle.Font = new Font("Segoe UI Semibold", Math.Max(10f, 14 * scaleLeft));
                lblListTitle.Location = new Point((int)(20 * scaleLeft), (int)(20 * scaleLeft));

                btnBack.Size = new Size((int)(110 * scaleLeft), (int)(45 * scaleLeft));
                btnBack.Location = new Point((int)(20 * scaleLeft), pnlLeft.Height - btnBack.Height - (int)(20 * scaleLeft));

                dgvEmployees.Location = new Point((int)(20 * scaleLeft), (int)(60 * scaleLeft));
                dgvEmployees.Size = new Size(pnlLeft.Width - (int)(40 * scaleLeft), btnBack.Top - dgvEmployees.Top - (int)(20 * scaleLeft));
                dgvEmployees.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", Math.Max(9f, 11 * scaleLeft));
                dgvEmployees.DefaultCellStyle.Font = new Font("Segoe UI", Math.Max(9f, 11 * scaleLeft));
                dgvEmployees.ColumnHeadersHeight = (int)(45 * scaleLeft);
            };
        }

        private TextBox CreateLabeledInput(Panel parent, string labelText, int y, int width, bool isPassword)
        {
            Label lbl = new Label { Text = labelText, ForeColor = Color.Gray, AutoSize = true };
            parent.Controls.Add(lbl);
            TextBox txt = new TextBox { BorderStyle = BorderStyle.FixedSingle, PasswordChar = isPassword ? '•' : '\0' };
            parent.Controls.Add(txt);
            rightInputs.Add(new InputLayout { Lbl = lbl, Ctrl = txt, BaseY = y, BaseWidth = width });
            return txt;
        }

        private Panel CreateCustomButton(Panel parent, string text, Color normalColor, Color hoverColor)
        {
            Panel btn = new Panel { Cursor = Cursors.Hand };
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, btn, new object[] { true });
            bool localHover = false;
            btn.MouseEnter += (s, e) => { localHover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { localHover = false; btn.Invalidate(); };
            btn.Resize += (s, e) => btn.Invalidate();
            btn.Paint += (s, e) => {
                e.Graphics.Clear(Color.White);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                int r = (int)(btn.Height * 0.20);
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90); path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90); path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90); path.CloseFigure();
                    e.Graphics.FillPath(new SolidBrush(localHover ? hoverColor : normalColor), path);
                }
                float btnFontSize = Math.Max(10f, btn.Height * 0.25f);
                using (Font btnFont = new Font("Segoe UI Semibold", btnFontSize)) { e.Graphics.DrawString(text, btnFont, Brushes.White, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }); }
            };
            parent.Controls.Add(btn);
            return btn;
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,

                // FAILSAFE: Force the native grid lines to be white just in case Windows tries to draw them
                GridColor = Color.White
            };

            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(44, 62, 80);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 246, 250);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 245, 251);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);
            grid.DefaultCellStyle.Padding = new Padding(5, 10, 5, 10);

            // Eradicate the dotted focus box on click
            grid.RowPrePaint += (s, e) => {
                e.PaintParts &= ~DataGridViewPaintParts.Focus;
            };

            grid.CellPainting += (s, e) => {

                // THE FIX: Reset the paintbrush to default for every single cell. 
                // This prevents anti-aliasing bleeds from creating random black lines.
                e.Graphics.SmoothingMode = SmoothingMode.None;

                // 1. Paint Column Headers
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    using (SolidBrush brush = new SolidBrush(grid.ColumnHeadersDefaultCellStyle.BackColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width + 1, e.CellBounds.Height));
                    }
                    e.PaintContent(e.CellBounds);
                    using (Pen pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                    {
                        e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    }
                    e.Handled = true;
                    return;
                }

                bool isSelected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
                Color bgColor = isSelected ? grid.DefaultCellStyle.SelectionBackColor : grid.DefaultCellStyle.BackColor;

                // 2. Paint the Custom Buttons (Delete & Reset)
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && (grid.Columns[e.ColumnIndex].Name == "DeleteAction" || grid.Columns[e.ColumnIndex].Name == "ResetAction"))
                {
                    using (SolidBrush brush = new SolidBrush(bgColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width + 1, e.CellBounds.Height));
                    }

                    // Turn ON AntiAlias strictly and ONLY for our custom round shapes
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    int pX = 8, pY = 8;
                    Rectangle rect = new Rectangle(e.CellBounds.X + pX, e.CellBounds.Y + pY, e.CellBounds.Width - (pX * 2), e.CellBounds.Height - (pY * 2));
                    int r = Math.Max(1, (int)(rect.Height * 0.30));

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90); path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90); path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90); path.CloseFigure();

                        if (grid.Columns[e.ColumnIndex].Name == "DeleteAction")
                        {
                            int rowStaffId = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["staffID"].Value);
                            bool isSelf = (rowStaffId == _currentManager.StaffID);
                            Color btnColor = isSelf ? Color.FromArgb(200, 200, 200) : Color.FromArgb(231, 76, 60);
                            e.Graphics.FillPath(new SolidBrush(btnColor), path);

                            using (Pen pen = new Pen(Color.White, 2f))
                            {
                                int cx = e.CellBounds.X + (e.CellBounds.Width / 2), cy = e.CellBounds.Y + (e.CellBounds.Height / 2);
                                e.Graphics.DrawLine(pen, cx - 8, cy - 6, cx + 8, cy - 6); e.Graphics.DrawLine(pen, cx - 3, cy - 6, cx - 3, cy - 9); e.Graphics.DrawLine(pen, cx + 3, cy - 6, cx + 3, cy - 9); e.Graphics.DrawLine(pen, cx - 3, cy - 9, cx + 3, cy - 9); e.Graphics.DrawLine(pen, cx - 6, cy - 4, cx - 4, cy + 8); e.Graphics.DrawLine(pen, cx + 6, cy - 4, cx + 4, cy + 8); e.Graphics.DrawLine(pen, cx - 4, cy + 8, cx + 4, cy + 8); e.Graphics.DrawLine(pen, cx - 1, cy - 2, cx - 1, cy + 5); e.Graphics.DrawLine(pen, cx + 2, cy - 2, cx + 2, cy + 5);
                            }
                        }
                        else
                        {
                            e.Graphics.FillPath(new SolidBrush(Color.FromArgb(243, 156, 18)), path);

                            // Text needs GridFit, not raw AntiAlias, to prevent blurry edges
                            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                            TextRenderer.DrawText(e.Graphics, "Reset Password", new Font("Segoe UI Semibold", 9), rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }

                    // Turn OFF AntiAlias before drawing straight dividing lines
                    e.Graphics.SmoothingMode = SmoothingMode.None;
                    using (Pen pen = new Pen(Color.FromArgb(235, 235, 235), 1))
                    {
                        e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    }

                    e.Handled = true;
                    return;
                }

                // 3. Paint ALL Normal Data Cells
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    using (SolidBrush brush = new SolidBrush(bgColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width + 1, e.CellBounds.Height));
                    }

                    e.PaintContent(e.CellBounds);

                    using (Pen pen = new Pen(Color.FromArgb(235, 235, 235), 1))
                    {
                        e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    }
                    e.Handled = true;
                }
            };

            grid.CellClick += DgvEmployees_ActionClick;

            grid.DataBindingComplete += (s, e) => {
                grid.ClearSelection();
                grid.CurrentCell = null;
            };

            return grid;
        }

        private void LoadEmployeeData()
        {
            dgvEmployees.DataSource = _controller.GetAllEmployees();

            dgvEmployees.Columns["staffID"].Visible = true;
            dgvEmployees.Columns["staffID"].HeaderText = "ID";
            dgvEmployees.Columns["staffID"].DisplayIndex = 0;

            if (!dgvEmployees.Columns.Contains("ResetAction"))
            {
                DataGridViewTextBoxColumn btnReset = new DataGridViewTextBoxColumn
                {
                    Name = "ResetAction",
                    HeaderText = "",
                    Width = 140,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                dgvEmployees.Columns.Add(btnReset);
            }

            if (!dgvEmployees.Columns.Contains("DeleteAction"))
            {
                DataGridViewTextBoxColumn btnDelete = new DataGridViewTextBoxColumn
                {
                    Name = "DeleteAction",
                    HeaderText = "",
                    Width = 60,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                dgvEmployees.Columns.Add(btnDelete);
            }
        }

        private void BtnAddEmployee_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text) || cmbRole.SelectedIndex == -1)
            {
                MessageBox.Show("Please fill out all employee details and select a role.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(txtManagerAuth.Text))
            {
                MessageBox.Show("Security layer requires your Manager Password to create accounts.", "Authorization Required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }

            Staff newEmployee = new Staff { FullName = txtFullName.Text.Trim(), Username = txtUsername.Text.Trim(), Password = txtPassword.Text.Trim(), Role = cmbRole.SelectedItem.ToString() };
            string errorMsg;
            if (_controller.AddEmployee(newEmployee, txtManagerAuth.Text, _currentManager, out errorMsg))
            {
                MessageBox.Show($"Account for {newEmployee.FullName} created successfully.\nRole assigned: {newEmployee.Role}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadEmployeeData(); ClearForm();
            }
            else
            {
                MessageBox.Show(errorMsg, "Creation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtManagerAuth.Clear(); txtManagerAuth.Focus();
            }
        }

        private void DgvEmployees_ActionClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                int targetId = Convert.ToInt32(dgvEmployees.Rows[e.RowIndex].Cells["staffID"].Value);
                string empName = dgvEmployees.Rows[e.RowIndex].Cells["Full Name"].Value.ToString();

                if (dgvEmployees.Columns[e.ColumnIndex].Name == "DeleteAction")
                {
                    if (targetId == _currentManager.StaffID)
                    {
                        MessageBox.Show("Security Safeguard: You cannot delete your own active session account.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }

                    string authPassword = RequestManagerPassword(empName);
                    if (authPassword == null) return;

                    string errorMsg;
                    if (_controller.DeleteEmployee(targetId, authPassword, _currentManager, out errorMsg))
                    {
                        LoadEmployeeData();
                    }
                    else
                    {
                        MessageBox.Show(errorMsg, "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (dgvEmployees.Columns[e.ColumnIndex].Name == "ResetAction")
                {
                    if (ShowResetPasswordModal(empName, out string newPass, out string mgrPass))
                    {
                        if (string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(mgrPass))
                        {
                            MessageBox.Show("Both a new password and manager authorization are required.", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        if (_controller.ResetEmployeePassword(targetId, newPass, mgrPass, _currentManager, out string errorMsg))
                        {
                            MessageBox.Show($"Password successfully reset for {empName}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(errorMsg, "Reset Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private string RequestManagerPassword(string targetName)
        {
            string input = null;
            using (Form prompt = new Form { Width = 350, Height = 220, FormBorderStyle = FormBorderStyle.None, BackColor = Color.White, StartPosition = FormStartPosition.CenterParent })
            {
                prompt.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(231, 76, 60), 2), 0, 0, prompt.Width - 1, prompt.Height - 1);
                prompt.Controls.Add(new Label { Text = $"Authorize deletion of {targetName}", Left = 20, Top = 20, AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = Color.FromArgb(44, 62, 80) });
                prompt.Controls.Add(new Label { Text = "Manager Password:", Left = 20, Top = 50, AutoSize = true, ForeColor = Color.Gray });

                TextBox txtPass = new TextBox { Left = 20, Top = 75, Width = 310, Font = new Font("Segoe UI", 12), PasswordChar = '•', BorderStyle = BorderStyle.FixedSingle };
                prompt.Controls.Add(txtPass);

                Button btnOk = new Button { Text = "Confirm", Left = 20, Top = 140, Width = 140, Height = 40, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10), Cursor = Cursors.Hand };
                btnOk.FlatAppearance.BorderSize = 0; btnOk.Click += (s, e) => { input = txtPass.Text; prompt.DialogResult = DialogResult.OK; };

                Button btnCancel = new Button { Text = "Cancel", Left = 190, Top = 140, Width = 140, Height = 40, BackColor = Color.LightGray, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderSize = 0; btnCancel.Click += (s, e) => { prompt.DialogResult = DialogResult.Cancel; };

                prompt.Controls.Add(btnOk); prompt.Controls.Add(btnCancel);
                prompt.ShowDialog();
            }
            return input;
        }

        private bool ShowResetPasswordModal(string targetName, out string newPass, out string managerPass)
        {
            newPass = "";
            managerPass = "";
            bool result = false;

            string tempNewPass = "";
            string tempMgrPass = "";

            using (Form prompt = new Form { Width = 350, Height = 280, FormBorderStyle = FormBorderStyle.None, BackColor = Color.White, StartPosition = FormStartPosition.CenterParent })
            {
                prompt.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(243, 156, 18), 2), 0, 0, prompt.Width - 1, prompt.Height - 1);
                prompt.Controls.Add(new Label { Text = $"Reset Password for {targetName}", Left = 20, Top = 20, AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = Color.FromArgb(44, 62, 80) });

                prompt.Controls.Add(new Label { Text = "New Account Password:", Left = 20, Top = 50, AutoSize = true, ForeColor = Color.Gray });

                TextBox txtNew = new TextBox { Left = 20, Top = 75, Width = 240, Font = new Font("Segoe UI", 12), BorderStyle = BorderStyle.FixedSingle, PasswordChar = '•' };
                prompt.Controls.Add(txtNew);

                Button btnShowHide = new Button { Text = "Show", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(41, 128, 185), FlatStyle = FlatStyle.Flat, Size = new Size(65, 29), Left = 265, Top = 74, Cursor = Cursors.Hand, BackColor = Color.White };
                btnShowHide.FlatAppearance.BorderSize = 0;
                btnShowHide.Click += (s, e) => {
                    txtNew.PasswordChar = txtNew.PasswordChar == '•' ? '\0' : '•';
                    btnShowHide.Text = txtNew.PasswordChar == '•' ? "Show" : "Hide";
                    txtNew.Focus();
                };
                prompt.Controls.Add(btnShowHide);

                prompt.Controls.Add(new Label { Text = "Manager Password:", Left = 20, Top = 115, AutoSize = true, ForeColor = Color.Gray });
                TextBox txtMgr = new TextBox { Left = 20, Top = 140, Width = 310, Font = new Font("Segoe UI", 12), PasswordChar = '•', BorderStyle = BorderStyle.FixedSingle };
                prompt.Controls.Add(txtMgr);

                Button btnOk = new Button { Text = "Reset", Left = 20, Top = 200, Width = 140, Height = 40, BackColor = Color.FromArgb(243, 156, 18), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10), Cursor = Cursors.Hand };
                btnOk.FlatAppearance.BorderSize = 0;

                btnOk.Click += (s, e) => {
                    tempNewPass = txtNew.Text;
                    tempMgrPass = txtMgr.Text;
                    result = true;
                    prompt.Close();
                };

                Button btnCancel = new Button { Text = "Cancel", Left = 190, Top = 200, Width = 140, Height = 40, BackColor = Color.LightGray, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderSize = 0; btnCancel.Click += (s, e) => prompt.Close();

                prompt.Controls.Add(btnOk); prompt.Controls.Add(btnCancel);
                prompt.ShowDialog();
            }

            if (result)
            {
                newPass = tempNewPass;
                managerPass = tempMgrPass;
            }

            return result;
        }

        private void ClearForm() { txtFullName.Clear(); txtUsername.Clear(); txtPassword.Clear(); txtManagerAuth.Clear(); cmbRole.SelectedIndex = -1; txtFullName.Focus(); }
    }
}