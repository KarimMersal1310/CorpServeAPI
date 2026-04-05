using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AuthDTOs;
using CorpServe.Shared.DTOs.UserPreferenceDTOs;
using CorpServe.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    [Authorize]
    public class UserProfileController : ApiBaseController
    {
        private readonly IUserPreferenceService _userPreferenceService;
        private readonly IAuthenticationService _authenticationService;

        public UserProfileController(IUserPreferenceService userPreferenceService ,IAuthenticationService authenticationService)
        {
            _userPreferenceService = userPreferenceService;
            _authenticationService = authenticationService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDTO>> GetCurrentUserProfileAsync()
        {
            var result = await _authenticationService.GetUserProfileAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [Authorize]        
        [HttpPost("update-user")]
        public async Task<ActionResult<bool>> UpdateUserAsync(UpdateUserDTO updateUserDTO)
        {
            var result = await _authenticationService.UpdateUserAsync(GetUserIdFromToken(), updateUserDTO);
            return HandleResult(result);
        }
        [HttpPost("change-password")]
        public async Task<ActionResult<bool>> ChangePasswordAsync(ChangePasswordDTO changePasswordDTO)
        {
            var result = await _authenticationService.ChangePasswordAsync(GetUserIdFromToken(), changePasswordDTO);
            return HandleResult(result);
        }

        [HttpGet("user-preference")]
        public async Task<ActionResult<UserPreferenceDTO>> GetPreferences()
        {
            var result = await _userPreferenceService.GetPreferenceAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [HttpPost("update-user-preference")]
        public async Task<ActionResult<bool>> UpdatePreferences([FromBody] UserPreferenceDTO preferenceDTO)
        {
            var result = await _userPreferenceService.UpdatePreferenceAsync(GetUserIdFromToken(), preferenceDTO);
            return HandleResult(result);
        }
    }
}
