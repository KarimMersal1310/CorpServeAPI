using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.VendorVerify;
using EventHub.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CorpServe.Presentation.Controllers
{
     [Authorize(Roles = "Admin")]
    public class AdminVendorController : ApiBaseController
    {
        private readonly IAdminVendorService _adminVendorService;

        public AdminVendorController(IAdminVendorService adminVendorService)
        {
            _adminVendorService = adminVendorService;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<VendorVerifyDTO>>> GetPendingVerifications()
        {
            var result = await _adminVendorService.GetPendingVerificationsAsync();
            return HandleResult(result);
        }

        [HttpPost("approve/{id}")]
        public async Task<ActionResult<bool>> Approve(string id)
        {
            var adminId = GetUserIdFromToken() ?? "Admin"; // Fallback if no specific admin identity
            var result = await _adminVendorService.ApproveVerificationAsync(id, adminId);
            return HandleResult(result);
        }

        [HttpPost("reject/{id}")]
        public async Task<ActionResult<bool>> Reject(string id, [FromBody] RejectVerificationRequestDTO request)
        {
            var adminId = GetUserIdFromToken() ?? "Admin"; // Fallback if no specific admin identity
            var result = await _adminVendorService.RejectVerificationAsync(id, adminId, request.RejectReason);
            return HandleResult(result);
        }
    }
}
