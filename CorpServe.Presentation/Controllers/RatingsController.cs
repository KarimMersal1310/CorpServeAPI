using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.RatingDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    public class RatingsController : ApiBaseController
    {
        private readonly IRatingService _ratingService;

        public RatingsController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [Authorize(Roles = "Client")]
        [HttpGet("requests/{requestId}/required")]
        public async Task<ActionResult<RatingRequirementDTO>> GetRatingRequired(string requestId)
        {
            var result = await _ratingService.GetRatingRequirementForRequestAsync(GetUserIdFromToken(), requestId);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("my/pending")]
        public async Task<ActionResult<IEnumerable<RatingRequirementDTO>>> GetMyPendingRatings()
        {
            var result = await _ratingService.GetPendingRatingsForClientAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpPost("requests/{requestId}")]
        public async Task<ActionResult<RatingSummaryDTO>> SubmitRating(string requestId, [FromBody] SubmitRatingDTO dto)
        {
            var result = await _ratingService.SubmitRatingAsync(GetUserIdFromToken(), requestId, dto);
            return HandleResult(result);
        }
    }
}
