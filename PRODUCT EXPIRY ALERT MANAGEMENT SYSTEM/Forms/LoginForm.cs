using System;
using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Forms
{
    /// <summary>
    /// Login screen. Only registered users can enter the system.
    /// Default account on first run: username "admin", password "admin123"
    /// (change it from Settings after logging in).
    /// </summary>
    public class LoginForm : Form
    {
        private readonly TextBox _username = new TextBox();
        private readonly TextBox _password = new TextBox();
        private readonly Label _error = new Label();
        private int _attempts;

        public LoginForm()
        {
            Text = "Login - Product Expiry Alert Management System";
            Size = new Size(860, 520);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Theme.Surface;
            Font = Theme.Body;
            Icon = AppIcon.Get();

            // ---------- left: branding panel
            var left = new Panel { Dock = DockStyle.Left, Width = 390, BackColor = Theme.Sidebar };
            var logo = new PictureBox { Image = AppIcon.Bitmap(72), SizeMode = PictureBoxSizeMode.AutoSize, Location = new Point(40, 50), BackColor = Color.Transparent };
            var title = Theme.MakeLabel("Product Expiry\nAlert Management\nSystem", new Font("Segoe UI Semibold", 20F), Color.White);
            title.Location = new Point(36, 135);
            var desc = Theme.MakeLabel("Track product expiry dates, get early\nwarnings and reduce waste and losses\nfrom expired goods.",
                Theme.Subtitle, Color.FromArgb(190, 205, 220));
            desc.Location = new Point(40, 260);
            var credit = Theme.MakeLabel("Iderima Lawrentta Onosedeba\nIAUE/2025/PGD/0029  •  PGD Information Technology\nIgnatius Ajuru University of Education",
                Theme.Small, Color.FromArgb(150, 170, 190));
            credit.Location = new Point(40, 395);
            left.Controls.AddRange(new Control[] { logo, title, desc, credit });

            // ---------- right: login form
            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(50, 60, 50, 30) };
            var welcome = Theme.MakeLabel("Welcome back", Theme.Title);
            welcome.Location = new Point(50, 55);
            var hint = Theme.MakeLabel("Sign in to continue", Theme.Subtitle, Theme.MutedText);
            hint.Location = new Point(52, 95);

            var lu = Theme.MakeLabel("Username", Theme.BodyBold);
            lu.Location = new Point(52, 145);
            _username.SetBounds(52, 170, 350, 30);
            _username.Font = new Font("Segoe UI", 12F);

            var lp = Theme.MakeLabel("Password", Theme.BodyBold);
            lp.Location = new Point(52, 215);
            _password.SetBounds(52, 240, 350, 30);
            _password.Font = new Font("Segoe UI", 12F);
            _password.UseSystemPasswordChar = true;

            var show = new CheckBox { Text = "Show password", Location = new Point(52, 278), AutoSize = true, Font = Theme.Small };
            show.CheckedChanged += (s, e) => _password.UseSystemPasswordChar = !show.Checked;

            _error.SetBounds(52, 305, 350, 22);
            _error.ForeColor = Theme.Danger;
            _error.Font = Theme.Small;

            var login = Theme.MakeButton("LOGIN", Theme.Primary, 350);
            login.Location = new Point(52, 332);
            login.Height = 42;
            login.Click += (s, e) => DoLogin();

            var defaults = Theme.MakeLabel($"First time? Use  {UserService.DefaultAdminUser} / {UserService.DefaultAdminPassword}",
                Theme.Small, Theme.MutedText);
            defaults.Location = new Point(52, 390);

            right.Controls.AddRange(new Control[] { welcome, hint, lu, _username, lp, _password, show, _error, login, defaults });

            Controls.Add(right);
            Controls.Add(left);

            AcceptButton = login;
            Shown += (s, e) => _username.Focus();
        }

        private void DoLogin()
        {
            _error.Text = "";
            if (string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrEmpty(_password.Text))
            {
                _error.Text = "Please enter both username and password.";
                return;
            }

            var user = UserService.Login(_username.Text, _password.Text);
            if (user == null)
            {
                _attempts++;
                _error.Text = "Wrong username or password, or the account is disabled.";
                _password.SelectAll();
                _password.Focus();
                if (_attempts >= 5)
                {
                    Theme.Warn("Too many failed attempts. The program will close.", "Login");
                    DialogResult = DialogResult.Cancel;
                }
                return;
            }

            Session.CurrentUser = user;
            ActivityLogService.Log("Login", $"{user.FullName} ({user.Role}) logged in");
            DialogResult = DialogResult.OK;
        }
    }
}
