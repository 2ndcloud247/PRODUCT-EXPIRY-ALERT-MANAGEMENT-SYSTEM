using System;
using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>
    /// USERS (Admin only) - create staff accounts, change roles, disable accounts or reset passwords.
    /// The system always keeps at least one active Admin so nobody is locked out.
    /// </summary>
    public class UsersView : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _username = new TextBox { Width = 300 };
        private readonly TextBox _fullName = new TextBox { Width = 300 };
        private readonly ComboBox _role = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox _active = new CheckBox { Text = "Account is active (can log in)", AutoSize = true, Checked = true };
        private readonly TextBox _password = new TextBox { Width = 300, UseSystemPasswordChar = true };
        private readonly Label _pwdHint = new Label { AutoSize = true, ForeColor = Theme.MutedText, Font = Theme.Small };
        private int _editingId;

        public UsersView()
        {
            BackColor = Theme.Background;
            var header = Theme.MakeHeader("User Accounts",
                "Admins can manage everything. Staff can manage products, alerts and reports but not users or the activity log.");

            Theme.StyleGrid(_grid);
            _grid.Columns.Add(Theme.Col("Id", "Id", 10));
            _grid.Columns.Add(Theme.Col("Username", "Username", 90));
            _grid.Columns.Add(Theme.Col("Full Name", "FullName", 150));
            _grid.Columns.Add(Theme.Col("Role", "Role", 60));
            _grid.Columns.Add(Theme.Col("Status", "Status", 60));
            _grid.Columns["Id"].Visible = false;
            _grid.SelectionChanged += (s, e) => LoadSelected();

            _role.Items.AddRange(new object[] { "Staff", "Admin" });
            _role.SelectedIndex = 0;

            var flow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.Surface };
            flow.Controls.Add(new Label { Text = "User details", Font = Theme.Heading, AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
            void Field(string label, Control c) { flow.Controls.Add(new Label { Text = label, Font = Theme.BodyBold, AutoSize = true, Margin = new Padding(0, 6, 0, 2) }); flow.Controls.Add(c); }
            Field("Username *", _username);
            Field("Full Name *", _fullName);
            Field("Role", _role);
            Field("Password", _password);
            flow.Controls.Add(_pwdHint);
            _active.Margin = new Padding(0, 10, 0, 0);
            flow.Controls.Add(_active);

            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 18, 0, 0) };
            var nw = Theme.MakeButton("New", Theme.Secondary, 90);
            var save = Theme.MakeButton("✔ Save", Theme.Primary, 100);
            var del = Theme.MakeButton("Delete", Theme.Danger, 90);
            nw.Click += (s, e) => ClearForm();
            save.Click += (s, e) => Save();
            del.Click += (s, e) => Delete();
            buttons.Controls.AddRange(new Control[] { nw, save, del });
            flow.Controls.Add(buttons);

            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
            var gridCard = Theme.MakeCard(_grid, new Padding(1)); gridCard.Dock = DockStyle.Fill; gridCard.Margin = new Padding(0, 0, 12, 0);
            var formCard = Theme.MakeCard(flow, new Padding(18)); formCard.Dock = DockStyle.Fill; formCard.Margin = new Padding(0);
            split.Controls.Add(gridCard, 0, 0);
            split.Controls.Add(formCard, 1, 0);

            Controls.Add(split);
            Controls.Add(header);
            Reload();
            ClearForm();
        }

        private void Reload()
        {
            _grid.Rows.Clear();
            foreach (var u in UserService.GetAll())
            {
                int i = _grid.Rows.Add(u.UserId, u.Username, u.FullName, u.Role, u.IsActive ? "Active" : "Disabled");
                if (!u.IsActive) _grid.Rows[i].DefaultCellStyle.ForeColor = Theme.MutedText;
            }
        }

        private void LoadSelected()
        {
            if (_grid.CurrentRow == null) return;
            _editingId = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            _username.Text = _grid.CurrentRow.Cells["Username"].Value?.ToString();
            _username.ReadOnly = true;
            _fullName.Text = _grid.CurrentRow.Cells["FullName"].Value?.ToString();
            _role.SelectedItem = _grid.CurrentRow.Cells["Role"].Value?.ToString();
            _active.Checked = (string)_grid.CurrentRow.Cells["Status"].Value == "Active";
            _password.Text = "";
            _pwdHint.Text = "Leave blank to keep the current password.";
        }

        private void ClearForm()
        {
            _editingId = 0;
            _username.ReadOnly = false;
            _username.Text = _fullName.Text = _password.Text = "";
            _role.SelectedIndex = 0;
            _active.Checked = true;
            _pwdHint.Text = "Required for a new user (at least 6 characters).";
            _grid.ClearSelection();
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_username.Text) || string.IsNullOrWhiteSpace(_fullName.Text))
            { Theme.Warn("Username and Full Name are required."); return; }
            if ((_editingId == 0 || _password.Text != "") && _password.Text.Length < 6)
            { Theme.Warn("The password must be at least 6 characters."); return; }

            try
            {
                if (_editingId == 0)
                {
                    UserService.Create(_username.Text, _fullName.Text, _password.Text, (string)_role.SelectedItem);
                    ActivityLogService.Log("Add User", _username.Text.Trim());
                }
                else
                {
                    bool removingAdmin = ((string)_role.SelectedItem != "Admin" || !_active.Checked)
                                         && (string)_grid.CurrentRow?.Cells["Role"].Value == "Admin"
                                         && UserService.CountActiveAdmins() <= 1;
                    if (removingAdmin) { Theme.Warn("This is the last active Admin. Create another Admin first."); return; }
                    if (_editingId == Session.CurrentUser.UserId && !_active.Checked) { Theme.Warn("You cannot disable your own account."); return; }

                    UserService.Update(_editingId, _fullName.Text, (string)_role.SelectedItem, _active.Checked);
                    if (_password.Text != "") UserService.SetPassword(_editingId, _password.Text);
                    ActivityLogService.Log("Edit User", _username.Text + (_password.Text != "" ? " (password reset)" : ""));
                }
                Reload();
                ClearForm();
                Theme.Info("User saved.");
            }
            catch (Exception ex) { Theme.Error(ex, "save the user (the username may already exist)"); }
        }

        private void Delete()
        {
            if (_editingId == 0) { Theme.Info("Select a user first."); return; }
            if (_editingId == Session.CurrentUser.UserId) { Theme.Warn("You cannot delete your own account while logged in."); return; }
            if ((string)_grid.CurrentRow?.Cells["Role"].Value == "Admin" && UserService.CountActiveAdmins() <= 1)
            { Theme.Warn("This is the last active Admin and cannot be deleted."); return; }
            if (!Theme.Confirm($"Delete user \"{_username.Text}\"? This cannot be undone.")) return;
            UserService.Delete(_editingId);
            ActivityLogService.Log("Delete User", _username.Text);
            Reload();
            ClearForm();
        }
    }
}
