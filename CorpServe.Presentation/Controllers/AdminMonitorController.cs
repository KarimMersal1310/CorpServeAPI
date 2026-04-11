using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.AdminDTOs;
using CorpServe.Shared.QueryParams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public async Task<ActionResult<AdminUsersManageDTO>> GetUsers([FromQuery] AdminUserManagementQueryParams queryParams)
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
        public async Task<ActionResult<AdminRequestsManageDTO>> GetRequests([FromQuery] AdminRequestMonitorQueryParams queryParams)
        {
            var result = await _adminMonitorService.GetRequestMonitorAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("slas")]
        public async Task<ActionResult<AdminSlaMonitorDTO>> GetSlas([FromQuery] AdminSlaMonitorQueryParams queryParams)
        {
            MergeSlaMonitorQueryFromRequest(Request, queryParams);
            var result = await _adminMonitorService.GetSlaMonitorAsync(queryParams);
            return HandleResult(result);
        }

        /// <summary>
        /// Ensures camelCase query keys bind reliably (complex-type [FromQuery] can miss nested props with some clients).
        /// </summary>
        private static void MergeSlaMonitorQueryFromRequest(HttpRequest request, AdminSlaMonitorQueryParams queryParams)
        {
            if (TryQueryInt(request.Query, "contractStatus", out var contractStatus))
                queryParams.ContractStatus = contractStatus;
            if (TryQueryInt(request.Query, "slaStatus", out var slaStatus))
                queryParams.SlaStatus = slaStatus;
            if (TryQueryString(request.Query, "categoryId", out var categoryId))
                queryParams.CategoryId = categoryId;
        }

        private static bool TryQueryString(IQueryCollection query, string name, out string value)
        {
            if (query.TryGetValue(name, out var raw) && raw.Count > 0)
            {
                var s = raw.ToString().Trim();
                if (s.Length > 0)
                {
                    value = s;
                    return true;
                }
            }
            value = string.Empty;
            return false;
        }

        private static bool TryQueryInt(IQueryCollection query, string name, out int value)
        {
            if (query.TryGetValue(name, out var raw) && raw.Count > 0 && int.TryParse(raw.ToString(), out value))
                return true;
            value = default;
            return false;
        }
    }
}
