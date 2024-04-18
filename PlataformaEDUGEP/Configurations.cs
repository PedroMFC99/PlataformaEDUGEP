using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using PlataformaEDUGEP.Models;
using System;
using System.Threading.Tasks;

namespace PlataformaEDUGEP
{
    /// <summary>
    /// Handles initial configuration tasks for roles and admin user setup.
    /// </summary>
    public static class Configurations
    {
        /// <summary>
        /// Creates default roles within the application if they do not already exist.
        /// </summary>
        /// <param name="serviceProvider">An instance of IServiceProvider to resolve service instances.</param>
        /// <param name="configuration">An instance of IConfiguration to access application settings.</param>
        /// <returns>A task representing the asynchronous operation of creating roles.</returns>
        /// <remarks>
        /// This method iteratively checks for the existence of each role and creates them if they are not found.
        /// It relies on RoleManager and UserManager services provided via dependency injection.
        /// </remarks>
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

            // Call method to ensure an admin user is setup
            await EnsureAdminUser(serviceProvider, configuration);
        }

        /// <summary>
        /// Ensures that an admin user is created and assigned the Admin role.
        /// </summary>
        /// <param name="serviceProvider">An instance of IServiceProvider to resolve service instances.</param>
        /// <param name="configuration">An instance of IConfiguration to access admin user settings.</param>
        /// <returns>A task representing the asynchronous operation of creating or verifying the admin user.</returns>
        /// <remarks>
        /// This method checks for the existence of an admin email from configuration, and if the user does not exist,
        /// it creates the user and assigns them the Admin role. It utilizes UserManager for user operations.
        /// </remarks>
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