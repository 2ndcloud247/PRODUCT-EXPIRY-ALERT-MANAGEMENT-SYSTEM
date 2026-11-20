using System;
using System.Windows.Forms;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Forms;

namespace ExpiryAlertSystem
{
    /// <summary>
    /// Entry point of the application.
    ///
    /// Start-up sequence:
    ///   1. Prepare the SQLite database (create the file and tables if they do not exist,
    ///      and add a default admin account plus sample data on the very first run).
    ///   2. Show the Login screen.
    ///   3. If login succeeds, open the Main window (dashboard, products, alerts, reports...).
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Show a friendly message instead of crashing if something unexpected happens.
            Application.ThreadException += (s, e) => ShowError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowError(e.ExceptionObject as Exception);

            try
            {
                Database.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("The database could not be opened or created.\n\n" + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK) return;
            }

            Application.Run(new MainForm());
        }

        private static void ShowError(Exception ex)
        {
            MessageBox.Show("Something went wrong:\n\n" + (ex?.Message ?? "Unknown error"),
                "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
