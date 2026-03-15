using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.UserPreferenceDTOs;
using E_Commerce.Shared.CommonResult;
using Microsoft.AspNetCore.Identity;
using CorpServe.Domain.Entities.IdentityModule;

namespace CorpServe.Services
{
    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserPreferenceService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<UserPreferenceDTO>> GetPreferenceAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            user.UserPreference ??= new UserPreference();

            return new UserPreferenceDTO
            {
                EmailNotification = user.UserPreference.EmailNotification,
                SystemNotification = user.UserPreference.SystemNotification
            };
        }

        public async Task<Result<bool>> UpdatePreferenceAsync(string userId, UserPreferenceDTO preferenceDTO)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Error.NotFound("User.NotFound", "User not found.");

            user.UserPreference ??= new UserPreference();
            user.UserPreference.EmailNotification = preferenceDTO.EmailNotification;
            user.UserPreference.SystemNotification = preferenceDTO.SystemNotification;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return updateResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

            return true;
        }
    }
}
