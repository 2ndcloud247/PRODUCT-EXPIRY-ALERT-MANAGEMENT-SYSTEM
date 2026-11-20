using System;

namespace ExpiryAlertSystem.Models
{
    /// <summary>
    /// The expiry state of a product, worked out from how many days are left before it expires.
    ///   Expired        : expiry date has passed (days left &lt; 0)
    ///   Critical       : expires within the "critical" window (default 7 days)
    ///   ExpiringSoon   : expires within the "warning" window (default 30 days)
    ///   Safe           : more time than the warning window
    ///   Disposed       : product was removed from stock (no longer monitored)
    /// </summary>
    public enum ExpiryStatus
    {
        Expired,
        Critical,
        ExpiringSoon,
        Safe,
        Disposed
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string BatchNumber { get; set; }
        public string Barcode { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Location { get; set; }
        public string Status { get; set; } = "Active";
        public string Notes { get; set; }

        /// <summary>Number of days from today until the expiry date (negative = already expired).</summary>
        public int DaysLeft => (int)(ExpiryDate.Date - DateTime.Today).TotalDays;

        public double TotalValue => Quantity * UnitPrice;
    }

    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Extra3 { get; set; }
        public override string ToString() => Name;
    }

    public class AlertRecord
    {
        public int AlertId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string AlertType { get; set; }
        public string Message { get; set; }
        public DateTime AlertDate { get; set; }
        public bool IsAcknowledged { get; set; }
        public string AcknowledgedBy { get; set; }
    }

    /// <summary>Keeps track of who is logged in during this session.</summary>
    public static class Session
    {
        public static User CurrentUser { get; set; }
        public static string Username => CurrentUser?.Username ?? "system";
    }
}
