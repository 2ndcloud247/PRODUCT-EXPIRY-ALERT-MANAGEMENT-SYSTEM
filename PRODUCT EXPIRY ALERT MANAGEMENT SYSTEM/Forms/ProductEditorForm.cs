using System;
using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Forms
{
    /// <summary>
    /// Add / Edit product window. Fields marked * are required.
    /// While you pick the expiry date, the box at the bottom shows what status the product will have.
    /// </summary>
    public class ProductEditorForm : Form
    {
        private readonly Product _product;

        private readonly TextBox _name = new TextBox();
        private readonly ComboBox _category = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _supplier = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _batch = new TextBox();
        private readonly TextBox _barcode = new TextBox();
        private readonly NumericUpDown _qty = new NumericUpDown { Maximum = 10000000, ThousandsSeparator = true };
        private readonly NumericUpDown _price = new NumericUpDown { Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true };
        private readonly DateTimePicker _mfg = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMMM yyyy", ShowCheckBox = true };
        private readonly DateTimePicker _exp = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMMM yyyy" };
        private readonly TextBox _location = new TextBox();
        private readonly ComboBox _status = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _notes = new TextBox { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
        private readonly Label _preview = new Label();

        public ProductEditorForm(Product product)
        {
            _product = product ?? new Product { ExpiryDate = DateTime.Today.AddMonths(6) };
            bool isNew = product == null;

            Text = isNew ? "Add New Product" : "Edit Product";
            Size = new Size(720, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.Surface;
            Font = Theme.Body;
            Icon = AppIcon.Get();

            var title = new Label
            {
                Text = isNew ? "Add New Product" : "Edit Product",
                Font = Theme.Title,
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(22, 12, 0, 0)
            };

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(22, 8, 22, 0), AutoScroll = true };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            int row = 0;
            AddRow(table, row++, "Product Name *", _name, span: true);
            AddPair(table, row++, "Category", _category, "Supplier", _supplier);
            AddPair(table, row++, "Batch Number", _batch, "Barcode", _barcode);
            AddPair(table, row++, "Quantity", _qty, "Unit Price (₦)", _price);
            AddPair(table, row++, "Manufacture Date", _mfg, "Expiry Date *", _exp);
            AddPair(table, row++, "Store Location", _location, "Status", _status);
            AddRow(table, row++, "Notes", _notes, span: true);

            _preview.Dock = DockStyle.Fill;
            _preview.Height = 54;
            _preview.Font = Theme.BodyBold;
            _preview.TextAlign = ContentAlignment.MiddleLeft;
            _preview.Padding = new Padding(12, 0, 0, 0);
            _preview.Margin = new Padding(0, 14, 0, 0);
            table.Controls.Add(_preview, 0, row);
            table.SetColumnSpan(_preview, 4);

            var help = new Label
            {
                UseMnemonic = false,
                Text = "Tip: tick the box beside Manufacture Date only if the date is known. Category and Supplier lists are managed under \"Categories & Suppliers\".",
                Dock = DockStyle.Fill,
                ForeColor = Theme.MutedText,
                Font = Theme.Small,
                Height = 40,
                Margin = new Padding(0, 8, 0, 0)
            };
            table.Controls.Add(help, 0, row + 1);
            table.SetColumnSpan(help, 4);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16, 12, 16, 12) };
            var save = Theme.MakeButton("✔  Save", Theme.Primary, 130);
            var cancel = Theme.MakeButton("Cancel", Theme.Secondary, 110);
            cancel.DialogResult = DialogResult.Cancel;
            save.Click += (s, e) => SaveProduct();
            bottom.Controls.Add(save);
            bottom.Controls.Add(cancel);

            Controls.Add(table);
            Controls.Add(bottom);
            Controls.Add(title);
            AcceptButton = save;
            CancelButton = cancel;

            LoadLists();
            LoadValues();
            _exp.ValueChanged += (s, e) => UpdatePreview();
            _status.SelectedIndexChanged += (s, e) => UpdatePreview();
            UpdatePreview();
        }

        private static void AddRow(TableLayoutPanel t, int row, string label, Control c, bool span)
        {
            t.Controls.Add(FieldLabel(label), 0, row);
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(0, 6, 12, 6);
            t.Controls.Add(c, 1, row);
            if (span) t.SetColumnSpan(c, 3);
        }

        private static void AddPair(TableLayoutPanel t, int row, string l1, Control c1, string l2, Control c2)
        {
            t.Controls.Add(FieldLabel(l1), 0, row);
            c1.Dock = DockStyle.Fill; c1.Margin = new Padding(0, 6, 12, 6);
            t.Controls.Add(c1, 1, row);
            t.Controls.Add(FieldLabel(l2), 2, row);
            c2.Dock = DockStyle.Fill; c2.Margin = new Padding(0, 6, 12, 6);
            t.Controls.Add(c2, 3, row);
        }

        private static Label FieldLabel(string text) => new Label
        {
            Text = text,
            Font = Theme.BodyBold,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = text.EndsWith("*") ? Theme.Sidebar : Theme.Text
        };

        private void LoadLists()
        {
            _category.Items.Add(new LookupItem { Id = 0, Name = "(none)" });
            foreach (var c in LookupService.GetCategories()) _category.Items.Add(c);
            _supplier.Items.Add(new LookupItem { Id = 0, Name = "(none)" });
            foreach (var s in LookupService.GetSuppliers()) _supplier.Items.Add(s);
            _status.Items.AddRange(new object[] { "Active", "Disposed" });
        }

        private void LoadValues()
        {
            var p = _product;
            _name.Text = p.Name;
            SelectLookup(_category, p.CategoryId);
            SelectLookup(_supplier, p.SupplierId);
            _batch.Text = p.BatchNumber;
            _barcode.Text = p.Barcode;
            _qty.Value = Math.Max(0, p.Quantity);
            _price.Value = (decimal)Math.Max(0, p.UnitPrice);
            _mfg.Checked = p.ManufactureDate.HasValue;
            if (p.ManufactureDate.HasValue) _mfg.Value = p.ManufactureDate.Value;
            _exp.Value = p.ExpiryDate;
            _location.Text = p.Location;
            _status.SelectedItem = p.Status ?? "Active";
            _notes.Text = p.Notes;
        }

        private static void SelectLookup(ComboBox box, int? id)
        {
            box.SelectedIndex = 0;
            if (!id.HasValue) return;
            for (int i = 0; i < box.Items.Count; i++)
                if (((LookupItem)box.Items[i]).Id == id.Value) { box.SelectedIndex = i; return; }
        }

        private void UpdatePreview()
        {
            var tmp = new Product { ExpiryDate = _exp.Value.Date, Status = _status.SelectedItem as string ?? "Active" };
            var st = AlertService.GetStatus(tmp);
            _preview.BackColor = Theme.StatusRowColor(st);
            _preview.ForeColor = Theme.StatusTextColor(st);
            _preview.Text = st == ExpiryStatus.Disposed
                ? "Status: DISPOSED - this product will not raise alerts."
                : $"Status preview: {AlertService.StatusText(st).ToUpper()}  -  {AlertService.DaysLeftText(tmp)}";
        }

        private void SaveProduct()
        {
            // ---- validation
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                Theme.Warn("Please enter the product name.");
                _name.Focus();
                return;
            }
            if (_mfg.Checked && _mfg.Value.Date >= _exp.Value.Date)
            {
                Theme.Warn("The expiry date must be AFTER the manufacture date.");
                _exp.Focus();
                return;
            }
            if (_mfg.Checked && _mfg.Value.Date > DateTime.Today)
            {
                Theme.Warn("The manufacture date cannot be in the future.");
                _mfg.Focus();
                return;
            }

            var cat = (LookupItem)_category.SelectedItem;
            var sup = (LookupItem)_supplier.SelectedItem;
            _product.Name = _name.Text.Trim();
            _product.CategoryId = cat.Id == 0 ? (int?)null : cat.Id;
            _product.SupplierId = sup.Id == 0 ? (int?)null : sup.Id;
            _product.BatchNumber = _batch.Text.Trim();
            _product.Barcode = _barcode.Text.Trim();
            _product.Quantity = (int)_qty.Value;
            _product.UnitPrice = (double)_price.Value;
            _product.ManufactureDate = _mfg.Checked ? _mfg.Value.Date : (DateTime?)null;
            _product.ExpiryDate = _exp.Value.Date;
            _product.Location = _location.Text.Trim();
            _product.Status = _status.SelectedItem as string ?? "Active";
            _product.Notes = _notes.Text.Trim();

            try
            {
                bool isNew = _product.ProductId == 0;
                ProductService.Save(_product);
                ActivityLogService.Log(isNew ? "Add Product" : "Edit Product",
                    $"{_product.Name} (Batch {_product.BatchNumber}, Expiry {_product.ExpiryDate:dd MMM yyyy})");
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Theme.Error(ex, "save the product");
            }
        }
    }
}
