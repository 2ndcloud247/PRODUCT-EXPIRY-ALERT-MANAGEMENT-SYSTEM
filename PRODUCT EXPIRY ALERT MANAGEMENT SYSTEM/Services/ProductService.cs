using System;
using System.Collections.Generic;
using System.Linq;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Models;
using Microsoft.Data.Sqlite;

namespace ExpiryAlertSystem.Services
{
    /// <summary>
    /// Create, read, update and delete (CRUD) operations for products.
    /// All SQL uses parameters (@name) which protects the database from SQL-injection.
    /// </summary>
    public static class ProductService
    {
        private const string SelectSql = @"
SELECT p.ProductId, p.Name, p.CategoryId, c.Name, p.SupplierId, s.Name, p.BatchNumber, p.Barcode,
       p.Quantity, p.UnitPrice, p.ManufactureDate, p.ExpiryDate, p.Location, p.Status, p.Notes
FROM Products p
LEFT JOIN Categories c ON c.CategoryId = p.CategoryId
LEFT JOIN Suppliers  s ON s.SupplierId = p.SupplierId";

        /// <summary>Returns all products, optionally filtered by a search word and category.</summary>
        public static List<Product> GetAll(string search = null, int? categoryId = null, bool includeDisposed = true)
        {
            var where = new List<string>();
            var prms = new List<(string, object)>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Add("(p.Name LIKE @s OR p.BatchNumber LIKE @s OR p.Barcode LIKE @s OR s.Name LIKE @s OR p.Location LIKE @s)");
                prms.Add(("@s", "%" + search.Trim() + "%"));
            }
            if (categoryId.HasValue)
            {
                where.Add("p.CategoryId = @c");
                prms.Add(("@c", categoryId.Value));
            }
            if (!includeDisposed) where.Add("p.Status = 'Active'");

            var sql = SelectSql + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + " ORDER BY p.ExpiryDate";
            return Query(sql, prms.ToArray());
        }

        public static Product GetById(int id) =>
            Query(SelectSql + " WHERE p.ProductId = @id", ("@id", id)).FirstOrDefault();

        /// <summary>Insert when ProductId is 0, otherwise update. Returns the product id.</summary>
        public static int Save(Product p)
        {
            var now = Database.ToDbTime(DateTime.Now);
            var prms = new (string, object)[]
            {
                ("@name", p.Name.Trim()), ("@cat", p.CategoryId), ("@sup", p.SupplierId),
                ("@batch", p.BatchNumber), ("@bar", p.Barcode), ("@qty", p.Quantity), ("@price", p.UnitPrice),
                ("@mfg", p.ManufactureDate.HasValue ? Database.ToDb(p.ManufactureDate.Value) : null),
                ("@exp", Database.ToDb(p.ExpiryDate)), ("@loc", p.Location), ("@status", p.Status ?? "Active"),
                ("@notes", p.Notes), ("@now", now), ("@id", p.ProductId)
            };

            using (var con = Database.Open())
            {
                if (p.ProductId == 0)
                {
                    Database.Execute(con, @"INSERT INTO Products
                        (Name, CategoryId, SupplierId, BatchNumber, Barcode, Quantity, UnitPrice, ManufactureDate,
                         ExpiryDate, Location, Status, Notes, CreatedAt, UpdatedAt)
                        VALUES(@name, @cat, @sup, @batch, @bar, @qty, @price, @mfg, @exp, @loc, @status, @notes, @now, @now)", prms);
                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = "SELECT last_insert_rowid()";
                        p.ProductId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                else
                {
                    Database.Execute(con, @"UPDATE Products SET
                        Name=@name, CategoryId=@cat, SupplierId=@sup, BatchNumber=@batch, Barcode=@bar, Quantity=@qty,
                        UnitPrice=@price, ManufactureDate=@mfg, ExpiryDate=@exp, Location=@loc, Status=@status,
                        Notes=@notes, UpdatedAt=@now
                        WHERE ProductId=@id", prms);

                    // The expiry date may have changed, so old alerts for this product are cleared
                    // and the alert engine re-evaluates it on the next check.
                    Database.Execute(con, "DELETE FROM AlertLog WHERE ProductId=@id AND IsAcknowledged=0", ("@id", p.ProductId));
                }
            }
            return p.ProductId;
        }

        public static void Delete(int id) =>
            Database.Execute("DELETE FROM Products WHERE ProductId=@id", ("@id", id));

        /// <summary>
        /// "Dispose" = the expired product was removed from the shelf and destroyed/returned.
        /// It stays in the database for records but is no longer monitored for alerts.
        /// </summary>
        public static void MarkDisposed(int id)
        {
            Database.Execute("UPDATE Products SET Status='Disposed', UpdatedAt=@now WHERE ProductId=@id",
                ("@now", Database.ToDbTime(DateTime.Now)), ("@id", id));
            Database.Execute("UPDATE AlertLog SET IsAcknowledged=1, AcknowledgedBy=@u, AcknowledgedAt=@now " +
                             "WHERE ProductId=@id AND IsAcknowledged=0",
                ("@u", Session.Username), ("@now", Database.ToDbTime(DateTime.Now)), ("@id", id));
        }

        // ---------------------------------------------------------------- mapping

        private static List<Product> Query(string sql, params (string, object)[] prms)
        {
            var list = new List<Product>();
            using (var con = Database.Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = sql;
                Database.AddParameters(cmd, prms);
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        private static Product Map(SqliteDataReader r) => new Product
        {
            ProductId = r.GetInt32(0),
            Name = r.GetString(1),
            CategoryId = r.IsDBNull(2) ? (int?)null : r.GetInt32(2),
            CategoryName = r.IsDBNull(3) ? "" : r.GetString(3),
            SupplierId = r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
            SupplierName = r.IsDBNull(5) ? "" : r.GetString(5),
            BatchNumber = r.IsDBNull(6) ? "" : r.GetString(6),
            Barcode = r.IsDBNull(7) ? "" : r.GetString(7),
            Quantity = r.GetInt32(8),
            UnitPrice = r.GetDouble(9),
            ManufactureDate = r.IsDBNull(10) ? (DateTime?)null : Database.FromDb(r.GetString(10)),
            ExpiryDate = Database.FromDb(r.GetString(11)),
            Location = r.IsDBNull(12) ? "" : r.GetString(12),
            Status = r.GetString(13),
            Notes = r.IsDBNull(14) ? "" : r.GetString(14)
        };
    }
}
