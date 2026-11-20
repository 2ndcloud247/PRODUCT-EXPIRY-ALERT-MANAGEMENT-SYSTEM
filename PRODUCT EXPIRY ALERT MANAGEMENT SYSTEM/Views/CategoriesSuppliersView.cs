using System;
using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>
    /// CATEGORIES &amp; SUPPLIERS - the lists you choose from when adding a product.
    /// Select a row to load it into the form on the right, change it and click Save.
    /// Click "New" to clear the form and add a new record.
    /// </summary>
    public class CategoriesSuppliersView : UserControl
    {
        public CategoriesSuppliersView()
        {
            BackColor = Theme.Background;
            var header = Theme.MakeHeader("Categories & Suppliers",
                "Manage the product categories and suppliers. Select a row to edit it, or click New to add one.");

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.BodyBold, Padding = new Point(18, 6) };
            tabs.TabPages.Add(BuildCategoriesTab());
            tabs.TabPages.Add(BuildSuppliersTab());

            Controls.Add(tabs);
            Controls.Add(header);
        }

        // ---------------------------------------------------------------- categories

        private TabPage BuildCategoriesTab()
        {
            var page = new TabPage("Categories") { BackColor = Theme.Background, Padding = new Padding(10) };
            var grid = new DataGridView();
            Theme.StyleGrid(grid);
            grid.Columns.Add(Theme.Col("Id", "Id", 10));
            grid.Columns.Add(Theme.Col("Category Name", "Name", 120));
            grid.Columns.Add(Theme.Col("Description", "Desc", 180));
            grid.Columns.Add(Theme.Col("Products", "Count", 50, null, DataGridViewContentAlignment.MiddleRight));
            grid.Columns["Id"].Visible = false;

            var name = new TextBox { Width = 300 };
            var desc = new TextBox { Width = 300, Multiline = true, Height = 70 };
            int editingId = 0;

            void Reload()
            {
                grid.Rows.Clear();
                foreach (var c in LookupService.GetCategories()) grid.Rows.Add(c.Id, c.Name, c.Extra1, c.Extra2);
            }
            void Clear() { editingId = 0; name.Text = ""; desc.Text = ""; name.Focus(); }

            grid.SelectionChanged += (s, e) =>
            {
                if (grid.CurrentRow == null) return;
                editingId = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
                name.Text = grid.CurrentRow.Cells["Name"].Value?.ToString();
                desc.Text = grid.CurrentRow.Cells["Desc"].Value?.ToString();
            };

            var form = EditorPanel("Category details",
                ("Name *", name), ("Description", desc),
                onNew: Clear,
                onSave: () =>
                {
                    if (string.IsNullOrWhiteSpace(name.Text)) { Theme.Warn("Please enter a category name."); return; }
                    try
                    {
                        LookupService.SaveCategory(editingId, name.Text, desc.Text.Trim());
                        ActivityLogService.Log(editingId == 0 ? "Add Category" : "Edit Category", name.Text.Trim());
                        Reload(); Clear();
                    }
                    catch (Exception ex) { Theme.Error(ex, "save the category (the name may already exist)"); }
                },
                onDelete: () =>
                {
                    if (editingId == 0) { Theme.Info("Select a category first."); return; }
                    if (!Theme.Confirm($"Delete category \"{name.Text}\"?\nProducts in it will keep existing but will have no category.")) return;
                    LookupService.DeleteCategory(editingId);
                    ActivityLogService.Log("Delete Category", name.Text);
                    Reload(); Clear();
                });

            PlaceSideBySide(page, grid, form);
            Reload();
            return page;
        }

        // ---------------------------------------------------------------- suppliers

        private TabPage BuildSuppliersTab()
        {
            var page = new TabPage("Suppliers") { BackColor = Theme.Background, Padding = new Padding(10) };
            var grid = new DataGridView();
            Theme.StyleGrid(grid);
            grid.Columns.Add(Theme.Col("Id", "Id", 10));
            grid.Columns.Add(Theme.Col("Supplier Name", "Name", 140));
            grid.Columns.Add(Theme.Col("Phone", "Phone", 80));
            grid.Columns.Add(Theme.Col("Email", "Email", 120));
            grid.Columns.Add(Theme.Col("Address", "Address", 100));
            grid.Columns["Id"].Visible = false;

            var name = new TextBox { Width = 300 };
            var phone = new TextBox { Width = 300 };
            var email = new TextBox { Width = 300 };
            var addr = new TextBox { Width = 300, Multiline = true, Height = 60 };
            int editingId = 0;

            void Reload()
            {
                grid.Rows.Clear();
                foreach (var s in LookupService.GetSuppliers()) grid.Rows.Add(s.Id, s.Name, s.Extra1, s.Extra2, s.Extra3);
            }
            void Clear() { editingId = 0; name.Text = phone.Text = email.Text = addr.Text = ""; name.Focus(); }

            grid.SelectionChanged += (s, e) =>
            {
                if (grid.CurrentRow == null) return;
                editingId = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);
                name.Text = grid.CurrentRow.Cells["Name"].Value?.ToString();
                phone.Text = grid.CurrentRow.Cells["Phone"].Value?.ToString();
                email.Text = grid.CurrentRow.Cells["Email"].Value?.ToString();
                addr.Text = grid.CurrentRow.Cells["Address"].Value?.ToString();
            };

            var form = EditorPanel("Supplier details",
                ("Name *", name), ("Phone", phone), ("Email", email), ("Address", addr),
                onNew: Clear,
                onSave: () =>
                {
                    if (string.IsNullOrWhiteSpace(name.Text)) { Theme.Warn("Please enter the supplier name."); return; }
                    if (email.Text.Trim() != "" && !email.Text.Contains("@")) { Theme.Warn("The email address does not look valid."); return; }
                    try
                    {
                        LookupService.SaveSupplier(editingId, name.Text, phone.Text.Trim(), email.Text.Trim(), addr.Text.Trim());
                        ActivityLogService.Log(editingId == 0 ? "Add Supplier" : "Edit Supplier", name.Text.Trim());
                        Reload(); Clear();
                    }
                    catch (Exception ex) { Theme.Error(ex, "save the supplier (the name may already exist)"); }
                },
                onDelete: () =>
                {
                    if (editingId == 0) { Theme.Info("Select a supplier first."); return; }
                    if (!Theme.Confirm($"Delete supplier \"{name.Text}\"?\nProducts from this supplier will keep existing.")) return;
                    LookupService.DeleteSupplier(editingId);
                    ActivityLogService.Log("Delete Supplier", name.Text);
                    Reload(); Clear();
                });

            PlaceSideBySide(page, grid, form);
            Reload();
            return page;
        }

        // ---------------------------------------------------------------- shared layout

        private static void PlaceSideBySide(TabPage page, DataGridView grid, Control form)
        {
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
            var card = Theme.MakeCard(grid, new Padding(1));
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 12, 0);
            split.Controls.Add(card, 0, 0);
            split.Controls.Add(form, 1, 0);
            page.Controls.Add(split);
        }

        private static Control EditorPanel(string title, (string label, Control box) f1, (string label, Control box) f2,
            Action onNew, Action onSave, Action onDelete) =>
            EditorPanel(title, new[] { f1, f2 }, onNew, onSave, onDelete);

        private static Control EditorPanel(string title, (string label, Control box) f1, (string label, Control box) f2,
            (string label, Control box) f3, (string label, Control box) f4, Action onNew, Action onSave, Action onDelete) =>
            EditorPanel(title, new[] { f1, f2, f3, f4 }, onNew, onSave, onDelete);

        private static Control EditorPanel(string title, (string label, Control box)[] fields, Action onNew, Action onSave, Action onDelete)
        {
            var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Surface };
            flow.Controls.Add(new Label { Text = title, Font = Theme.Heading, AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
            foreach (var (label, box) in fields)
            {
                flow.Controls.Add(new Label { Text = label, Font = Theme.BodyBold, AutoSize = true, Margin = new Padding(0, 4, 0, 2) });
                box.Font = Theme.Body;
                flow.Controls.Add(box);
            }
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 18, 0, 0) };
            var nw = Theme.MakeButton("New", Theme.Secondary, 90);
            var save = Theme.MakeButton("✔ Save", Theme.Primary, 100);
            var del = Theme.MakeButton("Delete", Theme.Danger, 90);
            nw.Click += (s, e) => onNew();
            save.Click += (s, e) => onSave();
            del.Click += (s, e) => onDelete();
            buttons.Controls.AddRange(new Control[] { nw, save, del });
            flow.Controls.Add(buttons);

            var card = Theme.MakeCard(flow, new Padding(18));
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0);
            return card;
        }
    }
}
