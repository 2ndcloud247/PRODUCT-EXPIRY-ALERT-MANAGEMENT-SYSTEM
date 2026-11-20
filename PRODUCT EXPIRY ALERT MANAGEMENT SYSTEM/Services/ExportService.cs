using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ExpiryAlertSystem.Services
{
    /// <summary>Saves whatever is shown in a table (DataGridView) to a CSV file that opens in Microsoft Excel.</summary>
    public static class ExportService
    {
        public static void ExportGridToCsv(DataGridView grid, string suggestedName)
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = "CSV file (opens in Excel)|*.csv",
                FileName = suggestedName + ".csv",
                Title = "Export to Excel (CSV)"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
                var sb = new StringBuilder();
                sb.AppendLine(string.Join(",", cols.Select(c => Escape(c.HeaderText))));
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    sb.AppendLine(string.Join(",", cols.Select(c => Escape(row.Cells[c.Index].FormattedValue?.ToString()))));
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                ActivityLogService.Log("Export", Path.GetFileName(dlg.FileName));

                if (MessageBox.Show("Export complete.\n\nDo you want to open the file now?", "Export",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(dlg.FileName);
            }
        }

        private static string Escape(string s)
        {
            s = s ?? "";
            return s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
