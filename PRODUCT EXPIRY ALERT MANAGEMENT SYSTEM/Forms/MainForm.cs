using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;
using ExpiryAlertSystem.Views;

namespace ExpiryAlertSystem.Forms
{
    /// <summary>
    /// The main window. Layout:
    ///
    ///   +-----------+------------------------------------------+
    ///   |  SIDEBAR  |  top bar (user, date, alert bell)        |
    ///   |  menu     +------------------------------------------+
    ///   |  buttons  |                                          |
    ///   |           |  CONTENT AREA - the selected screen      |
    ///   |           |  (Dashboard, Products, Alerts, ...)      |
    ///   +-----------+------------------------------------------+
    ///
    /// It also runs a TIMER that checks for expiring products every few minutes and shows a
    /// Windows notification (system tray balloon) when new alerts appear.
    /// </summary>
    public class MainForm : Form
    {
        public static MainForm Instance { get; private set; }

        private readonly Panel _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(28, 22, 28, 22) };
        private readonly Panel _sidebar = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Sidebar };
        private readonly Label _bell = new Label();
        private readonly Label _clock = new Label();
        private readonly Dictionary<string, Button> _menu = new Dictionary<string, Button>();
        private readonly Timer _alertTimer = new Timer();
        private readonly Timer _clockTimer = new Timer { Interval = 30000 };
        private readonly NotifyIcon _tray = new NotifyIcon();
        private string _current;

        public MainForm()
        {
            Instance = this;
            Text = "Product Expiry Alert Management System";
            Icon = AppIcon.Get();
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1150, 700);
            Font = Theme.Body;
            BackColor = Theme.Background;

            BuildSidebar();
            var top = BuildTopBar();

            Controls.Add(_content);
            Controls.Add(top);
            Controls.Add(_sidebar);

            // System tray icon - used for pop-up notifications
            _tray.Icon = AppIcon.Get();
            _tray.Text = "Product Expiry Alerts";
            _tray.Visible = true;
            _tray.BalloonTipClicked += (s, e) => { Activate(); Navigate("alerts"); };
            _tray.DoubleClick += (s, e) => { Activate(); Navigate("alerts"); };

            // Periodic alert check
            _alertTimer.Interval = Math.Max(1, SettingsService.CheckIntervalMins) * 60 * 1000;
            _alertTimer.Tick += (s, e) => RunAlertCheck(showNotification: true);
            _alertTimer.Start();

            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();

            Shown += OnFirstShown;
            FormClosed += (s, e) =>
            {
                _tray.Visible = false;
                _tray.Dispose();
                ActivityLogService.Log("Logout", "Application closed");
            };
        }

        private void OnFirstShown(object sender, EventArgs e)
        {
            RunAlertCheck(showNotification: false);
            Navigate("dashboard");

            // Pop-up summary of urgent products when the program opens
            if (SettingsService.ShowStartupAlert && AlertService.CountUnacknowledged() > 0)
                using (var popup = new AlertPopupForm())
                    if (popup.ShowDialog(this) == DialogResult.OK)
                        Navigate("alerts");
        }

        /// <summary>Runs the alert engine, updates the bell counter and (optionally) shows a Windows notification.</summary>
        public void RunAlertCheck(bool showNotification)
        {
            try
            {
                int created = AlertService.CheckAndGenerateAlerts();
                UpdateBell();
                if (showNotification && created > 0)
                {
                    _tray.BalloonTipTitle = "Product Expiry Alert";
                    _tray.BalloonTipText = $"{created} new product expiry alert(s). Click to view.";
                    _tray.BalloonTipIcon = ToolTipIcon.Warning;
                    _tray.ShowBalloonTip(8000);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public void ApplyTimerInterval() =>
            _alertTimer.Interval = Math.Max(1, SettingsService.CheckIntervalMins) * 60 * 1000;

        public void UpdateBell()
        {
            int n = AlertService.CountUnacknowledged();
            _bell.Text = n == 0 ? "\u2714  No pending alerts" : $"\u26A0  {n} pending alert{(n == 1 ? "" : "s")}";
            _bell.BackColor = n == 0 ? Theme.SafeColor : Theme.ExpiredColor;
            if (_menu.TryGetValue("alerts", out var b)) b.Text = n == 0 ? "   ⚠   Alerts" : $"   ⚠   Alerts  ({n})";
        }

        private void UpdateClock() => _clock.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy   hh:mm tt");

        // ---------------------------------------------------------------- navigation

        /// <summary>Opens a screen in the content area.</summary>
        public void Navigate(string key)
        {
            Control view;
            switch (key)
            {
                case "dashboard": view = new DashboardView(); break;
                case "products": view = new ProductsView(); break;
                case "alerts": view = new AlertsView(); break;
                case "reports": view = new ReportsView(); break;
                case "lookups": view = new CategoriesSuppliersView(); break;
                case "users": view = new UsersView(); break;
                case "activity": view = new ActivityLogView(); break;
                case "settings": view = new SettingsView(); break;
                case "help": view = new HelpView(); break;
                default: return;
            }

            _current = key;
            foreach (var kv in _menu)
                kv.Value.BackColor = kv.Key == key ? Theme.SidebarActive : Theme.Sidebar;

            _content.SuspendLayout();
            foreach (Control c in _content.Controls) c.Dispose();
            _content.Controls.Clear();
            view.Dock = DockStyle.Fill;
            _content.Controls.Add(view);
            _content.ResumeLayout();
            UpdateBell();
        }

        public void ProductsChanged()
        {
            RunAlertCheck(showNotification: false);
        }

        // ---------------------------------------------------------------- layout builders

        private void BuildSidebar()
        {
            var brand = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.Sidebar };
            var logo = new PictureBox { Image = AppIcon.Bitmap(44), SizeMode = PictureBoxSizeMode.AutoSize, Location = new Point(18, 24) };
            var name = Theme.MakeLabel("EXPIRY ALERT\nMANAGEMENT", new Font("Segoe UI Semibold", 11F), Color.White);
            name.Location = new Point(70, 26);
            brand.Controls.Add(logo);
            brand.Controls.Add(name);

            var menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Theme.Sidebar,
                Padding = new Padding(0, 10, 0, 0)
            };

            AddMenu(menu, "dashboard", "⌂", "Dashboard");
            AddMenu(menu, "products", "▦", "Products");
            AddMenu(menu, "alerts", "⚠", "Alerts");
            AddMenu(menu, "reports", "☰", "Reports");
            AddMenu(menu, "lookups", "≡", "Categories && Suppliers");
            if (Session.CurrentUser.IsAdmin)
            {
                AddMenu(menu, "users", "☺", "Users");
                AddMenu(menu, "activity", "⌚", "Activity Log");
            }
            AddMenu(menu, "settings", "⚙", "Settings");
            AddMenu(menu, "help", "?", "Help && About");

            var logout = MenuButton("   ↩   Logout");
            logout.Dock = DockStyle.Bottom;
            logout.Click += (s, e) =>
            {
                if (!Theme.Confirm("Do you want to log out?")) return;
                ActivityLogService.Log("Logout", Session.CurrentUser.FullName + " logged out");
                _tray.Visible = false;
                Application.Restart();
                Environment.Exit(0);
            };

            _sidebar.Controls.Add(menu);
            _sidebar.Controls.Add(logout);
            _sidebar.Controls.Add(brand);
        }

        private void AddMenu(FlowLayoutPanel menu, string key, string icon, string text)
        {
            var b = MenuButton($"   {icon}   {text}");
            b.Click += (s, e) => Navigate(key);
            _menu[key] = b;
            menu.Controls.Add(b);
        }

        private static Button MenuButton(string text)
        {
            var b = new FlatButton
            {
                Text = text,
                Width = 230,
                Height = 46,
                Margin = new Padding(0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Theme.Sidebar,
                Font = new Font("Segoe UI", 10.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            return b;
        }

        private Panel BuildTopBar()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.Surface };
            top.Paint += (s, e) => { using (var p = new Pen(Theme.Border)) e.Graphics.DrawLine(p, 0, top.Height - 1, top.Width, top.Height - 1); };

            var user = Theme.MakeLabel($"Logged in as:  {Session.CurrentUser.FullName}   ({Session.CurrentUser.Role})", Theme.BodyBold);
            user.Location = new Point(28, 17);

            _clock.AutoSize = true;
            _clock.Font = Theme.Body;
            _clock.ForeColor = Theme.MutedText;
            _clock.Location = new Point(360, 18);
            UpdateClock();

            _bell.AutoSize = false;
            _bell.Size = new Size(210, 34);
            _bell.TextAlign = ContentAlignment.MiddleCenter;
            _bell.ForeColor = Color.White;
            _bell.Font = Theme.BodyBold;
            _bell.Cursor = Cursors.Hand;
            _bell.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _bell.Click += (s, e) => Navigate("alerts");
            top.Resize += (s, e) => _bell.Location = new Point(top.Width - _bell.Width - 24, 11);

            top.Controls.AddRange(new Control[] { user, _clock, _bell });
            return top;
        }
    }
}
