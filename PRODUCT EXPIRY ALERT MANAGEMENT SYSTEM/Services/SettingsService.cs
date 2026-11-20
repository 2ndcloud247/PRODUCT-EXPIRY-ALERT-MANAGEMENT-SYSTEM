using System;
using ExpiryAlertSystem.Data;

namespace ExpiryAlertSystem.Services
{
    /// <summary>
    /// Reads and saves configurable values in the Settings table.
    ///   WarningDays        - products expiring within this many days are "Expiring Soon" (default 30)
    ///   CriticalDays       - products expiring within this many days are "Critical" (default 7)
    ///   CheckIntervalMins  - how often (minutes) the program re-checks for expiring products (default 30)
    ///   ShowStartupAlert   - show the alert pop-up when the program opens (1 = yes, 0 = no)
    ///   BusinessName       - name printed on reports
    /// </summary>
    public static class SettingsService
    {
        public static void EnsureDefaults()
        {
            SetIfMissing("WarningDays", "30");
            SetIfMissing("CriticalDays", "7");
            SetIfMissing("CheckIntervalMins", "30");
            SetIfMissing("ShowStartupAlert", "1");
            SetIfMissing("BusinessName", "My Store / Pharmacy");
        }

        public static int WarningDays
        {
            get => GetInt("WarningDays", 30);
            set => Set("WarningDays", value.ToString());
        }

        public static int CriticalDays
        {
            get => GetInt("CriticalDays", 7);
            set => Set("CriticalDays", value.ToString());
        }

        public static int CheckIntervalMins
        {
            get => GetInt("CheckIntervalMins", 30);
            set => Set("CheckIntervalMins", value.ToString());
        }

        public static bool ShowStartupAlert
        {
            get => Get("ShowStartupAlert", "1") == "1";
            set => Set("ShowStartupAlert", value ? "1" : "0");
        }

        public static string BusinessName
        {
            get => Get("BusinessName", "My Store");
            set => Set("BusinessName", value);
        }

        // ---------------------------------------------------------------- low level

        public static string Get(string key, string fallback)
        {
            var v = Database.Scalar("SELECT Value FROM Settings WHERE Key = @k", ("@k", key));
            return v == null || v == DBNull.Value ? fallback : v.ToString();
        }

        public static int GetInt(string key, int fallback) =>
            int.TryParse(Get(key, fallback.ToString()), out var n) ? n : fallback;

        public static void Set(string key, string value) =>
            Database.Execute("INSERT INTO Settings(Key, Value) VALUES(@k, @v) " +
                             "ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value",
                ("@k", key), ("@v", value));

        private static void SetIfMissing(string key, string value) =>
            Database.Execute("INSERT OR IGNORE INTO Settings(Key, Value) VALUES(@k, @v)",
                ("@k", key), ("@v", value));
    }
}
