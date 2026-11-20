using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExpiryAlertSystem.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>
    /// ALERTS - list of warnings produced by the alert engine.
    ///   * Pending alerts are those nobody has acknowledged yet.
    ///   * "Acknowledge" means: I have seen this alert and taken action (e.g. moved the item to the front,
    ///     discounted it, or returned it to the supplier).
    ///   * "Dispose Product" marks the product as removed from stock and closes its alerts.
    /// </summary>
    public class AlertsView : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly CheckBox _showAll = new CheckBox { Text = "Show history", AutoSize = true, Font = Theme.Body, Margin = new Padding(16, 10, 0, 0) };
        private readonly Label _info = new Label { Dock = DockStyle.Bottom, Height = 30, ForeColor = Theme.MutedText, TextAlign = ContentAlignment.MiddleLeft };

        public AlertsView()
        {
            BackColor = Theme.Background;

            var header = Theme.MakeHeader("Expiry Alerts",
                $"Checked every {SettingsService.CheckIntervalMins} min.  Expired = date passed  •  Critical = within {SettingsService.CriticalDays} days  •  " +
                $"Expiring Soon = within {SettingsService.WarningDays} days.  Acknowledge an alert after acting on it.");

            var bar = Theme.MakeToolbar();
            var ack = Theme.MakeButton("✔  Acknowledge Selected", Theme.Primary, 210);
            var ackAll = Theme.MakeButton("Acknowledge All", Theme.Secondary, 160);
            var dispose = Theme.MakeButton("✖  Dispose Product", Theme.CriticalColor, 170);
            var check = Theme.MakeButton("↻  Check Now", Theme.Sidebar, 130);
            var export = Theme.MakeButton("⇩  Export", Theme.Sidebar, 110);
            ack.Click += (s, e) => Acknowledge(SelectedIds());
            ackAll.Click += (s, e) =>
            {
                var ids = AllPendingIds();
                if (ids.Count == 0) { Theme.Info("There are no pending alerts."); return; }
                if (Theme.Confirm($"Acknowledge all {ids.Count} pending alert(s)?")) Acknowledge(ids);
            };
            dispose.Click += (s, e) => DisposeSelected();
            check.Click += (s, e) =>
            {
                int before = AlertService.CountUnacknowledged();
                MainForm.Instance.RunAlertCheck(showNotification: false);
                int created = AlertService.CountUnacknowledged() - before;
                LoadData();
                Theme.Info(created > 0 ? $"{created} new alert(s) found." : "Check complete. No new alerts.", "Alert check");
            };
            export.Click += (s, e) => ExportService.ExportGridToCsv(_grid, "Alerts_" + DateTime.Today.ToString("yyyyMMdd"));
            _showAll.CheckedChanged += (s, e) => LoadData();
            bar.Controls.AddRange(new Control[] { ack, ackAll, dispose, check, export, _showAll });

            Theme.StyleGrid(_grid);
            _grid.MultiSelect = true;
            _grid.Columns.Add(Theme.Col("Id", "Id", 10));
            _grid.Columns.Add(Theme.Col("PId", "ProductId", 10));
            _grid.Columns.Add(Theme.Col("Alert", "Type", 60));
            _grid.Columns.Add(Theme.Col("Product", "Product", 130));
            _grid.Columns.Add(Theme.Col("Batch", "Batch", 55));
            _grid.Columns.Add(Theme.Col("Expiry Date", "Expiry", 65, "dd MMM yyyy"));
            _grid.Columns.Add(Theme.Col("Details", "Message", 230));
            _grid.Columns.Add(Theme.Col("Raised On", "Raised", 80, "dd MMM yyyy HH:mm"));
            _grid.Columns.Add(Theme.Col("State", "State", 90));
            _grid.Columns["Id"].Visible = false;
            _grid.Columns["ProductId"].Visible = false;

            var card = Theme.MakeCard(_grid, new Padding(1));
            card.Dock = DockStyle.Fill;

            Controls.Add(card);
            Controls.Add(_info);
            Controls.Add(bar);
            Controls.Add(header);
            LoadData();
        }

        private void LoadData()
        {
            var alerts = AlertService.GetAlerts(_showAll.Checked);
            _grid.Rows.Clear();
            foreach (var a in alerts)
            {
                int i = _grid.Rows.Add(a.AlertId, a.ProductId, a.AlertType, a.ProductName, a.BatchNumber, a.ExpiryDate,
                    a.Message, a.AlertDate, a.IsAcknowledged ? "✔ " + a.AcknowledgedBy : "Pending");
                var row = _grid.Rows[i];
                var color = Theme.StatusColor(a.AlertType);
                row.Cells["Type"].Style.BackColor = color;
                row.Cells["Type"].Style.ForeColor = a.AlertType == "Expiring Soon" ? Theme.Text : Color.White;
                row.Cells["Type"].Style.Font = Theme.BodyBold;
                if (a.IsAcknowledged) row.DefaultCellStyle.ForeColor = Theme.MutedText;
                else row.Cells["State"].Style.ForeColor = Theme.Danger;
            }
            int pending = alerts.Count(a => !a.IsAcknowledged);
            _info.Text = pending == 0
                ? "✔  No pending alerts. All products are under control."
                : $"{pending} pending alert(s). Tip: hold Ctrl or Shift to select several rows.";
            MainForm.Instance?.UpdateBell();
        }

        private List<int> SelectedIds() =>
            _grid.SelectedRows.Cast<DataGridViewRow>()
                 .Where(r => (string)r.Cells["State"].Value == "Pending")
                 .Select(r => Convert.ToInt32(r.Cells["Id"].Value)).ToList();

        private List<int> AllPendingIds() =>
            _grid.Rows.Cast<DataGridViewRow>()
                 .Where(r => (string)r.Cells["State"].Value == "Pending")
                 .Select(r => Convert.ToInt32(r.Cells["Id"].Value)).ToList();

        private void Acknowledge(List<int> ids)
        {
            if (ids.Count == 0) { Theme.Info("Please select one or more PENDING alerts first."); return; }
            AlertService.Acknowledge(ids);
            ActivityLogService.Log("Acknowledge Alerts", $"{ids.Count} alert(s)");
            LoadData();
        }

        private void DisposeSelected()
        {
            if (_grid.CurrentRow == null) { Theme.Info("Please select an alert first."); return; }
            int productId = Convert.ToInt32(_grid.CurrentRow.Cells["ProductId"].Value);
            var p = ProductService.GetById(productId);
            if (p == null) return;
            if (p.Status == "Disposed") { Theme.Info("This product is already disposed."); return; }
            if (!Theme.Confirm($"Mark \"{p.Name}\" (Batch {p.BatchNumber}, Qty {p.Quantity}) as DISPOSED?\n\nAll its alerts will be closed.")) return;
            ProductService.MarkDisposed(productId);
            ActivityLogService.Log("Dispose Product", $"{p.Name} (Batch {p.BatchNumber}, Qty {p.Quantity})");
            LoadData();
        }
    }
}
