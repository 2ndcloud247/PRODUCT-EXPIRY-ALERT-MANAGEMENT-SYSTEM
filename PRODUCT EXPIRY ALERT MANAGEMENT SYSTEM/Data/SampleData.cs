using System;
using ExpiryAlertSystem.Models;
using ExpiryAlertSystem.Services;

namespace ExpiryAlertSystem.Data
{
    /// <summary>
    /// Demo data inserted ONLY the first time the program runs (when the database file is new),
    /// so the dashboard and alerts have something to show during a presentation.
    /// Expiry dates are calculated from today's date, so there are always some expired,
    /// critical, expiring-soon and safe products. Delete them any time from the Products screen.
    /// </summary>
    internal static class SampleData
    {
        public static void Insert()
        {
            string[] categories = { "Drugs & Medicine", "Dairy Products", "Beverages", "Canned Foods", "Bakery", "Cosmetics", "Grains & Cereals" };
            foreach (var c in categories) LookupService.SaveCategory(0, c, null);

            LookupService.SaveSupplier(0, "Emzor Pharmaceuticals", "08030000001", "sales@emzor.example", "Lagos");
            LookupService.SaveSupplier(0, "Nestle Nigeria", "08030000002", "orders@nestle.example", "Lagos");
            LookupService.SaveSupplier(0, "Rivers Distribution Ltd", "08030000003", "info@riversdist.example", "Port Harcourt");
            LookupService.SaveSupplier(0, "Fidson Healthcare", "08030000004", "contact@fidson.example", "Ogun");

            var cats = LookupService.GetCategories();
            var sups = LookupService.GetSuppliers();
            int Cat(string name) => cats.Find(x => x.Name == name).Id;
            int Sup(string name) => sups.Find(x => x.Name == name).Id;

            void Add(string name, string cat, string sup, string batch, int qty, double price, int expiresInDays, string loc)
            {
                var exp = DateTime.Today.AddDays(expiresInDays);
                ProductService.Save(new Product
                {
                    Name = name,
                    CategoryId = Cat(cat),
                    SupplierId = Sup(sup),
                    BatchNumber = batch,
                    Barcode = "615" + Math.Abs((name + batch).GetHashCode() % 1000000000).ToString("D9"),
                    Quantity = qty,
                    UnitPrice = price,
                    ManufactureDate = exp.AddMonths(-18),
                    ExpiryDate = exp,
                    Location = loc
                });
            }

            // Expired
            Add("Paracetamol 500mg Tablets", "Drugs & Medicine", "Emzor Pharmaceuticals", "EMZ-2301", 120, 350, -12, "Shelf A1");
            Add("Peak Evaporated Milk 160g", "Dairy Products", "Nestle Nigeria", "PK-7781", 48, 450, -3, "Shelf C2");
            Add("Sliced Bread (Family Loaf)", "Bakery", "Rivers Distribution Ltd", "BR-0012", 15, 1200, -1, "Front Counter");
            // Critical (within 7 days)
            Add("Amoxicillin 250mg Capsules", "Drugs & Medicine", "Fidson Healthcare", "FID-4410", 60, 1500, 2, "Shelf A2");
            Add("Fresh Yoghurt 500ml", "Dairy Products", "Nestle Nigeria", "YG-3321", 30, 900, 5, "Fridge 1");
            Add("Orange Juice 1L", "Beverages", "Rivers Distribution Ltd", "OJ-9902", 24, 1300, 0, "Shelf D1");
            // Expiring soon (within 30 days)
            Add("Vitamin C 1000mg", "Drugs & Medicine", "Emzor Pharmaceuticals", "EMZ-2350", 200, 2500, 14, "Shelf A3");
            Add("Milo Chocolate Drink 400g", "Beverages", "Nestle Nigeria", "ML-1180", 36, 3200, 21, "Shelf D2");
            Add("Baked Beans 415g", "Canned Foods", "Rivers Distribution Ltd", "BB-5560", 50, 1100, 28, "Shelf B1");
            // Safe
            Add("Cough Syrup 100ml", "Drugs & Medicine", "Fidson Healthcare", "FID-4502", 80, 1800, 95, "Shelf A4");
            Add("Golden Penny Spaghetti 500g", "Grains & Cereals", "Rivers Distribution Ltd", "GP-2201", 150, 800, 240, "Shelf E1");
            Add("Body Lotion 400ml", "Cosmetics", "Rivers Distribution Ltd", "BL-7710", 40, 2700, 400, "Shelf F1");
            Add("Corn Flakes 500g", "Grains & Cereals", "Nestle Nigeria", "CF-3390", 45, 2900, 160, "Shelf E2");
            Add("Sardines in Oil 125g", "Canned Foods", "Rivers Distribution Ltd", "SD-8812", 70, 950, 520, "Shelf B2");

            ActivityLogService.Log("Setup", "Database created with sample data");
        }
    }
}
