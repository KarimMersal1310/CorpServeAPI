using CorpServe.Shared.DTOs.UserPreferenceDTOs;
using E_Commerce.Shared.CommonResult;

namespace CorpServe.Services.Abstraction
{
    public interface IUserPreferenceService
    {
        Task<Result<UserPreferenceDTO>> GetPreferenceAsync(string userId);
        Task<Result<bool>> UpdatePreferenceAsync(string userId, UserPreferenceDTO preferenceDTO);
    }
}
