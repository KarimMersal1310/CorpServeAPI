using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Presistence.Data.DataSeed
{
    public class IdentityDataInitializer : IDataInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<IdentityDataInitializer> _logger;

        public IdentityDataInitializer(UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            ILogger<IdentityDataInitializer> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }
        public async Task InitializeAsync()
        {
            try
            {
                if (!_roleManager.Roles.Any())
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    await _roleManager.CreateAsync(new IdentityRole("Client"));
                    await _roleManager.CreateAsync(new IdentityRole("Vendor"));
                }
                if (!_userManager.Users.Any())
                {
                    var adminUser = new ApplicationUser
                    {
                        FullName = "Admin_CorpServe",
                        Email = "admin@corpserve.com",
                        UserName = "admin",
                        PhoneNumber = "01129773714",
                        Status = UserStatus.Active
                    };
                    var result = await _userManager.CreateAsync(adminUser, "Admin@123");
                    if(result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                    else{
                        _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding identity data.");
            }
        }
    }
}

