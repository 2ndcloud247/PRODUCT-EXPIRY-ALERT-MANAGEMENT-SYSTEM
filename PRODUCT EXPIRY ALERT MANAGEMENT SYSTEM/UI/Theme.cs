using System;
using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Models;

namespace ExpiryAlertSystem.UI
{
    /// <summary>A flat button that does not draw the dotted "focus" border.</summary>
    public class FlatButton : Button
    {
        protected override bool ShowFocusCues => false;
    }

    /// <summary>
    /// One place for all colours, fonts and "control factory" helpers, so every screen
    /// looks the same. Change a colour here and the whole application changes.
    /// </summary>
    public static class Theme
    {
        // Main colours
        public static readonly Color Sidebar = Color.FromArgb(22, 41, 66);
        public static readonly Color SidebarHover = Color.FromArgb(36, 60, 92);
        public static readonly Color SidebarActive = Color.FromArgb(0, 150, 136);
        public static readonly Color Primary = Color.FromArgb(0, 137, 123);
        public static readonly Color Secondary = Color.FromArgb(84, 110, 122);
        public static readonly Color Danger = Color.FromArgb(211, 47, 47);
        public static readonly Color Background = Color.FromArgb(243, 246, 249);
        public static readonly Color Surface = Color.White;
        public static readonly Color Border = Color.FromArgb(222, 228, 234);
        public static readonly Color Text = Color.FromArgb(33, 43, 54);
        public static readonly Color MutedText = Color.FromArgb(110, 122, 135);

        // Expiry status colours (traffic-light idea)
        public static readonly Color ExpiredColor = Color.FromArgb(211, 47, 47);   // red
        public static readonly Color CriticalColor = Color.FromArgb(245, 124, 0);  // orange
        public static readonly Color SoonColor = Color.FromArgb(251, 192, 45);     // yellow
        public static readonly Color SafeColor = Color.FromArgb(56, 142, 60);      // green
        public static readonly Color DisposedColor = Color.FromArgb(120, 130, 140); // grey

        /// <summary>Darker yellow so "Expiring Soon" text is readable on white.</summary>
        public static readonly Color SoonTextColor = Color.FromArgb(176, 120, 0);

        /// <summary>Colour for status WORDS (the yellow is darkened so it stays readable).</summary>
        public static Color StatusTextColor(ExpiryStatus s) => s == ExpiryStatus.ExpiringSoon ? SoonTextColor : StatusColor(s);

        // Fonts
        public static readonly Font Body = new Font("Segoe UI", 10F);
        public static readonly Font BodyBold = new Font("Segoe UI Semibold", 10F);
        public static readonly Font Small = new Font("Segoe UI", 9F);
        public static readonly Font Title = new Font("Segoe UI Semibold", 18F);
        public static readonly Font Subtitle = new Font("Segoe UI", 10.5F);
        public static readonly Font CardNumber = new Font("Segoe UI Semibold", 26F);
        public static readonly Font Heading = new Font("Segoe UI Semibold", 12F);

        public static Color StatusColor(ExpiryStatus s)
        {
            switch (s)
            {
                case ExpiryStatus.Expired: return ExpiredColor;
                case ExpiryStatus.Critical: return CriticalColor;
                case ExpiryStatus.ExpiringSoon: return SoonColor;
                case ExpiryStatus.Disposed: return DisposedColor;
                default: return SafeColor;
            }
        }

        /// <summary>Very light version of the status colour, used as a table row background.</summary>
        public static Color StatusRowColor(ExpiryStatus s)
        {
            switch (s)
            {
                case ExpiryStatus.Expired: return Color.FromArgb(255, 228, 228);
                case ExpiryStatus.Critical: return Color.FromArgb(255, 236, 214);
                case ExpiryStatus.ExpiringSoon: return Color.FromArgb(255, 248, 210);
                case ExpiryStatus.Disposed: return Color.FromArgb(236, 238, 240);
                default: return Color.FromArgb(232, 245, 233);
            }
        }

        public static Color StatusColor(string alertType)
        {
            switch (alertType)
            {
                case "Expired": return ExpiredColor;
                case "Critical": return CriticalColor;
                default: return SoonColor;
            }
        }

        // ---------------------------------------------------------------- factories

        public static Button MakeButton(string text, Color back, int width = 130)
        {
            var b = new FlatButton
            {
                Text = text,
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = BodyBold,
                Size = new Size(width, 36),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0),
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(back, 0.05f);
            return b;
        }

        public static Label MakeLabel(string text, Font font = null, Color? color = null)
        {
            return new Label
            {
                Text = text,
                UseMnemonic = false,
                Font = font ?? Body,
                ForeColor = color ?? Text,
                AutoSize = true,
                BackColor = Color.Transparent
            };
        }

        /// <summary>Title + short explanation shown at the top of every screen.</summary>
        public static Panel MakeHeader(string title, string explanation)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Background, Padding = new Padding(0, 0, 0, 10) };
            var t = MakeLabel(title, Title);
            t.Location = new Point(0, 0);
            var s = MakeLabel(explanation, Subtitle, MutedText);
            s.Location = new Point(2, 38);
            s.MaximumSize = new Size(1100, 0);
            p.Controls.Add(t);
            p.Controls.Add(s);
            return p;
        }

        /// <summary>A row of buttons/inputs that flows left to right (toolbar).</summary>
        public static FlowLayoutPanel MakeToolbar()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Background,
                Padding = new Padding(0, 4, 0, 8),
                WrapContents = false,
                AutoScroll = false
            };
        }

        /// <summary>Applies a clean modern look to a data table.</summary>
        public static void StyleGrid(DataGridView g)
        {
            g.BackgroundColor = Surface;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Border;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersDefaultCellStyle.BackColor = Sidebar;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = BodyBold;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Sidebar;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 40;
            g.DefaultCellStyle.Font = Body;
            g.DefaultCellStyle.ForeColor = Text;
            g.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(178, 223, 219);
            g.DefaultCellStyle.SelectionForeColor = Text;
            g.RowTemplate.Height = 34;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.Dock = DockStyle.Fill;
        }

        public static DataGridViewTextBoxColumn Col(string header, string name, float weight = 100, string format = null,
            DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleLeft)
        {
            var c = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Name = name,
                DataPropertyName = name,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            c.DefaultCellStyle.Alignment = align;
            if (format != null) c.DefaultCellStyle.Format = format;
            return c;
        }

        public static string Money(double v) => "₦" + v.ToString("N2"); // Naira sign

        /// <summary>Wraps a control in a white "card" with a thin border.</summary>
        public static Panel MakeCard(Control inner, Padding? padding = null)
        {
            var card = new Panel { BackColor = Surface, Padding = padding ?? new Padding(14) };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            inner.Dock = DockStyle.Fill;
            card.Controls.Add(inner);
            return card;
        }

        public static void Info(string msg, string title = "Information") =>
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void Warn(string msg, string title = "Please check") =>
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static bool Confirm(string msg, string title = "Please confirm") =>
            MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        public static void Error(Exception ex, string action) =>
            MessageBox.Show($"Could not {action}.\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
