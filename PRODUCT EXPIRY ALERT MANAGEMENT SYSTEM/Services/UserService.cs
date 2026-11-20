using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ExpiryAlertSystem.Data;
using ExpiryAlertSystem.Models;

namespace ExpiryAlertSystem.Services
{
    /// <summary>
    /// User accounts and login.
    ///
    /// Passwords are NEVER stored as plain text. Each password is combined with a random "salt"
    /// and hashed with SHA-256. Only "salt:hash" is saved, so even someone who opens the database
    /// file cannot read the passwords.
    ///
    /// Roles:
    ///   Admin - full access, including managing users and settings
    ///   Staff - can manage products, view alerts and reports
    /// </summary>
    public static class UserService
    {
        public const string DefaultAdminUser = "admin";
        public const string DefaultAdminPassword = "admin123";

        public static void EnsureDefaultAdmin()
        {
            var count = Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Users"));
            if (count > 0) return;
            Create(DefaultAdminUser, "System Administrator", DefaultAdminPassword, "Admin");
        }

        /// <summary>Returns the user if username + password are correct, otherwise null.</summary>
        public static User Login(string username, string password)
        {
            using (var con = Database.Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT UserId, Username, FullName, Role, IsActive, PasswordHash " +
                                  "FROM Users WHERE Username = @u";
                cmd.Parameters.AddWithValue("@u", username.Trim());
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    if (!VerifyPassword(password, r.GetString(5))) return null;
                    var user = Map(r);
                    return user.IsActive ? user : null;
                }
            }
        }

        public static List<User> GetAll()
        {
            var list = new List<User>();
            using (var con = Database.Open())
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT UserId, Username, FullName, Role, IsActive FROM Users ORDER BY Username";
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        public static void Create(string username, string fullName, string password, string role)
        {
            Database.Execute("INSERT INTO Users(Username, FullName, PasswordHash, Role, IsActive, CreatedAt) " +
                             "VALUES(@u, @f, @p, @r, 1, @c)",
                ("@u", username.Trim()), ("@f", fullName.Trim()), ("@p", HashPassword(password)),
                ("@r", role), ("@c", Database.ToDbTime(DateTime.Now)));
        }

        public static void Update(int userId, string fullName, string role, bool isActive)
        {
            Database.Execute("UPDATE Users SET FullName=@f, Role=@r, IsActive=@a WHERE UserId=@id",
                ("@f", fullName.Trim()), ("@r", role), ("@a", isActive ? 1 : 0), ("@id", userId));
        }

        public static void SetPassword(int userId, string newPassword)
        {
            Database.Execute("UPDATE Users SET PasswordHash=@p WHERE UserId=@id",
                ("@p", HashPassword(newPassword)), ("@id", userId));
        }

        public static bool CheckPassword(int userId, string password)
        {
            var hash = Database.Scalar("SELECT PasswordHash FROM Users WHERE UserId=@id", ("@id", userId));
            return hash != null && VerifyPassword(password, hash.ToString());
        }

        public static void Delete(int userId) =>
            Database.Execute("DELETE FROM Users WHERE UserId=@id", ("@id", userId));

        public static int CountActiveAdmins() =>
            Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Users WHERE Role='Admin' AND IsActive=1"));

        // ---------------------------------------------------------------- hashing

        public static string HashPassword(string password)
        {
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);
            return salt + ":" + Sha256(salt + password);
        }

        private static bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split(':');
            return parts.Length == 2 && Sha256(parts[0] + password) == parts[1];
        }

        private static string Sha256(string text)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        private static User Map(Microsoft.Data.Sqlite.SqliteDataReader r) => new User
        {
            UserId = r.GetInt32(0),
            Username = r.GetString(1),
            FullName = r.GetString(2),
            Role = r.GetString(3),
            IsActive = r.GetInt32(4) == 1
        };
    }
}
