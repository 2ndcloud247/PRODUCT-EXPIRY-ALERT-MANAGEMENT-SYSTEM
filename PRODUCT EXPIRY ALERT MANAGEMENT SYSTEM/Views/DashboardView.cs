using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ExpiryAlertSystem.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>
    /// DASHBOARD - a quick overview of the stock.
    ///   * Summary cards: how many products are Expired / Critical / Expiring Soon / Safe.
    ///     Click a card to see those products.
    ///   * "Needs attention" table: products that will expire next.
    ///   * Doughnut chart: share of products in each status, plus the money value at risk.
    /// </summary>
    public class DashboardView : UserControl
    {
        public DashboardView()
        {
            BackColor = Theme.Background;
            var products = ProductService.GetAll(includeDisposed: false);
            var summary = AlertService.GetSummary(products);

            var header = Theme.MakeHeader("Dashboard",
                $"Overview of all products in stock as at {DateTime.Today:dddd, dd MMMM yyyy}. Click a coloured card to see the products behind the number.");

            // ---------- summary cards
            var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 130, ColumnCount = 5, RowCount = 1, BackColor = Theme.Background, Margin = new Padding(0) };
            for (int i = 0; i < 5; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            cards.Controls.Add(Card("Total Products", products.Count, Theme.Primary, "Active items being monitored", null), 0, 0);
            cards.Controls.Add(Card("Expired", summary[ExpiryStatus.Expired], Theme.ExpiredColor, "Remove from shelf now", ExpiryStatus.Expired), 1, 0);
            cards.Controls.Add(Card("Critical", summary[ExpiryStatus.Critical], Theme.CriticalColor, $"Expire within {SettingsService.CriticalDays} days", ExpiryStatus.Critical), 2, 0);
            cards.Controls.Add(Card("Expiring Soon", summary[ExpiryStatus.ExpiringSoon], Theme.SoonColor, $"Expire within {SettingsService.WarningDays} days", ExpiryStatus.ExpiringSoon), 3, 0);
            cards.Controls.Add(Card("Safe", summary[ExpiryStatus.Safe], Theme.SafeColor, $"More than {SettingsService.WarningDays} days left", ExpiryStatus.Safe), 4, 0);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Theme.Background };

            // ---------- bottom area: table (left) + chart (right)
            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

            bottom.Controls.Add(BuildUrgentTable(products), 0, 0);
            bottom.Controls.Add(BuildChart(summary, products), 1, 0);

            Controls.Add(bottom);
            Controls.Add(spacer);
            Controls.Add(cards);
            Controls.Add(header);
        }

        private Control Card(string title, int value, Color color, string hint, ExpiryStatus? filter)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Margin = new Padding(0, 0, 12, 0), Cursor = Cursors.Hand };
            card.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(color)) e.Graphics.FillRectangle(b, 0, 0, 6, card.Height);
                using (var p = new Pen(Theme.Border)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            var t = Theme.MakeLabel(title.ToUpper(), Theme.BodyBold, Theme.MutedText);
            t.Location = new Point(20, 14);
            var n = Theme.MakeLabel(value.ToString(), Theme.CardNumber, color == Theme.SoonColor ? Theme.SoonTextColor : color);
            n.Location = new Point(16, 38);
            var h = Theme.MakeLabel(hint, Theme.Small, Theme.MutedText);
            h.Location = new Point(20, 92);
            card.Controls.AddRange(new Control[] { t, n, h });

            EventHandler open = (s, e) =>
            {
                ProductsView.InitialFilter = filter;
                MainForm.Instance.Navigate("products");
            };
            card.Click += open;
            foreach (Control c in card.Controls) { c.Click += open; c.Cursor = Cursors.Hand; }
            return card;
        }

        private Control BuildUrgentTable(List<Product> products)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), BackColor = Theme.Surface, Padding = new Padding(16) };
            panel.Paint += (s, e) => { using (var p = new Pen(Theme.Border)) e.Graphics.DrawRectangle(p, 0, 0, panel.Width - 1, panel.Height - 1); };

            var title = new Label { Text = "Needs attention - products expiring next", Font = Theme.Heading, Dock = DockStyle.Top, Height = 34, ForeColor = Theme.Text };

            var grid = new DataGridView();
            Theme.StyleGrid(grid);
            grid.Columns.Add(Theme.Col("Product", "Name", 150));
            grid.Columns.Add(Theme.Col("Batch", "Batch", 60));
            grid.Columns.Add(Theme.Col("Qty", "Qty", 40, null, DataGridViewContentAlignment.MiddleRight));
            grid.Columns.Add(Theme.Col("Expiry Date", "Expiry", 70));
            grid.Columns.Add(Theme.Col("Time Left", "Left", 85));
            grid.Columns.Add(Theme.Col("Status", "Status", 65));

            int c = SettingsService.CriticalDays, w = SettingsService.WarningDays;
            var urgent = products.Where(p => AlertService.GetStatus(p, c, w) != ExpiryStatus.Safe)
                                 .OrderBy(p => p.ExpiryDate).Take(25).ToList();
            foreach (var p in urgent)
            {
                var st = AlertService.GetStatus(p, c, w);
                int i = grid.Rows.Add(p.Name, p.BatchNumber, p.Quantity, p.ExpiryDate.ToString("dd MMM yyyy"), AlertService.DaysLeftText(p), AlertService.StatusText(st));
                grid.Rows[i].DefaultCellStyle.BackColor = Theme.StatusRowColor(st);
                grid.Rows[i].Cells["Status"].Style.ForeColor = Theme.StatusTextColor(st);
                grid.Rows[i].DefaultCellStyle.SelectionBackColor = Theme.StatusRowColor(st);
                grid.Rows[i].DefaultCellStyle.SelectionForeColor = Theme.Text;
                grid.Rows[i].Cells["Status"].Style.Font = Theme.BodyBold;
            }

            var empty = new Label
            {
                Text = "✔  Great! No product is expired or close to expiry.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.SafeColor,
                Font = Theme.Heading,
                Visible = urgent.Count == 0
            };

            panel.Controls.Add(urgent.Count == 0 ? (Control)empty : grid);
            panel.Controls.Add(title);
            return panel;
        }

        private Control BuildChart(Dictionary<ExpiryStatus, int> summary, List<Product> products)
        {
            int c = SettingsService.CriticalDays, w = SettingsService.WarningDays;
            double atRisk = products.Where(p => { var s = AlertService.GetStatus(p, c, w); return s == ExpiryStatus.Expired || s == ExpiryStatus.Critical; })
                                    .Sum(p => p.TotalValue);
            double lost = products.Where(p => AlertService.GetStatus(p, c, w) == ExpiryStatus.Expired).Sum(p => p.TotalValue);

            var slices = new[]
            {
                (Name: "Expired", Value: summary[ExpiryStatus.Expired], Color: Theme.ExpiredColor),
                (Name: "Critical", Value: summary[ExpiryStatus.Critical], Color: Theme.CriticalColor),
                (Name: "Expiring Soon", Value: summary[ExpiryStatus.ExpiringSoon], Color: Theme.SoonColor),
                (Name: "Safe", Value: summary[ExpiryStatus.Safe], Color: Theme.SafeColor)
            };
            int total = slices.Sum(s => s.Value);

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Margin = new Padding(0) };
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Theme.Border)) g.DrawRectangle(p, 0, 0, panel.Width - 1, panel.Height - 1);
                using (var tb = new SolidBrush(Theme.Text)) g.DrawString("Stock status breakdown", Theme.Heading, tb, 16, 16);

                int size = Math.Max(80, Math.Min(panel.Width - 60, panel.Height - 250));
                size = Math.Max(80, Math.Min(size, Math.Min(230, panel.Height - 290)));
                var rect = new Rectangle((panel.Width - size) / 2, 56, size, size);

                float start = -90;
                if (total == 0)
                {
                    using (var b = new SolidBrush(Theme.Border)) g.FillEllipse(b, rect);
                }
                else
                {
                    foreach (var sl in slices)
                    {
                        if (sl.Value == 0) continue;
                        float sweep = 360f * sl.Value / total;
                        using (var b = new SolidBrush(sl.Color)) g.FillPie(b, rect, start, sweep);
                        start += sweep;
                    }
                }
                // hole in the middle -> doughnut
                int hole = (int)(size * 0.58);
                var holeRect = new Rectangle(rect.X + (size - hole) / 2, rect.Y + (size - hole) / 2, hole, hole);
                using (var b = new SolidBrush(Theme.Surface)) g.FillEllipse(b, holeRect);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var b = new SolidBrush(Theme.Text)) g.DrawString(total.ToString(), Theme.CardNumber, b, holeRect, sf);

                // legend
                int y = rect.Bottom + 18;
                foreach (var sl in slices)
                {
                    using (var b = new SolidBrush(sl.Color)) g.FillRectangle(b, 24, y + 4, 14, 14);
                    string pct = total == 0 ? "0%" : (100.0 * sl.Value / total).ToString("0") + "%";
                    using (var b = new SolidBrush(Theme.Text)) g.DrawString($"{sl.Name}", Theme.Body, b, 46, y);
                    using (var b = new SolidBrush(Theme.MutedText)) g.DrawString($"{sl.Value}  ({pct})", Theme.Body, b, panel.Width - 110, y);
                    y += 26;
                }

                y += 8;
                using (var p = new Pen(Theme.Border)) g.DrawLine(p, 20, y, panel.Width - 20, y);
                y += 10;
                using (var b = new SolidBrush(Theme.MutedText)) g.DrawString("Value of expired stock (loss):", Theme.Small, b, 22, y);
                using (var b = new SolidBrush(Theme.ExpiredColor)) g.DrawString(Theme.Money(lost), Theme.BodyBold, b, 22, y + 18);
                using (var b = new SolidBrush(Theme.MutedText)) g.DrawString("Value at risk (expired + critical):", Theme.Small, b, 22, y + 44);
                using (var b = new SolidBrush(Theme.CriticalColor)) g.DrawString(Theme.Money(atRisk), Theme.BodyBold, b, 22, y + 62);
            };
            panel.Resize += (s, e) => panel.Invalidate();
            return panel;
        }
    }
}
