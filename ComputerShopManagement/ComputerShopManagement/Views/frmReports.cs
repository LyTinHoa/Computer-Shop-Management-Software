using ComputerShopManagement.Models;
using ComputerShopManagement.Controllers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ComputerShopManagement.Views
{
    public partial class frmReports : Form
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
        private ReportController _controller;

        // Premium Brand Colors
        private Color techBlue = Color.FromArgb(41, 128, 185);
        private Color deepText = Color.FromArgb(44, 62, 80);
        private Color lightGray = Color.FromArgb(240, 242, 245);
        private Color dangerRed = Color.FromArgb(231, 76, 60);
        private Color successGreen = Color.FromArgb(46, 204, 113);

        // UI Components
        private Panel pnlTitleBar, pnlFilters, pnlFilterContainer, pnlKPIs, pnlGridContainer;
        private Panel btnBack, btnGenerate;
        private ComboBox cmbReportType;
        private DateTimePicker dtpStart, dtpEnd;
        private Label lblType, lblStart, lblEnd;
        private DataGridView dgvReport;

        // KPI Cards
        private Panel cardMetric1, cardMetric2, cardMetric3;

        // Prevents infinite loops when programmatically changing dates
        private bool _isUpdatingDates = false;

        public frmReports(Staff currentUser)
        {
            _currentUser = currentUser;
            _controller = new ReportController();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            SetupResponsiveUI();

            this.OnResize(EventArgs.Empty);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ReflowFilterBar();
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
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, control, new object[] { true });
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
            int titleHeight = 80;
            pnlTitleBar = new Panel { Dock = DockStyle.Top, Height = titleHeight, BackColor = Color.White };
            pnlTitleBar.Resize += (s, e) => pnlTitleBar.Invalidate();
            pnlTitleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } };
            this.Controls.Add(pnlTitleBar);

            pnlTitleBar.Paint += (s, e) => {
                e.Graphics.Clear(Color.White);
                e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230), 1), 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
            };

            Label lblTitle = new Label { Text = "BitTekk | Report/Analytics", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(30, 0, 0, 0), Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = deepText, BackColor = Color.Transparent };
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
                this.WindowState = this.WindowState == FormWindowState.Normal ? FormWindowState.Maximized : FormWindowState.Normal;
            };

            Button btnClose = new Button(); SetupWindowBtn(btnClose, "Close");
            btnClose.Click += (s, e) => {
                if (MessageBox.Show("Are you sure you want to exit the application?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();

            pnlWindowControls.Controls.Add(btnClose); pnlWindowControls.Controls.Add(btnMax); pnlWindowControls.Controls.Add(btnMin);


            // ==========================================
            // 2. THE FILTER BAR & BACK BUTTON
            // ==========================================
            pnlFilters = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.Transparent };
            this.Controls.Add(pnlFilters);
            pnlFilters.BringToFront();

            btnBack = new Panel { Size = new Size(110, 45), Cursor = Cursors.Hand, BackColor = Color.Transparent, Location = new Point(30, 27) };
            EnableDoubleBuffering(btnBack);
            bool isBackHovered = false;
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
            pnlFilters.Controls.Add(btnBack);

            pnlFilterContainer = new Panel { Height = 60, BackColor = Color.Transparent };
            pnlFilterContainer.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pnlFilterContainer.Width - 1, pnlFilterContainer.Height - 1), 30))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(new Pen(Color.LightGray, 1), path);
                }
            };
            pnlFilters.Controls.Add(pnlFilterContainer);

            // FIX: Enforced "dd/MM/yyyy" format for regional consistency
            lblType = new Label { Text = "Report Type:", Font = new Font("Segoe UI Semibold", 12), ForeColor = deepText, AutoSize = true, BackColor = Color.White };
            cmbReportType = new ComboBox { Font = new Font("Segoe UI", 12), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReportType.Items.AddRange(new string[] { "Daily Sales", "Weekly Sales", "Monthly Sales", "Inventory Valuation" });

            lblStart = new Label { Text = "Start:", Font = new Font("Segoe UI Semibold", 12), ForeColor = deepText, AutoSize = true, BackColor = Color.White };
            dtpStart = new DateTimePicker { Font = new Font("Segoe UI", 12), Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 140 };

            lblEnd = new Label { Text = "End:", Font = new Font("Segoe UI Semibold", 12), ForeColor = deepText, AutoSize = true, BackColor = Color.White };
            dtpEnd = new DateTimePicker { Font = new Font("Segoe UI", 12), Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 140 };

            btnGenerate = new Panel { Size = new Size(160, 42), Cursor = Cursors.Hand, BackColor = Color.White };
            EnableDoubleBuffering(btnGenerate);
            bool isGenHovered = false;
            btnGenerate.MouseEnter += (s, e) => { if (!isGenHovered) { isGenHovered = true; btnGenerate.Invalidate(); } };
            btnGenerate.MouseLeave += (s, e) => { if (isGenHovered) { isGenHovered = false; btnGenerate.Invalidate(); } };
            btnGenerate.Click += BtnGenerate_Click;
            btnGenerate.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btnGenerate.Width - 1, btnGenerate.Height - 1), 20))
                {
                    e.Graphics.FillPath(new SolidBrush(isGenHovered ? Color.FromArgb(39, 174, 96) : successGreen), path);
                }
                TextRenderer.DrawText(e.Graphics, "GENERATE", new Font("Segoe UI Semibold", 11), new Rectangle(0, 0, btnGenerate.Width, btnGenerate.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            pnlFilterContainer.Controls.AddRange(new Control[] { lblType, cmbReportType, lblStart, dtpStart, lblEnd, dtpEnd, btnGenerate });

            // Hook up the Smart Date Engine
            cmbReportType.SelectedIndexChanged += CmbReportType_SelectedIndexChanged;
            dtpStart.ValueChanged += (s, e) => CalculateDateBounds(dtpStart.Value);
            dtpEnd.ValueChanged += (s, e) => CalculateDateBounds(dtpEnd.Value);

            // Trigger default state
            cmbReportType.SelectedIndex = 0;

            // ==========================================
            // 3. KPI METRIC CARDS (Dynamic Zone)
            // ==========================================
            pnlKPIs = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Color.Transparent };
            this.Controls.Add(pnlKPIs);
            pnlKPIs.BringToFront();

            cardMetric1 = CreateMetricCard("Total Revenue", "$0.00", "Awaiting generation");
            cardMetric2 = CreateMetricCard("Items Sold", "0", "Awaiting generation");
            cardMetric3 = CreateMetricCard("Top Performing Category", "N/A", "Awaiting generation");

            pnlKPIs.Controls.Add(cardMetric1);
            pnlKPIs.Controls.Add(cardMetric2);
            pnlKPIs.Controls.Add(cardMetric3);

            // ==========================================
            // 4. PREMIUM DATA GRID
            // ==========================================
            pnlGridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30, 10, 30, 30), BackColor = Color.Transparent };
            this.Controls.Add(pnlGridContainer);
            pnlGridContainer.BringToFront();

            Panel pnlGridBorder = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlGridBorder.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pnlGridBorder.Width - 1, pnlGridBorder.Height - 1), 20))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(new Pen(Color.LightGray, 1), path);
                }
            };
            pnlGridContainer.Controls.Add(pnlGridBorder);

            dgvReport = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(230, 230, 230)
            };

            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = lightGray;
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = deepText;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionBackColor = lightGray;
            dgvReport.ColumnHeadersDefaultCellStyle.SelectionForeColor = deepText;
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 13);
            dgvReport.ColumnHeadersHeight = 50;

            dgvReport.DefaultCellStyle.BackColor = Color.White;
            dgvReport.DefaultCellStyle.ForeColor = deepText;
            dgvReport.DefaultCellStyle.Font = new Font("Segoe UI", 12);
            dgvReport.DefaultCellStyle.SelectionBackColor = techBlue;
            dgvReport.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvReport.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgvReport.RowTemplate.Height = 50;

            Panel pnlGridWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            pnlGridWrapper.Controls.Add(dgvReport);
            pnlGridBorder.Controls.Add(pnlGridWrapper);

            // ==========================================
            // MASTER RESIZE ENGINE
            // ==========================================
            this.Resize += (s, e) => {
                ReflowFilterBar();

                int cardWidth = 360;
                int gap = 50;
                int totalKPIWidth = (cardWidth * 3) + (gap * 2);
                int startX = (this.Width - totalKPIWidth) / 2;

                cardMetric1.Location = new Point(startX, 10);
                cardMetric2.Location = new Point(startX + cardWidth + gap, 10);
                cardMetric3.Location = new Point(startX + (cardWidth + gap) * 2, 10);
            };
        }

        // ==========================================
        // DYNAMIC LAYOUT & SMART DATE ENGINE
        // ==========================================
        private void CmbReportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Temporarily lift all constraints so we can mathematically manipulate dates safely
            dtpStart.MinDate = DateTimePicker.MinimumDateTime;
            dtpStart.MaxDate = DateTimePicker.MaximumDateTime;
            dtpEnd.MinDate = DateTimePicker.MinimumDateTime;
            dtpEnd.MaxDate = DateTimePicker.MaximumDateTime;

            string type = cmbReportType.SelectedItem.ToString();
            DateTime today = DateTime.Today;

            if (type == "Daily Sales")
            {
                lblStart.Text = "Date:";
                lblStart.Visible = true;
                dtpStart.Visible = true;
                lblEnd.Visible = false;
                dtpEnd.Visible = false;

                _isUpdatingDates = true;
                dtpStart.Value = today;

                // User can no longer pick a date past today for Daily Sales
                dtpStart.MaxDate = today;

                _isUpdatingDates = false;
            }
            else if (type == "Weekly Sales" || type == "Monthly Sales")
            {
                lblStart.Text = "Start:";
                lblStart.Visible = true;
                dtpStart.Visible = true;
                lblEnd.Visible = true;
                dtpEnd.Visible = true;

                CalculateDateBounds(today);
            }
            else if (type == "Inventory Valuation")
            {
                lblStart.Visible = false;
                dtpStart.Visible = false;
                lblEnd.Visible = false;
                dtpEnd.Visible = false;
            }

            ReflowFilterBar();
        }

        private void CalculateDateBounds(DateTime referenceDate)
        {
            if (_isUpdatingDates) return;
            _isUpdatingDates = true;

            // Lift constraints to calculate safely
            dtpStart.MinDate = DateTimePicker.MinimumDateTime;
            dtpStart.MaxDate = DateTimePicker.MaximumDateTime;
            dtpEnd.MinDate = DateTimePicker.MinimumDateTime;
            dtpEnd.MaxDate = DateTimePicker.MaximumDateTime;

            string type = cmbReportType.SelectedItem.ToString();
            DateTime today = DateTime.Today;

            // FIX: Safely lock "Daily Sales" back to maximum of Today if ValueChanged is fired
            if (type == "Daily Sales")
            {
                dtpStart.MaxDate = today;
            }
            else if (type == "Weekly Sales")
            {
                int day = referenceDate.Day;
                int year = referenceDate.Year;
                int month = referenceDate.Month;
                DateTime start, end;

                if (day <= 7) { start = new DateTime(year, month, 1); end = new DateTime(year, month, 7); }
                else if (day <= 14) { start = new DateTime(year, month, 8); end = new DateTime(year, month, 14); }
                else if (day <= 21) { start = new DateTime(year, month, 15); end = new DateTime(year, month, 21); }
                else if (day <= 28) { start = new DateTime(year, month, 22); end = new DateTime(year, month, 28); }
                else
                {
                    start = new DateTime(year, month, 29);
                    end = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                }

                if (end > today) end = today;

                dtpStart.Value = start;
                dtpEnd.Value = end;

                dtpStart.MaxDate = end;
                dtpEnd.MinDate = start;
                dtpEnd.MaxDate = today;
            }
            else if (type == "Monthly Sales")
            {
                DateTime start = new DateTime(referenceDate.Year, referenceDate.Month, 1);
                DateTime end = new DateTime(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month));

                if (end > today) end = today;

                dtpStart.Value = start;
                dtpEnd.Value = end;

                dtpStart.MaxDate = end;
                dtpEnd.MinDate = start;
                dtpEnd.MaxDate = today;
            }

            _isUpdatingDates = false;
        }

        private void ReflowFilterBar()
        {
            if (pnlFilterContainer == null) return;

            int currentX = 30;

            lblType.Location = new Point(currentX, 18);
            cmbReportType.Location = new Point(lblType.Right + 15, 15);
            currentX = cmbReportType.Right + 30;

            if (lblStart.Visible)
            {
                lblStart.Location = new Point(currentX, 18);
                dtpStart.Location = new Point(lblStart.Right + 15, 15);
                currentX = dtpStart.Right + 30;
            }

            if (lblEnd.Visible)
            {
                lblEnd.Location = new Point(currentX, 18);
                dtpEnd.Location = new Point(lblEnd.Right + 15, 15);
                currentX = dtpEnd.Right + 30;
            }

            btnGenerate.Location = new Point(currentX, 9);

            pnlFilterContainer.Width = btnGenerate.Right + 30;

            pnlFilterContainer.Left = (pnlFilters.Width - pnlFilterContainer.Width) / 2;
            pnlFilterContainer.Top = (pnlFilters.Height - pnlFilterContainer.Height) / 2;
        }


        private Panel CreateMetricCard(string title, string value, string subtitle)
        {
            Panel card = new Panel { Size = new Size(360, 150), BackColor = Color.Transparent };
            EnableDoubleBuffering(card);

            card.Tag = new string[] { title, value, subtitle };

            card.Paint += (s, e) => {
                string[] data = (string[])card.Tag;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 20))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.White), path);

                    e.Graphics.SetClip(path);
                    e.Graphics.FillRectangle(new SolidBrush(techBlue), 0, 0, 8, card.Height);
                    e.Graphics.ResetClip();

                    e.Graphics.DrawPath(new Pen(Color.LightGray, 1), path);
                }

                e.Graphics.DrawString(data[0], new Font("Segoe UI Semibold", 13), new SolidBrush(Color.Gray), new Point(30, 20));

                float fontSize = 32f;
                Font valueFont = new Font("Segoe UI", fontSize, FontStyle.Bold);
                while (e.Graphics.MeasureString(data[1], valueFont).Width > card.Width - 40 && fontSize > 12f)
                {
                    fontSize -= 2f;
                    valueFont.Dispose();
                    valueFont = new Font("Segoe UI", fontSize, FontStyle.Bold);
                }

                int yPos = 45 + (int)((32f - fontSize) / 2);
                e.Graphics.DrawString(data[1], valueFont, new SolidBrush(deepText), new Point(25, yPos));
                valueFont.Dispose();

                e.Graphics.DrawString(data[2], new Font("Segoe UI", 11), new SolidBrush(techBlue), new Point(30, 110));
            };
            return card;
        }

        private void UpdateMetricCard(Panel card, string value, string subtitle)
        {
            string[] data = (string[])card.Tag;
            data[1] = value;
            data[2] = subtitle;
            card.Invalidate();
        }

        // ==========================================
        // DATABASE BRIDGE SIMULATOR (Backend Readiness)
        // ==========================================
        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            System.Threading.Thread.Sleep(400);

            string reportType = cmbReportType.SelectedItem.ToString();
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date;

            // In production, this directly replaces the simulation:
            // DataTable dt = _controller.GetReportData(reportType, startDate, endDate);
            DataTable dt = SimulateDatabaseFetch(reportType, startDate, endDate);

            dgvReport.DataSource = dt;
            Cursor = Cursors.Default;
        }

        private DataTable SimulateDatabaseFetch(string reportType, DateTime start, DateTime end)
        {
            DataTable dt = new DataTable();

            if (reportType == "Daily Sales")
            {
                dt.Columns.Add("Date");
                dt.Columns.Add("Invoice ID");
                dt.Columns.Add("Customer Name");
                dt.Columns.Add("Items Purchased");
                dt.Columns.Add("Total Amount");

                // FIX: Used explicit dd/MM/yyyy pattern
                string displayDate = start.ToString("dd/MM/yyyy");

                dt.Rows.Add(displayDate, "INV-10045", "Alice Johnson", "NVIDIA RTX 4090", "$1,599.99");
                dt.Rows.Add(displayDate, "INV-10046", "Robert Smith", "Intel Core i9-14900K", "$589.50");
                dt.Rows.Add(displayDate, "INV-10047", "Maria Garcia", "Samsung 990 PRO 2TB", "$169.99");

                UpdateMetricCard(cardMetric1, "$2,359.48", "Gross Revenue (Today)");
                UpdateMetricCard(cardMetric2, "3", "Transactions (Today)");
                UpdateMetricCard(cardMetric3, "Graphics Cards", "Top Category (Today)");
            }
            else if (reportType == "Weekly Sales")
            {
                dt.Columns.Add("Week Of");
                dt.Columns.Add("Total Invoices");
                dt.Columns.Add("Unique Customers");
                dt.Columns.Add("Items Sold");
                dt.Columns.Add("Weekly Gross");

                string weekDisplay = $"{start.ToString("dd MMM")} - {end.ToString("dd MMM")}";

                dt.Rows.Add(weekDisplay, "28", "22", "45", "$14,580.00");
                dt.Rows.Add("01 May - 07 May", "32", "29", "51", "$16,220.50");
                dt.Rows.Add("22 Apr - 28 Apr", "25", "20", "39", "$12,400.00");

                UpdateMetricCard(cardMetric1, "$14,580.00", "Gross Revenue (Selected Week)");
                UpdateMetricCard(cardMetric2, "45", "Units Moved (Selected Week)");
                UpdateMetricCard(cardMetric3, "Processors", "Top Category (Selected Week)");
            }
            else if (reportType == "Monthly Sales")
            {
                dt.Columns.Add("Month");
                dt.Columns.Add("Total Invoices");
                dt.Columns.Add("Unique Customers");
                dt.Columns.Add("Items Sold");
                dt.Columns.Add("Monthly Gross");

                string monthDisplay = start.ToString("MMMM yyyy");

                dt.Rows.Add(monthDisplay, "112", "98", "180", "$52,400.00");
                dt.Rows.Add("April 2026", "145", "120", "210", "$68,950.25");
                dt.Rows.Add("March 2026", "130", "110", "195", "$61,200.00");

                UpdateMetricCard(cardMetric1, "$52,400.00", "Gross Revenue (Selected Month)");
                UpdateMetricCard(cardMetric2, "180", "Units Moved (Selected Month)");
                UpdateMetricCard(cardMetric3, "Graphics Cards", "Top Category (Selected Month)");
            }
            else if (reportType == "Inventory Valuation")
            {
                dt.Columns.Add("Category");
                dt.Columns.Add("Product Name");
                dt.Columns.Add("Current Stock");
                dt.Columns.Add("Unit Cost");
                dt.Columns.Add("Total Asset Value");

                dt.Rows.Add("Graphics Cards", "NVIDIA RTX 4090", "5", "$1,400.00", "$7,000.00");
                dt.Rows.Add("Processors", "Intel Core i9-14900K", "12", "$500.00", "$6,000.00");
                dt.Rows.Add("Storage", "Samsung 990 PRO 2TB", "20", "$120.00", "$2,400.00");
                dt.Rows.Add("Memory", "Corsair 32GB DDR5", "30", "$90.00", "$2,700.00");

                UpdateMetricCard(cardMetric1, "$18,100.00", "Total Capital in Inventory");
                UpdateMetricCard(cardMetric2, "67", "Total Physical Units");
                UpdateMetricCard(cardMetric3, "Graphics Cards", "Highest Capital Concentration");
            }

            return dt;
        }
    }
}
