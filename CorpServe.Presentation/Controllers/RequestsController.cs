using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using CorpServe.Shared.QueryParams;
using EventHub.Presentation.Controllers;
using EventHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    [Authorize(Roles = "Client")]
    public class RequestsController : ApiBaseController
    {
        private readonly IRequestService _requestService;

        public RequestsController(IRequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RequestDTO>> Create([FromForm] CreateRequestDTO request)
        {
            var result = await _requestService.CreateRequestAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }

        [HttpPost("generate-estimate")]
        public async Task<ActionResult<AIEstimationDTO>> GenerateEstimate([FromBody] GenerateRequestEstimateDTO request)
        {
            var result = await _requestService.GenerateEstimateAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }

        [HttpGet("my-requests")]
        public async Task<ActionResult<PaginatedResult<RequestDTO>>> GetMyRequests([FromQuery] RequestQueryParams queryParams)
        {
            var result = await _requestService.GetClientRequestsAsync(GetUserIdFromToken(), queryParams);
            return Ok(result);
        }
    }
}
