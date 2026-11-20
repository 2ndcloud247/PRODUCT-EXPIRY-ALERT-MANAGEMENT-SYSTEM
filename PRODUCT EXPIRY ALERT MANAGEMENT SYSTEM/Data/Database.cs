using System;
using System.IO;
using ExpiryAlertSystem.Services;
using Microsoft.Data.Sqlite;

namespace ExpiryAlertSystem.Data
{
    /// <summary>
    /// Everything about the SQLite database lives here.
    ///
    /// SQLite stores the whole database in ONE file ("expiry_alert.db"). There is no database
    /// server to install. The file is kept in a shared data folder:
    ///     C:\ProgramData\ProductExpiryAlertSystem\expiry_alert.db
    /// (the program itself is installed in "Program Files", which normal users cannot write to,
    /// so the data must live somewhere else). Every Windows user on the PC shares the same data.
    /// You can open the file with the free tool "DB Browser for SQLite" to look at the tables.
    ///
    /// Tables used by the system:
    ///   Users        - people who can log in (Admin or Staff)
    ///   Categories   - product groups, e.g. Drugs, Dairy, Beverages
    ///   Suppliers    - companies that supply the products
    ///   Products     - every product/batch with its manufacture and expiry date
    ///   AlertLog     - alerts the system raised (expired / expiring soon) and whether they were acknowledged
    ///   Settings     - configurable values such as "how many days before expiry to warn"
    ///   ActivityLog  - audit trail of who did what and when
    /// </summary>
    public static class Database
    {
        public const string FileName = "expiry_alert.db";

        public const string DataFolderName = "ProductExpiryAlertSystem";

        /// <summary>C:\ProgramData\ProductExpiryAlertSystem</summary>
        public static string DataFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), DataFolderName);

        public static string FilePath => Path.Combine(DataFolder, FileName);

        /// <summary>
        /// Creates the data folder if needed. If an older version kept the database next to the
        /// .exe, that file is copied across once so no data is lost.
        /// </summary>
        private static void PrepareDataFolder()
        {
            Directory.CreateDirectory(DataFolder);
            var oldFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
            if (!File.Exists(FilePath) && File.Exists(oldFile))
                File.Copy(oldFile, FilePath);
        }

        private static string ConnectionString =>
            new SqliteConnectionStringBuilder { DataSource = FilePath, ForeignKeys = true }.ToString();

        /// <summary>Opens a new connection. Always use it inside a "using" block so it is closed.</summary>
        public static SqliteConnection Open()
        {
            var con = new SqliteConnection(ConnectionString);
            con.Open();
            return con;
        }

        /// <summary>Called once when the program starts.</summary>
        public static void Initialize()
        {
            PrepareDataFolder();
            bool firstRun = !File.Exists(FilePath);

            using (var con = Open())
            {
                Execute(con, Schema);
            }

            SettingsService.EnsureDefaults();
            UserService.EnsureDefaultAdmin();

            if (firstRun)
                SampleData.Insert();
        }

        // ---------------------------------------------------------------- helpers

        public static int Execute(SqliteConnection con, string sql, params (string name, object value)[] parameters)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = sql;
                AddParameters(cmd, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static int Execute(string sql, params (string name, object value)[] parameters)
        {
            using (var con = Open()) return Execute(con, sql, parameters);
        }

        public static object Scalar(string sql, params (string name, object value)[] parameters)
        {
            using (var con = Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = sql;
                AddParameters(cmd, parameters);
                return cmd.ExecuteScalar();
            }
        }

        public static void AddParameters(SqliteCommand cmd, (string name, object value)[] parameters)
        {
            if (parameters == null) return;
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        /// <summary>Dates are stored as text "yyyy-MM-dd" so they sort and compare correctly.</summary>
        public static string ToDb(DateTime date) => date.ToString("yyyy-MM-dd");
        public static string ToDbTime(DateTime date) => date.ToString("yyyy-MM-dd HH:mm:ss");

        public static DateTime FromDb(object value)
        {
            if (value == null || value == DBNull.Value) return DateTime.MinValue;
            return DateTime.Parse(value.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------------- schema

        private const string Schema = @"
CREATE TABLE IF NOT EXISTS Users (
    UserId       INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT NOT NULL UNIQUE COLLATE NOCASE,
    FullName     TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    Role         TEXT NOT NULL DEFAULT 'Staff',      -- 'Admin' or 'Staff'
    IsActive     INTEGER NOT NULL DEFAULT 1,
    CreatedAt    TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Categories (
    CategoryId   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name         TEXT NOT NULL UNIQUE COLLATE NOCASE,
    Description  TEXT
);

CREATE TABLE IF NOT EXISTS Suppliers (
    SupplierId   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name         TEXT NOT NULL UNIQUE COLLATE NOCASE,
    Phone        TEXT,
    Email        TEXT,
    Address      TEXT
);

CREATE TABLE IF NOT EXISTS Products (
    ProductId       INTEGER PRIMARY KEY AUTOINCREMENT,
    Name            TEXT NOT NULL,
    CategoryId      INTEGER REFERENCES Categories(CategoryId) ON DELETE SET NULL,
    SupplierId      INTEGER REFERENCES Suppliers(SupplierId) ON DELETE SET NULL,
    BatchNumber     TEXT,
    Barcode         TEXT,
    Quantity        INTEGER NOT NULL DEFAULT 0,
    UnitPrice       REAL NOT NULL DEFAULT 0,
    ManufactureDate TEXT,
    ExpiryDate      TEXT NOT NULL,                    -- yyyy-MM-dd
    Location        TEXT,                             -- shelf / store location
    Status          TEXT NOT NULL DEFAULT 'Active',   -- 'Active' or 'Disposed'
    Notes           TEXT,
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_Products_ExpiryDate ON Products(ExpiryDate);

CREATE TABLE IF NOT EXISTS AlertLog (
    AlertId         INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId       INTEGER NOT NULL REFERENCES Products(ProductId) ON DELETE CASCADE,
    AlertType       TEXT NOT NULL,                    -- 'Expired', 'Critical', 'Expiring Soon'
    Message         TEXT NOT NULL,
    AlertDate       TEXT NOT NULL,
    IsAcknowledged  INTEGER NOT NULL DEFAULT 0,
    AcknowledgedBy  TEXT,
    AcknowledgedAt  TEXT,
    UNIQUE(ProductId, AlertType)                      -- the same alert is never raised twice
);

CREATE TABLE IF NOT EXISTS Settings (
    Key    TEXT PRIMARY KEY,
    Value  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ActivityLog (
    LogId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Username  TEXT,
    Action    TEXT NOT NULL,
    Details   TEXT,
    LogDate   TEXT NOT NULL
);
";
    }
}
