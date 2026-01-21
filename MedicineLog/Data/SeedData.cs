using MedicineLog.Data.Entities;
using Microsoft.AspNetCore.Identity;
using System.Reflection;

namespace MedicineLog.Data
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Auditor = "Auditor";

        private static readonly Lazy<IReadOnlyList<string>> _all = new(() =>
             typeof(UserRoles)
                 .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                 .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                 .Select(f => (string)f.GetRawConstantValue()!)
                 .ToArray()
            );

        public static IReadOnlyList<string> All => _all.Value;
    }

    class SeedData
    {
        public static async Task Seed(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            await SeedRoles(roleManager);
            await SeedAdminUser(userManager, context);
            await context.SaveChangesAsync();
        }

        static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in UserRoles.All)
            {
                var roleName = role.ToString();
                if (roleName != null && !await roleManager.RoleExistsAsync(roleName))
                {
                    var identityRole = new IdentityRole(roleName);
                    await roleManager.CreateAsync(identityRole);
                }
            }
        }

        static async Task SeedAdminUser(UserManager<AppUser> userManager, AppDbContext context)
        {
            if (!context.Users.Any())
            {
                var user = new AppUser("admin@insupport.se");
                var createUserResult = await userManager.CreateAsync(user, "Password1!");
                if (!createUserResult.Succeeded)
                    throw new Exception("Failed to seed database with admin user.");

                await userManager.AddToRoleAsync(user, UserRoles.Admin);
            }
        }
    }
}
