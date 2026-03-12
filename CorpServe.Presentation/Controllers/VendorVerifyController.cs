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
    [Authorize(Roles = "Vendor")]
    public class VendorVerifyController : ApiBaseController
    {
        private readonly IVendorVerifyService _vendorVerifyService;

        public VendorVerifyController(IVendorVerifyService vendorVerifyService)
        {
            _vendorVerifyService = vendorVerifyService;
        }

        [HttpPost("submit")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<VendorVerifyDTO>> SubmitVerification([FromForm] VendorVerifyRequestDTO request)
        {
            var vendorId = GetUserIdFromToken();
            var result = await _vendorVerifyService.SubmitVerificationAsync(vendorId, request);
            return HandleResult(result);
        }

        [HttpGet("status")]
        public async Task<ActionResult<VendorVerifyDTO>> GetStatus()
        {
            var vendorId = GetUserIdFromToken();
            var result = await _vendorVerifyService.GetVendorVerificationStatusAsync(vendorId);
            return HandleResult(result);
        }
    }
}
