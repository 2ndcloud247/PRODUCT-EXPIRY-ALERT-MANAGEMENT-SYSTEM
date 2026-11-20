using System;
using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>ACTIVITY LOG (Admin only) - audit trail of the last 500 actions in the system.</summary>
    public class ActivityLogView : UserControl
    {
        public ActivityLogView()
        {
            BackColor = Theme.Background;
            var header = Theme.MakeHeader("Activity Log",
                "A record of who did what and when (logins, product changes, disposals, alerts acknowledged). Newest first.");

            var grid = new DataGridView();
            Theme.StyleGrid(grid);
            grid.AutoGenerateColumns = true;
            grid.DataSource = ActivityLogService.GetRecent();
            grid.DataBindingComplete += (s, e) =>
            {
                grid.Columns[0].FillWeight = 60;
                grid.Columns[1].FillWeight = 40;
                grid.Columns[2].FillWeight = 60;
                grid.Columns[3].FillWeight = 200;
            };

            var bar = Theme.MakeToolbar();
            var export = Theme.MakeButton("⇩  Export", Theme.Sidebar, 110);
            export.Click += (s, e) => ExportService.ExportGridToCsv(grid, "ActivityLog_" + DateTime.Today.ToString("yyyyMMdd"));
            bar.Controls.Add(export);

            var card = Theme.MakeCard(grid, new Padding(1));
            card.Dock = DockStyle.Fill;
            Controls.Add(card);
            Controls.Add(bar);
            Controls.Add(header);
        }
    }
}
