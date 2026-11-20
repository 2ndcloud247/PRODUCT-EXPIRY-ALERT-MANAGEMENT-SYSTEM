using System;
using System.Collections.Generic;
using System.Linq;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Models;

namespace ExpiryAlertSystem.Services
{
    /// <summary>
    /// THE ALERT ENGINE - the heart of the system.
    ///
    /// How it works:
    ///   1. For every ACTIVE product, count the days left until its expiry date.
    ///   2. Classify it:
    ///         days left  &lt; 0               -> EXPIRED        (red)
    ///         days left &lt;= CriticalDays    -> CRITICAL       (orange)  default 7 days
    ///         days left &lt;= WarningDays     -> EXPIRING SOON  (yellow)  default 30 days
    ///         otherwise                    -> SAFE           (green)
    ///   3. For every product that is not Safe, an alert is written to the AlertLog table
    ///      (only once per product per alert type, so the user is not spammed).
    ///   4. The check runs when the program starts and again every few minutes (timer in MainForm).
    ///      New alerts show as a pop-up notification in the Windows system tray.
    ///   5. A user "acknowledges" an alert to say "I have seen this and taken action".
    /// </summary>
    public static class AlertService
    {
        public static ExpiryStatus GetStatus(Product p) =>
            GetStatus(p, SettingsService.CriticalDays, SettingsService.WarningDays);

        public static ExpiryStatus GetStatus(Product p, int criticalDays, int warningDays)
        {
            if (string.Equals(p.Status, "Disposed", StringComparison.OrdinalIgnoreCase)) return ExpiryStatus.Disposed;
            var days = p.DaysLeft;
            if (days < 0) return ExpiryStatus.Expired;
            if (days <= criticalDays) return ExpiryStatus.Critical;
            if (days <= warningDays) return ExpiryStatus.ExpiringSoon;
            return ExpiryStatus.Safe;
        }

        public static string StatusText(ExpiryStatus s)
        {
            switch (s)
            {
                case ExpiryStatus.Expired: return "Expired";
                case ExpiryStatus.Critical: return "Critical";
                case ExpiryStatus.ExpiringSoon: return "Expiring Soon";
                case ExpiryStatus.Disposed: return "Disposed";
                default: return "Safe";
            }
        }

        public static string DaysLeftText(Product p)
        {
            var d = p.DaysLeft;
            if (d < 0) return $"Expired {-d} day{(d == -1 ? "" : "s")} ago";
            if (d == 0) return "Expires TODAY";
            if (d == 1) return "Expires tomorrow";
            return $"{d} days left";
        }

        /// <summary>Checks all products and records new alerts. Returns how many NEW alerts were created.</summary>
        public static int CheckAndGenerateAlerts()
        {
            int critical = SettingsService.CriticalDays, warning = SettingsService.WarningDays;
            var products = ProductService.GetAll(includeDisposed: false);
            int created = 0;
            var now = Database.ToDbTime(DateTime.Now);

            using (var con = Database.Open())
            using (var tx = con.BeginTransaction())
            {
                foreach (var p in products)
                {
                    var status = GetStatus(p, critical, warning);
                    if (status == ExpiryStatus.Safe) continue;

                    var type = StatusText(status);
                    var msg = $"{p.Name} (Batch {(string.IsNullOrEmpty(p.BatchNumber) ? "-" : p.BatchNumber)}, Qty {p.Quantity}) " +
                              $"- {DaysLeftText(p)} (expiry {p.ExpiryDate:dd MMM yyyy}).";

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT OR IGNORE INTO AlertLog(ProductId, AlertType, Message, AlertDate, IsAcknowledged) " +
                                          "VALUES(@p, @t, @m, @d, 0)";
                        cmd.Parameters.AddWithValue("@p", p.ProductId);
                        cmd.Parameters.AddWithValue("@t", type);
                        cmd.Parameters.AddWithValue("@m", msg);
                        cmd.Parameters.AddWithValue("@d", now);
                        created += cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
            return created;
        }

        public static List<AlertRecord> GetAlerts(bool includeAcknowledged)
        {
            var list = new List<AlertRecord>();
            using (var con = Database.Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
SELECT a.AlertId, a.ProductId, p.Name, IFNULL(p.BatchNumber,''), p.ExpiryDate, a.AlertType, a.Message,
       a.AlertDate, a.IsAcknowledged, IFNULL(a.AcknowledgedBy,'')
FROM AlertLog a JOIN Products p ON p.ProductId = a.ProductId " +
                    (includeAcknowledged ? "" : "WHERE a.IsAcknowledged = 0 ") +
                    @"ORDER BY a.IsAcknowledged, CASE a.AlertType WHEN 'Expired' THEN 0 WHEN 'Critical' THEN 1 ELSE 2 END, p.ExpiryDate";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new AlertRecord
                        {
                            AlertId = r.GetInt32(0),
                            ProductId = r.GetInt32(1),
                            ProductName = r.GetString(2),
                            BatchNumber = r.GetString(3),
                            ExpiryDate = Database.FromDb(r.GetString(4)),
                            AlertType = r.GetString(5),
                            Message = r.GetString(6),
                            AlertDate = Database.FromDb(r.GetString(7)),
                            IsAcknowledged = r.GetInt32(8) == 1,
                            AcknowledgedBy = r.GetString(9)
                        });
            }
            return list;
        }

        public static int CountUnacknowledged() =>
            Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM AlertLog WHERE IsAcknowledged = 0"));

        public static void Acknowledge(IEnumerable<int> alertIds)
        {
            var now = Database.ToDbTime(DateTime.Now);
            using (var con = Database.Open())
                foreach (var id in alertIds)
                    Database.Execute(con, "UPDATE AlertLog SET IsAcknowledged=1, AcknowledgedBy=@u, AcknowledgedAt=@t WHERE AlertId=@id",
                        ("@u", Session.Username), ("@t", now), ("@id", id));
        }

        /// <summary>Counts of products in each status - used by the dashboard cards and chart.</summary>
        public static Dictionary<ExpiryStatus, int> GetSummary(List<Product> products)
        {
            int c = SettingsService.CriticalDays, w = SettingsService.WarningDays;
            var result = Enum.GetValues(typeof(ExpiryStatus)).Cast<ExpiryStatus>().ToDictionary(s => s, s => 0);
            foreach (var p in products) result[GetStatus(p, c, w)]++;
            return result;
        }
    }
}
