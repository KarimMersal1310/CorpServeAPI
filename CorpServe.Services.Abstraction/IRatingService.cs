using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.RatingDTOs;

namespace CorpServe.Services.Abstraction
{
    public interface IRatingService
    {
        Task<Result<RatingRequirementDTO>> GetRatingRequirementForRequestAsync(string clientId, string requestId);
        Task<Result<IEnumerable<RatingRequirementDTO>>> GetPendingRatingsForClientAsync(string clientId);
        Task<Result<RatingSummaryDTO>> SubmitRatingAsync(string clientId, string requestId, SubmitRatingDTO dto);
    }
}
