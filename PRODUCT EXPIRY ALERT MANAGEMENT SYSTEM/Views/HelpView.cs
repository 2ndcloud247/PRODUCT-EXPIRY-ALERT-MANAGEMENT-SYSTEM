using System.Drawing;
using System.Windows.Forms;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Services;
using ExpiryAlertSystem.UI;

namespace ExpiryAlertSystem.Views
{
    /// <summary>HELP &amp; ABOUT - a built-in user guide and the project information.</summary>
    public class HelpView : UserControl
    {
        public HelpView()
        {
            BackColor = Theme.Background;
            var header = Theme.MakeHeader("Help & About", "A simple guide to using the system, and information about this project.");

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.BodyBold, Padding = new Point(18, 6) };
            tabs.TabPages.Add(Page("User Guide", BuildGuide));
            tabs.TabPages.Add(Page("How Alerts Work", BuildAlertsHelp));
            tabs.TabPages.Add(Page("About the Project", BuildAbout));

            Controls.Add(tabs);
            Controls.Add(header);
        }

        private static TabPage Page(string title, System.Action<RichTextBox> fill)
        {
            var page = new TabPage(title) { BackColor = Theme.Surface, Padding = new Padding(24) };
            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Surface,
                Font = new Font("Segoe UI", 11F),
                DetectUrls = false
            };
            fill(rtb);
            rtb.SelectionStart = 0;
            page.Controls.Add(rtb);
            return page;
        }

        // ---------------------------------------------------------------- writers

        private static void H(RichTextBox r, string text) => Write(r, text + "\n", new Font("Segoe UI Semibold", 15F), Theme.Sidebar);
        private static void H2(RichTextBox r, string text) => Write(r, "\n" + text + "\n", new Font("Segoe UI Semibold", 12F), Theme.Primary);
        private static void P(RichTextBox r, string text) => Write(r, text + "\n", new Font("Segoe UI", 11F), Theme.Text);
        private static void B(RichTextBox r, string label, string text)
        {
            Write(r, "   •  " + label, new Font("Segoe UI Semibold", 11F), Theme.Text);
            Write(r, (text == null ? "" : "  -  " + text) + "\n", new Font("Segoe UI", 11F), Theme.Text);
        }
        private static void C(RichTextBox r, string label, Color color, string text)
        {
            Write(r, "   ■  ", new Font("Segoe UI", 14F), color);
            Write(r, label, new Font("Segoe UI Semibold", 11F), Theme.Text);
            Write(r, "  -  " + text + "\n", new Font("Segoe UI", 11F), Theme.Text);
        }

        private static void Write(RichTextBox r, string text, Font font, Color color)
        {
            r.SelectionStart = r.TextLength;
            r.SelectionLength = 0;
            r.SelectionFont = font;
            r.SelectionColor = color;
            r.AppendText(text);
        }

        // ---------------------------------------------------------------- content

        private static void BuildGuide(RichTextBox r)
        {
            H(r, "Getting started");
            P(r, $"1.  Log in. On the first run use username \"{UserService.DefaultAdminUser}\" and password \"{UserService.DefaultAdminPassword}\", then change the password under Settings.");
            P(r, "2.  The system comes with sample products so you can see how it works. You can edit or delete them at any time.");
            P(r, "3.  Use the menu on the left to move between screens.");

            H2(r, "The screens");
            B(r, "Dashboard", "summary cards, the list of products that will expire next, and a chart of stock status. Click a card to open the matching products.");
            B(r, "Products", "add, edit, search, filter, dispose or delete products. Double-click a row to edit it.");
            B(r, "Alerts", "warnings raised by the system. Acknowledge an alert after you have acted on it.");
            B(r, "Reports", "expired products, products expiring in a period, full stock status, disposed items (losses) and a summary by category. Print or export to Excel.");
            B(r, "Categories & Suppliers", "the lists you choose from when adding a product.");
            B(r, "Users", "(Admin only) create staff accounts and reset passwords.");
            B(r, "Activity Log", "(Admin only) who did what and when.");
            B(r, "Settings", "alert rules, change password, backup database.");

            H2(r, "Adding a product");
            P(r, "Products > Add Product. Enter the name, choose a category and supplier, type the batch number, quantity and unit price, then pick the EXPIRY DATE. " +
                 "The coloured box at the bottom shows the status the product will have. Click Save.");

            H2(r, "When a product expires");
            P(r, "Remove it from the shelf, then select it and click \"Mark as Disposed\". It stays in the records (for the Losses report) but no longer raises alerts.");

            H2(r, "Tips");
            B(r, "Search box", "finds products by name, batch, barcode, supplier or location while you type.");
            B(r, "Column headers", "click a header to sort the table by that column.");
            B(r, "System tray", "while the program is open, new alerts pop up as a Windows notification near the clock.");
            B(r, "Backup", "Settings > Backup Database copies all your data to a safe place.");
        }

        private static void BuildAlertsHelp(RichTextBox r)
        {
            H(r, "How the alert engine works");
            P(r, "Every product has an expiry date. The system counts the days left from TODAY to that date and puts the product in one of four groups:");
            P(r, "");
            C(r, "EXPIRED", Theme.ExpiredColor, "the expiry date has passed. Remove the product from sale immediately.");
            C(r, "CRITICAL", Theme.CriticalColor, $"expires within {SettingsService.CriticalDays} days. Sell first, discount, or return to the supplier.");
            C(r, "EXPIRING SOON", Theme.SoonColor, $"expires within {SettingsService.WarningDays} days. Plan to move this stock.");
            C(r, "SAFE", Theme.SafeColor, $"more than {SettingsService.WarningDays} days left. No action needed.");
            P(r, "");
            P(r, "The number of days for Critical and Expiring Soon can be changed by an Admin in Settings.");

            H2(r, "When does it check?");
            B(r, "At start-up", "every time the program opens, followed by a pop-up summary of urgent products.");
            B(r, "Automatically", $"every {SettingsService.CheckIntervalMins} minutes while the program is open (Windows notification for new alerts).");
            B(r, "After changes", "whenever a product is added or edited.");
            B(r, "On demand", "the \"Check Now\" button on the Alerts screen.");

            H2(r, "Alert life-cycle");
            P(r, "Raised (Pending)  >>  Acknowledged by a user  >>  (optionally) Product Disposed");
            P(r, "Each product gets at most one alert per level, so you are warned when it becomes Expiring Soon, again when it becomes Critical, and again when it Expires - but you are never flooded with repeats.");

            H2(r, "Example");
            P(r, "Today is 1 March. Paracetamol batch EMZ-2301 expires on 5 March (4 days left). With Critical = 7 days, it is CRITICAL (orange), " +
                 "an alert is raised and it appears at the top of the Dashboard.");
        }

        private static void BuildAbout(RichTextBox r)
        {
            H(r, "Design and Implementation of a Product Expiry Alert Management System");
            P(r, "");
            B(r, "Name", "Iderima Lawrentta Onosedeba");
            B(r, "Matric No.", "IAUE/2025/PGD/0029");
            B(r, "Department", "Computer Science");
            B(r, "Programme", "Postgraduate Diploma (PGD)");
            B(r, "Option", "Information Technology");
            B(r, "School", "Ignatius Ajuru University of Education, Rumuolumini, Port Harcourt");

            H2(r, "Aim");
            P(r, "To design and implement a desktop system that records products and their expiry dates, automatically detects products that are expired or about to expire, " +
                 "and alerts the user in time so that losses, waste and the sale of expired goods are reduced.");

            H2(r, "Objectives");
            B(r, "1", "Keep a reliable record of products, batches, quantities, suppliers and expiry dates.");
            B(r, "2", "Automatically classify products as Expired, Critical, Expiring Soon or Safe.");
            B(r, "3", "Notify users through a dashboard, start-up pop-up and Windows notifications.");
            B(r, "4", "Produce printable reports on expired stock, upcoming expiries and losses.");
            B(r, "5", "Secure the system with user accounts, roles and an activity log.");

            H2(r, "Technology used");
            B(r, "Language", "C# (.NET Framework 4.7.2)");
            B(r, "User interface", "Windows Forms (desktop application)");
            B(r, "Database", "SQLite (embedded, file-based - no server needed)");
            B(r, "Data access", "Microsoft.Data.Sqlite with parameterised SQL queries");
            B(r, "Security", "Salted SHA-256 password hashing, role-based access (Admin / Staff)");

            H2(r, "Database file");
            P(r, Database.FilePath);
        }
    }
}
