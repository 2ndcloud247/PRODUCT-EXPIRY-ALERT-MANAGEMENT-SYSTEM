using System.Collections.Generic;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Models;

namespace ExpiryAlertSystem.Services
{
    /// <summary>Categories and Suppliers (simple lists used when adding a product).</summary>
    public static class LookupService
    {
        // ---------------------------------------------------------------- categories

        public static List<LookupItem> GetCategories()
        {
            var list = new List<LookupItem>();
            using (var con = Database.Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"SELECT c.CategoryId, c.Name, IFNULL(c.Description,''),
                                           (SELECT COUNT(*) FROM Products p WHERE p.CategoryId = c.CategoryId)
                                    FROM Categories c ORDER BY c.Name";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new LookupItem { Id = r.GetInt32(0), Name = r.GetString(1), Extra1 = r.GetString(2), Extra2 = r.GetInt32(3).ToString() });
            }
            return list;
        }

        public static void SaveCategory(int id, string name, string description)
        {
            if (id == 0)
                Database.Execute("INSERT INTO Categories(Name, Description) VALUES(@n, @d)", ("@n", name.Trim()), ("@d", description));
            else
                Database.Execute("UPDATE Categories SET Name=@n, Description=@d WHERE CategoryId=@id", ("@n", name.Trim()), ("@d", description), ("@id", id));
        }

        public static void DeleteCategory(int id) =>
            Database.Execute("DELETE FROM Categories WHERE CategoryId=@id", ("@id", id));

        // ---------------------------------------------------------------- suppliers

        public static List<LookupItem> GetSuppliers()
        {
            var list = new List<LookupItem>();
            using (var con = Database.Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT SupplierId, Name, IFNULL(Phone,''), IFNULL(Email,''), IFNULL(Address,'') FROM Suppliers ORDER BY Name";
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new LookupItem { Id = r.GetInt32(0), Name = r.GetString(1), Extra1 = r.GetString(2), Extra2 = r.GetString(3), Extra3 = r.GetString(4) });
            }
            return list;
        }

        public static void SaveSupplier(int id, string name, string phone, string email, string address)
        {
            if (id == 0)
                Database.Execute("INSERT INTO Suppliers(Name, Phone, Email, Address) VALUES(@n, @p, @e, @a)",
                    ("@n", name.Trim()), ("@p", phone), ("@e", email), ("@a", address));
            else
                Database.Execute("UPDATE Suppliers SET Name=@n, Phone=@p, Email=@e, Address=@a WHERE SupplierId=@id",
                    ("@n", name.Trim()), ("@p", phone), ("@e", email), ("@a", address), ("@id", id));
        }

        public static void DeleteSupplier(int id) =>
            Database.Execute("DELETE FROM Suppliers WHERE SupplierId=@id", ("@id", id));
    }
}
