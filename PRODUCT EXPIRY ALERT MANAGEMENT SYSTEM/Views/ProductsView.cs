using System;
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
    /// PRODUCTS - register and manage every product (and batch) with its expiry date.
    ///   * Add / Edit / Delete products
    ///   * Search by name, batch number, barcode, supplier or location
    ///   * Filter by category and by expiry status
    ///   * Rows are coloured by status: red = expired, orange = critical, yellow = expiring soon, green = safe
    ///   * "Mark as Disposed" when an expired product has been removed from the shelf
    /// </summary>
    public class ProductsView : UserControl
    {
        /// <summary>Set by the dashboard cards so this screen opens already filtered.</summary>
        public static ExpiryStatus? InitialFilter;

        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _search = new TextBox { Width = 230, Font = Theme.Body };
        private readonly ComboBox _category = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Body };
        private readonly ComboBox _status = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Body };
        private readonly Label _count = new Label { Dock = DockStyle.Bottom, Height = 30, ForeColor = Theme.MutedText, TextAlign = ContentAlignment.MiddleLeft };

        public ProductsView()
        {
            BackColor = Theme.Background;

            var header = Theme.MakeHeader("Products",
                "Register products with their batch and expiry date. Double-click a row to edit it. Row colours show the expiry status.");

            // ---------- filters row
            var filters = Theme.MakeToolbar();
            filters.Controls.Add(FilterLabel("Search:"));
            filters.Controls.Add(_search);
            filters.Controls.Add(FilterLabel("Category:"));
            filters.Controls.Add(_category);
            filters.Controls.Add(FilterLabel("Status:"));
            filters.Controls.Add(_status);
            var clear = Theme.MakeButton("Clear", Theme.Secondary, 80);
            clear.Height = 30;
            clear.Click += (s, e) => { _search.Text = ""; _category.SelectedIndex = 0; _status.SelectedIndex = 0; };
            filters.Controls.Add(clear);
            filters.Controls.Add(Legend());
            foreach (Control c in filters.Controls) c.Margin = new Padding(0, 6, 8, 0);

            // ---------- action buttons
            var actions = Theme.MakeToolbar();
            var add = Theme.MakeButton("➕  Add Product", Theme.Primary, 150);
            var edit = Theme.MakeButton("✎  Edit", Theme.Secondary, 100);
            var dispose = Theme.MakeButton("Mark as Disposed", Theme.CriticalColor, 160);
            var delete = Theme.MakeButton("\u2716  Delete", Theme.Danger, 110);
            var export = Theme.MakeButton("⇩  Export", Theme.Sidebar, 110);
            add.Click += (s, e) => OpenEditor(null);
            edit.Click += (s, e) => { var p = Selected(); if (p != null) OpenEditor(p); };
            dispose.Click += (s, e) => DisposeSelected();
            delete.Click += (s, e) => DeleteSelected();
            export.Click += (s, e) => ExportService.ExportGridToCsv(_grid, "Products_" + DateTime.Today.ToString("yyyyMMdd"));
            actions.Controls.AddRange(new Control[] { add, edit, dispose, delete, export });

            // ---------- grid
            Theme.StyleGrid(_grid);
            _grid.Columns.Add(Theme.Col("ID", "Id", 30));
            _grid.Columns.Add(Theme.Col("Product Name", "Name", 170));
            _grid.Columns.Add(Theme.Col("Category", "Category", 100));
            _grid.Columns.Add(Theme.Col("Batch No.", "Batch", 70));
            _grid.Columns.Add(Theme.Col("Supplier", "Supplier", 110));
            _grid.Columns.Add(Theme.Col("Qty", "Qty", 45, null, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Unit Price (₦)", "Price", 70, "N2", DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Theme.Col("Expiry Date", "Expiry", 80, "dd MMM yyyy"));
            _grid.Columns.Add(Theme.Col("Time Left", "Left", 100));
            _grid.Columns.Add(Theme.Col("Status", "Status", 80));
            _grid.Columns.Add(Theme.Col("Location", "Location", 75));
            _grid.Columns["Id"].Visible = false;
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) { var p = Selected(); if (p != null) OpenEditor(p); } };
            _grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) DeleteSelected(); };

            var card = Theme.MakeCard(_grid, new Padding(1));
            card.Dock = DockStyle.Fill;

            Controls.Add(card);
            Controls.Add(_count);
            Controls.Add(actions);
            Controls.Add(filters);
            Controls.Add(header);

            // ---------- fill filter lists
            _category.Items.Add(new LookupItem { Id = 0, Name = "All categories" });
            foreach (var c in LookupService.GetCategories()) _category.Items.Add(c);
            _category.SelectedIndex = 0;

            _status.Items.AddRange(new object[] { "All", "Expired", "Critical", "Expiring Soon", "Safe", "Disposed" });
            _status.SelectedIndex = 0;
            if (InitialFilter.HasValue)
            {
                _status.SelectedItem = AlertService.StatusText(InitialFilter.Value);
                InitialFilter = null;
            }

            _search.TextChanged += (s, e) => LoadData();
            _category.SelectedIndexChanged += (s, e) => LoadData();
            _status.SelectedIndexChanged += (s, e) => LoadData();

            LoadData();
        }

        private static Label FilterLabel(string text)
        {
            var l = Theme.MakeLabel(text, Theme.BodyBold);
            l.Padding = new Padding(0, 4, 0, 0);
            return l;
        }

        private Control Legend()
        {
            var flow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(20, 8, 0, 0) };
            foreach (var (name, color) in new[] { ("Expired", Theme.ExpiredColor), ("Critical", Theme.CriticalColor), ("Soon", Theme.SoonColor), ("Safe", Theme.SafeColor) })
            {
                flow.Controls.Add(new Panel { BackColor = color, Size = new Size(14, 14), Margin = new Padding(6, 4, 4, 0) });
                flow.Controls.Add(new Label { Text = name, AutoSize = true, Font = Theme.Small, ForeColor = Theme.MutedText, Margin = new Padding(0, 3, 6, 0) });
            }
            return flow;
        }

        private void LoadData()
        {
            var cat = _category.SelectedItem as LookupItem;
            var products = ProductService.GetAll(_search.Text, cat == null || cat.Id == 0 ? (int?)null : cat.Id);
            int c = SettingsService.CriticalDays, w = SettingsService.WarningDays;
            string filter = _status.SelectedItem as string ?? "All";

            _grid.SuspendLayout();
            _grid.Rows.Clear();
            int shown = 0;
            foreach (var p in products)
            {
                var st = AlertService.GetStatus(p, c, w);
                var stText = AlertService.StatusText(st);
                if (filter != "All" && filter != stText) continue;

                int i = _grid.Rows.Add(p.ProductId, p.Name, p.CategoryName, p.BatchNumber, p.SupplierName, p.Quantity,
                    p.UnitPrice, p.ExpiryDate, st == ExpiryStatus.Disposed ? "-" : AlertService.DaysLeftText(p), stText, p.Location);
                var row = _grid.Rows[i];
                row.DefaultCellStyle.BackColor = Theme.StatusRowColor(st);
                row.Cells["Status"].Style.ForeColor = Theme.StatusTextColor(st);
                row.Cells["Status"].Style.Font = Theme.BodyBold;
                shown++;
            }
            _grid.ResumeLayout();
            _count.Text = $"Showing {shown} of {products.Count} product(s).   Tip: press Delete to remove the selected product, double-click to edit.";
        }

        private Product Selected()
        {
            if (_grid.CurrentRow == null)
            {
                Theme.Info("Please select a product in the table first.");
                return null;
            }
            return ProductService.GetById(Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value));
        }

        private void OpenEditor(Product p)
        {
            using (var f = new ProductEditorForm(p))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
            }
            MainForm.Instance.ProductsChanged();
            LoadData();
        }

        private void DisposeSelected()
        {
            var p = Selected();
            if (p == null) return;
            if (p.Status == "Disposed") { Theme.Info("This product is already marked as disposed."); return; }
            if (!Theme.Confirm($"Mark \"{p.Name}\" (Batch {p.BatchNumber}) as DISPOSED?\n\n" +
                               "Use this after the product has been removed from the shelf and destroyed or returned to the supplier. " +
                               "It will stay in the records but will no longer raise alerts.")) return;
            try
            {
                ProductService.MarkDisposed(p.ProductId);
                ActivityLogService.Log("Dispose Product", $"{p.Name} (Batch {p.BatchNumber}, Qty {p.Quantity})");
                MainForm.Instance.ProductsChanged();
                LoadData();
            }
            catch (Exception ex) { Theme.Error(ex, "dispose the product"); }
        }

        private void DeleteSelected()
        {
            var p = Selected();
            if (p == null) return;
            if (!Theme.Confirm($"Permanently DELETE \"{p.Name}\" (Batch {p.BatchNumber})?\n\nThis cannot be undone. " +
                               "Tip: use \"Mark as Disposed\" instead if you want to keep a record.", "Delete product")) return;
            try
            {
                ProductService.Delete(p.ProductId);
                ActivityLogService.Log("Delete Product", $"{p.Name} (Batch {p.BatchNumber})");
                MainForm.Instance.ProductsChanged();
                LoadData();
            }
            catch (Exception ex) { Theme.Error(ex, "delete the product"); }
        }
    }
}
