using MedicineLog.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace MedicineLog.Data
{
    public enum UserRoles { Admin, Auditor };

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
            foreach (var role in Enum.GetValues(typeof(UserRoles)))
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
                var user = new AppUser("insupport");
                var createUserResult = await userManager.CreateAsync(user, "Pass1!");
                if (!createUserResult.Succeeded)
                    throw new Exception("Failed to seed database with admin user.");

                await userManager.AddToRoleAsync(user, UserRoles.Admin.ToString());
            }
        }
    }
}
