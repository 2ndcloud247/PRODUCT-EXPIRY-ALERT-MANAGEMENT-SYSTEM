using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>
    /// SETTINGS
    ///   * Alert rules (Admin only): how many days before expiry a product becomes "Critical" / "Expiring Soon",
    ///     how often to check, and whether to show the pop-up when the program opens.
    ///   * Change my password (everyone).
    ///   * Backup the database (copies the .db file to a location you choose).
    /// </summary>
    public class SettingsView : UserControl
    {
        public SettingsView()
        {
            BackColor = Theme.Background;
            AutoScroll = true;
            var header = Theme.MakeHeader("Settings", "Adjust how the alert system behaves, change your password and back up your data.");

            var grid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, Height = 600, BackColor = Theme.Background };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.Controls.Add(AlertRulesCard(), 0, 0);
            grid.Controls.Add(PasswordAndBackupCard(), 1, 0);

            Controls.Add(grid);
            Controls.Add(header);
        }

        private Control AlertRulesCard()
        {
            bool admin = Session.CurrentUser.IsAdmin;
            var flow = NewFlow("Alert rules");

            var business = new TextBox { Width = 320, Text = SettingsService.BusinessName, Font = Theme.Body };
            var warning = new NumericUpDown { Width = 100, Minimum = 1, Maximum = 365, Value = SettingsService.WarningDays, Font = Theme.Body };
            var critical = new NumericUpDown { Width = 100, Minimum = 0, Maximum = 180, Value = SettingsService.CriticalDays, Font = Theme.Body };
            var interval = new NumericUpDown { Width = 100, Minimum = 1, Maximum = 1440, Value = SettingsService.CheckIntervalMins, Font = Theme.Body };
            var popup = new CheckBox { Text = "Show alert pop-up when the program opens", AutoSize = true, Checked = SettingsService.ShowStartupAlert, Font = Theme.Body };

            AddField(flow, "Business / store name (printed on reports)", business, null);
            AddField(flow, "\"Expiring Soon\" warning (days before expiry)", warning, "Products expiring within this many days are shown in YELLOW.");
            AddField(flow, "\"Critical\" warning (days before expiry)", critical, "Products expiring within this many days are shown in ORANGE. Must be smaller than the warning days.");
            AddField(flow, "Check for expiring products every (minutes)", interval, "The alert engine runs automatically in the background.");
            popup.Margin = new Padding(0, 10, 0, 0);
            flow.Controls.Add(popup);

            var save = Theme.MakeButton("✔  Save Settings", Theme.Primary, 170);
            save.Margin = new Padding(0, 20, 0, 0);
            save.Click += (s, e) =>
            {
                if (critical.Value >= warning.Value) { Theme.Warn("The Critical days must be smaller than the Expiring Soon days."); return; }
                SettingsService.BusinessName = business.Text.Trim() == "" ? "My Store" : business.Text.Trim();
                SettingsService.WarningDays = (int)warning.Value;
                SettingsService.CriticalDays = (int)critical.Value;
                SettingsService.CheckIntervalMins = (int)interval.Value;
                SettingsService.ShowStartupAlert = popup.Checked;
                ActivityLogService.Log("Settings Changed", $"Warning={warning.Value}d, Critical={critical.Value}d, Interval={interval.Value}min");
                MainForm.Instance.ApplyTimerInterval();
                MainForm.Instance.RunAlertCheck(showNotification: false);
                Theme.Info("Settings saved. The new rules apply immediately.");
            };
            flow.Controls.Add(save);

            if (!admin)
            {
                foreach (Control c in flow.Controls) if (!(c is Label)) c.Enabled = false;
                flow.Controls.Add(new Label { Text = "Only an Admin can change the alert rules.", AutoSize = true, ForeColor = Theme.Danger, Margin = new Padding(0, 10, 0, 0) });
            }

            return Wrap(flow, new Padding(0, 0, 12, 0));
        }

        private Control PasswordAndBackupCard()
        {
            var flow = NewFlow("Change my password");
            var current = new TextBox { Width = 320, UseSystemPasswordChar = true, Font = Theme.Body };
            var nw = new TextBox { Width = 320, UseSystemPasswordChar = true, Font = Theme.Body };
            var confirm = new TextBox { Width = 320, UseSystemPasswordChar = true, Font = Theme.Body };
            AddField(flow, "Current password", current, null);
            AddField(flow, "New password (at least 6 characters)", nw, null);
            AddField(flow, "Confirm new password", confirm, null);

            var change = Theme.MakeButton("Change Password", Theme.Primary, 170);
            change.Margin = new Padding(0, 16, 0, 0);
            change.Click += (s, e) =>
            {
                if (!UserService.CheckPassword(Session.CurrentUser.UserId, current.Text)) { Theme.Warn("The current password is not correct."); return; }
                if (nw.Text.Length < 6) { Theme.Warn("The new password must be at least 6 characters."); return; }
                if (nw.Text != confirm.Text) { Theme.Warn("The new passwords do not match."); return; }
                UserService.SetPassword(Session.CurrentUser.UserId, nw.Text);
                ActivityLogService.Log("Change Password", Session.CurrentUser.Username);
                current.Text = nw.Text = confirm.Text = "";
                Theme.Info("Your password has been changed.");
            };
            flow.Controls.Add(change);

            flow.Controls.Add(new Label { Text = "Backup", Font = Theme.Heading, AutoSize = true, Margin = new Padding(0, 34, 0, 6) });
            flow.Controls.Add(new Label
            {
                Text = "All data is stored in one SQLite file:\n" + Database.FilePath + "\nMake a copy regularly to avoid losing data.",
                AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = Theme.MutedText, Font = Theme.Small
            });
            var backup = Theme.MakeButton("⇩  Backup Database", Theme.Sidebar, 190);
            backup.Margin = new Padding(0, 12, 0, 0);
            backup.Click += (s, e) => Backup();
            flow.Controls.Add(backup);

            return Wrap(flow, new Padding(0));
        }

        private static void Backup()
        {
            using (var dlg = new SaveFileDialog { Filter = "SQLite database|*.db", FileName = "expiry_alert_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".db" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    // SQLite's built-in online backup makes a safe copy even while the program is using the file.
                    using (var src = Database.Open())
                    using (var dst = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + dlg.FileName))
                    {
                        dst.Open();
                        src.BackupDatabase(dst);
                    }
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    ActivityLogService.Log("Backup", Path.GetFileName(dlg.FileName));
                    Theme.Info("Backup saved to:\n" + dlg.FileName);
                }
                catch (Exception ex) { Theme.Error(ex, "create the backup"); }
            }
        }

        // ---------------------------------------------------------------- helpers

        private static FlowLayoutPanel NewFlow(string title)
        {
            var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Surface };
            flow.Controls.Add(new Label { Text = title, Font = Theme.Heading, AutoSize = true, Margin = new Padding(0, 0, 0, 8) });
            return flow;
        }

        private static void AddField(FlowLayoutPanel flow, string label, Control input, string hint)
        {
            flow.Controls.Add(new Label { Text = label, Font = Theme.BodyBold, AutoSize = true, Margin = new Padding(0, 10, 0, 2) });
            flow.Controls.Add(input);
            if (hint != null)
                flow.Controls.Add(new Label { Text = hint, Font = Theme.Small, ForeColor = Theme.MutedText, AutoSize = true, MaximumSize = new Size(440, 0) });
        }

        private static Control Wrap(Control inner, Padding margin)
        {
            var card = Theme.MakeCard(inner, new Padding(20));
            card.Dock = DockStyle.Fill;
            card.Margin = margin;
            return card;
        }
    }
}
