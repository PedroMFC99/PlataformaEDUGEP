using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration; // Add this for IConfiguration
using PlataformaEDUGEP.Models;
using System;
using System.Threading.Tasks;

namespace PlataformaEDUGEP
{
    public static class Configurations
    {
        public static async Task CreateStartingRoles(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] rolesNames = { "Teacher", "Student", "Admin" };
            IdentityResult result;

            foreach (var namesRole in rolesNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(namesRole);
                if (!roleExist)
                {
                    result = await roleManager.CreateAsync(new IdentityRole(namesRole));
                }
            }

            // Pass IConfiguration to EnsureAdminUser
            await EnsureAdminUser(serviceProvider, configuration);
        }

        private static async Task EnsureAdminUser(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Retrieve admin info from configuration
            var adminEmail = configuration["AdminUserInfo:Email"];
            var adminPassword = configuration["AdminUserInfo:Password"];
            var adminFullName = configuration["AdminUserInfo:FullName"];

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = adminFullName,
                    EmailConfirmed = true
                };

                var createUserResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createUserResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
