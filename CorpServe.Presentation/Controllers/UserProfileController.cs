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
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(
            IUserPreferenceService userPreferenceService,
            IAuthenticationService authenticationService,
            IUserProfileService userProfileService)
        {
            _userPreferenceService = userPreferenceService;
            _authenticationService = authenticationService;
            _userProfileService = userProfileService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDTO>> GetCurrentUserProfileAsync()
        {
            var result = await _authenticationService.GetUserProfileAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [HttpGet("me/details")]
        public async Task<ActionResult<UserProfileDetailsDTO>> GetMyDetailedProfileAsync()
        {
            var result = await _userProfileService.GetMyProfileAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserProfileDetailsDTO>> GetUserProfileAsync([FromRoute] string userId)
        {
            var result = await _userProfileService.GetUserProfileAsync(GetUserIdFromToken(), userId);
            return HandleResult(result);
        }

        [HttpPost("profile")]
        public async Task<ActionResult<bool>> UpsertProfileAsync([FromForm] UpsertUserProfileDTO upsertUserProfileDTO)
        {
            var result = await _userProfileService.UpsertProfileAsync(GetUserIdFromToken(), upsertUserProfileDTO);
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
