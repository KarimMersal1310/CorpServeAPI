using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.DashboardDTOs.ClientDTOs;
using CorpServe.Shared.DTOs.DashboardDTOs.VendorDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Presentation.Controllers
{
    public class DashboardController : ApiBaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [Authorize(Roles = "Client")]
        [HttpGet("client")]
        public async Task<ActionResult<ClientDashboardSummaryDTO>> GetClientDashboard()
        {
            var ClientId = GetUserIdFromToken();
            var dashboardData = await _dashboardService.GetClientDashboardSummaryAsync(ClientId);
            return HandleResult(dashboardData);
        }

        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor")]
        public async Task<ActionResult<VendorDashboardSummaryDTO>> GetVendorDashboard()
        {
            var vendorId = GetUserIdFromToken();
            var dashboardData = await _dashboardService.GetVendorDashboardSummaryAsync(vendorId);
            return HandleResult(dashboardData);
        }
    }
}
