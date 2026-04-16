using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AuthDTOs;

namespace CorpServe.Services.Abstraction
{
    public interface IUserProfileService
    {
        Task<Result<UserProfileDetailsDTO>> GetMyProfileAsync(string userId);
        Task<Result<UserProfileDetailsDTO>> GetUserProfileAsync(string requesterUserId, string targetUserId);
        Task<Result<bool>> UpsertProfileAsync(string userId, UpsertUserProfileDTO upsertUserProfileDTO);
    }
}
