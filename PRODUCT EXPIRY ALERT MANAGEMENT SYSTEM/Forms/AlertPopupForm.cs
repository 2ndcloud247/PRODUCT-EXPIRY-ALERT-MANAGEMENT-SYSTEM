using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Forms
{
    /// <summary>
    /// Pop-up shown when the program opens if there are pending (unacknowledged) alerts.
    /// It lists the urgent products so the user sees the problem immediately.
    /// </summary>
    public class AlertPopupForm : Form
    {
        public AlertPopupForm()
        {
            var alerts = AlertService.GetAlerts(includeAcknowledged: false);
            int expired = alerts.Count(a => a.AlertType == "Expired");
            int critical = alerts.Count(a => a.AlertType == "Critical");
            int soon = alerts.Count - expired - critical;

            Text = "Expiry Alerts";
            Size = new Size(720, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.Surface;
            Font = Theme.Body;
            Icon = AppIcon.Get();

            var banner = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.ExpiredColor, Padding = new Padding(20, 14, 20, 10) };
            var t = Theme.MakeLabel("⚠  Attention: products need your action", new Font("Segoe UI Semibold", 15F), Color.White);
            t.Location = new Point(20, 14);
            var s = Theme.MakeLabel($"{expired} expired   •   {critical} critical (expiring within {SettingsService.CriticalDays} days)   •   {soon} expiring soon",
                Theme.Subtitle, Color.White);
            s.Location = new Point(24, 54);
            banner.Controls.Add(t);
            banner.Controls.Add(s);

            var grid = new DataGridView();
            Theme.StyleGrid(grid);
            grid.Columns.Add(Theme.Col("Status", "Type", 60));
            grid.Columns.Add(Theme.Col("Product", "Product", 130));
            grid.Columns.Add(Theme.Col("Expiry Date", "Expiry", 70));
            foreach (var a in alerts.Take(50))
            {
                int i = grid.Rows.Add(a.AlertType, a.ProductName + (a.BatchNumber == "" ? "" : $"  (Batch {a.BatchNumber})"), a.ExpiryDate.ToString("dd MMM yyyy"));
                grid.Rows[i].Cells[0].Style.ForeColor = Theme.StatusColor(a.AlertType);
                grid.Rows[i].Cells[0].Style.Font = Theme.BodyBold;
            }
            var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 14, 20, 6) };
            gridHost.Controls.Add(grid);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 62, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 12, 16, 12) };
            var view = Theme.MakeButton("View All Alerts", Theme.Primary, 160);
            view.DialogResult = DialogResult.OK;
            var later = Theme.MakeButton("Remind Me Later", Theme.Secondary, 160);
            later.DialogResult = DialogResult.Cancel;
            bottom.Controls.Add(view);
            bottom.Controls.Add(later);

            Controls.Add(gridHost);
            Controls.Add(bottom);
            Controls.Add(banner);
            AcceptButton = view;
            CancelButton = later;
        }
    }
}
