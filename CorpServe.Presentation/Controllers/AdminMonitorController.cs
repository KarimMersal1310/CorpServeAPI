using CorpServe.Services.Abstraction;
using CorpServe.Shared;
using CorpServe.Shared.DTOs.AdminDTOs;
using CorpServe.Shared.QueryParams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMonitorController : ApiBaseController
    {
        private readonly IAdminMonitorService _adminMonitorService;

        public AdminMonitorController(IAdminMonitorService adminMonitorService)
        {
            _adminMonitorService = adminMonitorService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<PaginatedResult<AdminUserManagementDTO>>> GetUsers([FromQuery] AdminUserManagementQueryParams queryParams)
        {
            var result = await _adminMonitorService.GetUsersForManagementAsync(queryParams);
            return Ok(result);
        }

        [HttpPost("users/{userId}/suspend")]
        public async Task<ActionResult<bool>> SuspendUser(string userId)
        {
            var result = await _adminMonitorService.SuspendUserAsync(userId);
            return HandleResult(result);
        }

        [HttpPost("users/{userId}/activate")]
        public async Task<ActionResult<bool>> ActivateUser(string userId)
        {
            var result = await _adminMonitorService.ActivateUserAsync(userId);
            return HandleResult(result);
        }

        [HttpGet("requests")]
        public async Task<ActionResult<PaginatedResult<AdminRequestMonitorDTO>>> GetRequests([FromQuery] AdminRequestMonitorQueryParams queryParams)
        {
            var result = await _adminMonitorService.GetRequestMonitorAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("slas")]
        public async Task<ActionResult<AdminSlaMonitorDTO>> GetSlas([FromQuery] AdminSlaMonitorQueryParams queryParams)
        {
            var result = await _adminMonitorService.GetSlaMonitorAsync(queryParams);
            return HandleResult(result);
        }
    }
}
