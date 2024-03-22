using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace PlataformaEDUGEP
{
    public static class Configurations
    {
        public static async Task CreateStartingRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] rolesNames = { "Teacher", "Student" };
            IdentityResult result;
            foreach (var namesRole in rolesNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(namesRole);
                if (!roleExist)
                {
                    result = await roleManager.CreateAsync(new IdentityRole(namesRole));
                }
            }
        }
    }
}
