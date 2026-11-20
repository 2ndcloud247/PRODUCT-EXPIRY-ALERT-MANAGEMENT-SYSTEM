using System;
using System.Data;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Models;

namespace ExpiryAlertSystem.Services
{
    /// <summary>
    /// Audit trail: every important action (login, add product, delete, dispose, ...) is written
    /// to the ActivityLog table with the user and time, so the manager can see who did what.
    /// </summary>
    public static class ActivityLogService
    {
        public static void Log(string action, string details = null)
        {
            try
            {
                Database.Execute("INSERT INTO ActivityLog(Username, Action, Details, LogDate) VALUES(@u, @a, @d, @t)",
                    ("@u", Session.Username), ("@a", action), ("@d", details), ("@t", Database.ToDbTime(DateTime.Now)));
            }
            catch
            {
                // Logging must never stop the main work.
            }
        }

        public static DataTable GetRecent(int limit = 500)
        {
            var table = new DataTable();
            table.Columns.Add("Date / Time");
            table.Columns.Add("User");
            table.Columns.Add("Action");
            table.Columns.Add("Details");

            using (var con = Database.Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT LogDate, Username, Action, Details FROM ActivityLog ORDER BY LogId DESC LIMIT @n";
                cmd.Parameters.AddWithValue("@n", limit);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        table.Rows.Add(r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1),
                            r.GetString(2), r.IsDBNull(3) ? "" : r.GetString(3));
            }
            return table;
        }
    }
}
