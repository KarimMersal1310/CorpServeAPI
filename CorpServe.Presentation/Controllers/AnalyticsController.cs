using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AnalyticsDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    public class AnalyticsController : ApiBaseController
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<AdminAnalyticsDashboardDTO>> GetAdminAnalytics(
            [FromQuery] AnalyticsDateRangePreset rangePreset = AnalyticsDateRangePreset.Last30Days,
            [FromQuery] DateTime? startDateUtc = null,
            [FromQuery] DateTime? endDateUtc = null)
        {
            var filter = new AnalyticsFilterDTO
            {
                RangePreset = rangePreset,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc
            };

            var result = await _analyticsService.GetAdminAnalyticsAsync(filter);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("client")]
        public async Task<ActionResult<ClientAnaltyicsDashboardDTO>> GetClientAnalytics(
            [FromQuery] AnalyticsDateRangePreset rangePreset = AnalyticsDateRangePreset.Last30Days,
            [FromQuery] DateTime? startDateUtc = null,
            [FromQuery] DateTime? endDateUtc = null)
        {
            var clientId = GetUserIdFromToken();
            var filter = new AnalyticsFilterDTO
            {
                RangePreset = rangePreset,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc
            };

            var result = await _analyticsService.GetClientAnalyticsAsync(clientId, filter);
            return HandleResult(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor")]
        public async Task<ActionResult<VendorAnalyticsDashboardDTO>> GetVendorAnalytics(
            [FromQuery] AnalyticsDateRangePreset rangePreset = AnalyticsDateRangePreset.Last30Days,
            [FromQuery] DateTime? startDateUtc = null,
            [FromQuery] DateTime? endDateUtc = null)
        {
            var vendorId = GetUserIdFromToken();
            var filter = new AnalyticsFilterDTO
            {
                RangePreset = rangePreset,
                StartDateUtc = startDateUtc,
                EndDateUtc = endDateUtc
            };

            var result = await _analyticsService.GetVendorAnalyticsAsync(vendorId, filter);
            return HandleResult(result);
        }
    }
}
