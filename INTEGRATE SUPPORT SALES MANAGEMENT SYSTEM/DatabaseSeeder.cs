using Microsoft.EntityFrameworkCore;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using System; // Kept this using statement as Console and DateTime are used, ensuring the code remains syntactically correct.

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM;

public class DatabaseSeeder
{
    public static void SeedSuperAdmin(ApplicationDbContext context)
    {
        const string superAdminEmail = "ivycarlb@gmail.com";
        const string superAdminPassword = "ivycarlmercadobenjamin012004";

        // Check if SuperAdmin already exists
        var existingSuperAdmin = context.Users.FirstOrDefault(u => u.Email == superAdminEmail);

        if (existingSuperAdmin == null)
        {
            Console.WriteLine("Creating SuperAdmin user...");

            var superAdmin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = superAdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(superAdminPassword),
                Phone = "9999999999",
                Role = UserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(superAdmin);
            context.SaveChanges();

            Console.WriteLine($"SuperAdmin created successfully with ID: {superAdmin.Id}");
        }
        else
        {
            if (!BCrypt.Net.BCrypt.Verify(superAdminPassword, existingSuperAdmin.PasswordHash))
            {
                existingSuperAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(superAdminPassword);
                context.SaveChanges();
                Console.WriteLine($"SuperAdmin password updated for ID: {existingSuperAdmin.Id}");
            }
            else
            {
                Console.WriteLine($"SuperAdmin already exists with ID: {existingSuperAdmin.Id}");
            }
        }
    }
}
