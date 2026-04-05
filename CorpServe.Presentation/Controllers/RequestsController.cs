using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Presentation.Controllers;
using CorpServe.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    
    public class RequestsController : ApiBaseController
    {
        private readonly IRequestService _requestService;

        public RequestsController(IRequestService requestService)
        {
            _requestService = requestService;
        }
        [Authorize(Roles = "Client")]
        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RequestDTO>> Create([FromForm] CreateRequestDTO request)
        {
            var result = await _requestService.CreateRequestAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpPut("{requestId}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RequestDTO>> Update(string requestId, [FromForm] UpdateRequestDTO request)
        {
            var result = await _requestService.UpdateRequestAsync(GetUserIdFromToken(), requestId, request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpDelete("{requestId}")]
        public async Task<ActionResult<bool>> Delete(string requestId)
        {
            var result = await _requestService.DeleteRequestAsync(GetUserIdFromToken(), requestId);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpPost("generate-estimate")]
        public async Task<ActionResult<AIEstimationDTO>> GenerateEstimate([FromBody] GenerateRequestEstimateDTO request)
        {
            var result = await _requestService.GenerateEstimateAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }
        [Authorize(Roles = "Client")]
        [HttpGet("my-requests")]
        public async Task<ActionResult<PaginatedResult<RequestDTO>>> GetMyRequests([FromQuery] RequestQueryParams queryParams)
        {
            var result = await _requestService.GetClientRequestsAsync(GetUserIdFromToken(), queryParams);
            return Ok(result);
        }
        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor-requests")]
        public async Task<ActionResult<PaginatedResult<VendorRequestViewDTO>>> GetVendorRequests([FromQuery] RequestQueryParams queryParams)
        {
            var result = await _requestService.GetRequestsForVendor(GetUserIdFromToken(), queryParams);
            return Ok(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpPut("{requestId}/progress")]
        public async Task<ActionResult<bool>> UpdateProgress(string requestId, [FromBody] UpdateRequestProgressDTO request)
        {
            var result = await _requestService.VendorUpdateRequestProgressAsync(GetUserIdFromToken(), requestId, request);
            return HandleResult(result);
        }
    }
}
