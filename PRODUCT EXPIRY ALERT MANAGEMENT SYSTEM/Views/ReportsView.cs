using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>
    /// REPORTS - choose a report, click "Generate", then Print it or Export it to Excel.
    ///   1. Expired Products            - everything whose expiry date has passed (still in stock)
    ///   2. Products Expiring in Period - products whose expiry date falls between two dates
    ///   3. Full Stock Status Report    - every active product with its status
    ///   4. Disposed Products (Losses)  - products written off, with the money value lost
    ///   5. Summary by Category         - counts and values per category
    /// </summary>
    public class ReportsView : UserControl
    {
        private readonly ComboBox _type = new ComboBox { Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Body };
        private readonly DateTimePicker _from = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMM yyyy" };
        private readonly DateTimePicker _to = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMM yyyy" };
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _summary = new Label { Dock = DockStyle.Bottom, Height = 36, Font = Theme.BodyBold, ForeColor = Theme.Sidebar, TextAlign = ContentAlignment.MiddleLeft };
        private string _title = "";

        // printing state
        private int _printRow;
        private int _printPage;

        public ReportsView()
        {
            BackColor = Theme.Background;
            var header = Theme.MakeHeader("Reports",
                "Pick a report type, set the dates if needed, then click Generate. You can print the result or export it to Excel.");

            _type.Items.AddRange(new object[]
            {
                "1. Expired Products",
                "2. Products Expiring in Period",
                "3. Full Stock Status Report",
                "4. Disposed Products (Losses)",
                "5. Summary by Category"
            });
            _type.SelectedIndex = 0;
            _from.Value = DateTime.Today;
            _to.Value = DateTime.Today.AddDays(SettingsService.WarningDays);

            var bar = Theme.MakeToolbar();
            bar.Height = 54;
            var lblFrom = Theme.MakeLabel("From:", Theme.BodyBold);
            var lblTo = Theme.MakeLabel("To:", Theme.BodyBold);
            var gen = Theme.MakeButton("▶  Generate", Theme.Primary, 130);
            var print = Theme.MakeButton("⎙  Print", Theme.Sidebar, 100);
            var export = Theme.MakeButton("⇩  Export", Theme.Sidebar, 100);
            gen.Click += (s, e) => Generate();
            print.Click += (s, e) => Print();
            export.Click += (s, e) => ExportService.ExportGridToCsv(_grid, _title.Replace(" ", "_") + "_" + DateTime.Today.ToString("yyyyMMdd"));
            _type.SelectedIndexChanged += (s, e) => { UpdateDateInputs(); Generate(); };
            bar.Controls.AddRange(new Control[] { Theme.MakeLabel("Report:", Theme.BodyBold), _type, lblFrom, _from, lblTo, _to });
            foreach (Control c in bar.Controls) c.Margin = new Padding(0, c is Label ? 12 : 9, 8, 0);

            var buttons = Theme.MakeToolbar();
            buttons.Controls.AddRange(new Control[] { gen, print, export });

            Theme.StyleGrid(_grid);
            var card = Theme.MakeCard(_grid, new Padding(1));
            card.Dock = DockStyle.Fill;

            Controls.Add(card);
            Controls.Add(_summary);
            Controls.Add(buttons);
            Controls.Add(bar);
            Controls.Add(header);

            UpdateDateInputs();
            Generate();
        }

        private void UpdateDateInputs()
        {
            bool needsDates = _type.SelectedIndex == 1;
            _from.Enabled = _to.Enabled = needsDates;
        }

        private void Generate()
        {
            int c = SettingsService.CriticalDays, w = SettingsService.WarningDays;
            var all = ProductService.GetAll();
            _grid.Columns.Clear();
            _grid.Rows.Clear();
            _title = _type.SelectedItem.ToString().Substring(3);

            if (_type.SelectedIndex == 4)
            {
                BuildCategorySummary(all, c, w);
                return;
            }

            IEnumerable<Product> list;
            switch (_type.SelectedIndex)
            {
                case 0: list = all.Where(p => AlertService.GetStatus(p, c, w) == ExpiryStatus.Expired); break;
                case 1:
                    var from = _from.Value.Date; var to = _to.Value.Date;
                    if (to < from) { Theme.Warn("The 'To' date must be after the 'From' date."); return; }
                    _title += $" ({from:dd MMM yyyy} - {to:dd MMM yyyy})";
                    list = all.Where(p => p.Status == "Active" && p.ExpiryDate >= from && p.ExpiryDate <= to);
                    break;
                case 2: list = all.Where(p => p.Status == "Active"); break;
                default: list = all.Where(p => p.Status == "Disposed"); break;
            }
            var rows = list.OrderBy(p => p.ExpiryDate).ToList();

            _grid.Columns.Add(Theme.Col("S/N", "SN", 30));
            _grid.Columns.Add(Theme.Col("Product", "Name", 160));
            _grid.Columns.Add(Theme.Col("Category", "Category", 100));
            _grid.Columns.Add(Theme.Col("Batch", "Batch", 70));
            _grid.Columns.Add(Theme.Col("Supplier", "Supplier", 110));
            _grid.Columns.Add(Theme.Col("Qty", "Qty", 45, null, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Value (₦)", "Value", 80, "N2", DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Expiry Date", "Expiry", 80, "dd MMM yyyy"));
            _grid.Columns.Add(Theme.Col("Status", "Status", 90));

            int sn = 1;
            foreach (var p in rows)
            {
                var st = AlertService.GetStatus(p, c, w);
                int i = _grid.Rows.Add(sn++, p.Name, p.CategoryName, p.BatchNumber, p.SupplierName, p.Quantity, p.TotalValue, p.ExpiryDate,
                    st == ExpiryStatus.Disposed ? "Disposed" : AlertService.StatusText(st) + " (" + AlertService.DaysLeftText(p) + ")");
                _grid.Rows[i].DefaultCellStyle.BackColor = Theme.StatusRowColor(st);
            }

            double value = rows.Sum(p => p.TotalValue);
            _summary.Text = $"{_title}:   {rows.Count} product(s)   •   Total quantity: {rows.Sum(p => p.Quantity):N0}   •   Total value: {Theme.Money(value)}";
        }

        private void BuildCategorySummary(List<Product> all, int c, int w)
        {
            _grid.Columns.Add(Theme.Col("Category", "Category", 150));
            _grid.Columns.Add(Theme.Col("Active Items", "Active", 70, null, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Expired", "Expired", 60, null, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Critical", "Critical", 60, null, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Expiring Soon", "Soon", 70, null, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Safe", "Safe", 60, null, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Stock Value (₦)", "Value", 90, "N2", DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Expired Value (₦)", "Lost", 90, "N2", DataGridViewContentAlignment.MiddleRight));

            var active = all.Where(p => p.Status == "Active").ToList();
            foreach (var g in active.GroupBy(p => string.IsNullOrEmpty(p.CategoryName) ? "(No category)" : p.CategoryName).OrderBy(g => g.Key))
            {
                int Count(ExpiryStatus s) => g.Count(p => AlertService.GetStatus(p, c, w) == s);
                int i = _grid.Rows.Add(g.Key, g.Count(), Count(ExpiryStatus.Expired), Count(ExpiryStatus.Critical), Count(ExpiryStatus.ExpiringSoon),
                    Count(ExpiryStatus.Safe), g.Sum(p => p.TotalValue), g.Where(p => AlertService.GetStatus(p, c, w) == ExpiryStatus.Expired).Sum(p => p.TotalValue));
                if (Count(ExpiryStatus.Expired) > 0) _grid.Rows[i].Cells["Expired"].Style.ForeColor = Theme.ExpiredColor;
            }
            _summary.Text = $"Summary by Category:   {active.Count} active product(s)   •   Total stock value: {Theme.Money(active.Sum(p => p.TotalValue))}";
        }

        // ---------------------------------------------------------------- printing

        private void Print()
        {
            if (_grid.Rows.Count == 0) { Theme.Info("There is nothing to print. Generate a report with some rows first."); return; }
            var doc = new PrintDocument { DocumentName = _title };
            doc.DefaultPageSettings.Landscape = true;
            doc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
            doc.BeginPrint += (s, e) => { _printRow = 0; _printPage = 1; };
            doc.PrintPage += PrintPage;
            using (var preview = new PrintPreviewDialog { Document = doc, Width = 1100, Height = 800, Text = "Print Preview - " + _title })
                preview.ShowDialog(this);
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            var b = e.MarginBounds;
            float y = b.Top;
            using (var titleFont = new Font("Segoe UI Semibold", 15F))
            using (var font = new Font("Segoe UI", 8.5F))
            using (var bold = new Font("Segoe UI Semibold", 8.5F))
            {
                g.DrawString(SettingsService.BusinessName, bold, Brushes.Gray, b.Left, y);
                g.DrawString("Printed " + DateTime.Now.ToString("dd MMM yyyy HH:mm") + "  by " + Session.CurrentUser.FullName, font, Brushes.Gray, b.Right - 260, y);
                y += 18;
                g.DrawString("Product Expiry Alert Management System - " + _title, titleFont, Brushes.Black, b.Left, y);
                y += 36;

                var cols = _grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
                float totalWeight = cols.Sum(c => c.FillWeight);
                var widths = cols.Select(c => b.Width * c.FillWeight / totalWeight).ToList();
                float rowH = font.GetHeight(g) + 8;

                // header row
                float x = b.Left;
                g.FillRectangle(new SolidBrush(Theme.Sidebar), b.Left, y, b.Width, rowH);
                for (int i = 0; i < cols.Count; i++)
                {
                    g.DrawString(cols[i].HeaderText, bold, Brushes.White, new RectangleF(x + 3, y + 4, widths[i] - 6, rowH), new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                    x += widths[i];
                }
                y += rowH;

                while (_printRow < _grid.Rows.Count)
                {
                    if (y + rowH > b.Bottom - 20) { e.HasMorePages = true; break; }
                    var row = _grid.Rows[_printRow];
                    if (_printRow % 2 == 1) g.FillRectangle(new SolidBrush(Color.FromArgb(245, 247, 249)), b.Left, y, b.Width, rowH);
                    x = b.Left;
                    for (int i = 0; i < cols.Count; i++)
                    {
                        var text = row.Cells[cols[i].Index].FormattedValue?.ToString() ?? "";
                        g.DrawString(text, font, Brushes.Black, new RectangleF(x + 3, y + 4, widths[i] - 6, rowH), new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                        x += widths[i];
                    }
                    g.DrawLine(Pens.Gainsboro, b.Left, y + rowH, b.Right, y + rowH);
                    y += rowH;
                    _printRow++;
                }

                if (_printRow >= _grid.Rows.Count)
                {
                    e.HasMorePages = false;
                    g.DrawString(_summary.Text, bold, Brushes.Black, b.Left, y + 10);
                }
                g.DrawString("Page " + _printPage++, font, Brushes.Gray, b.Right - 60, b.Bottom);
            }
        }
    }
}
