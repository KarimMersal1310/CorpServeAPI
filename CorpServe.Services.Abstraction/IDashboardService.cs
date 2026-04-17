using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.DashboardDTOs.ClientDTOs;
using CorpServe.Shared.DTOs.DashboardDTOs.VendorDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Services.Abstraction
{
    public interface IDashboardService
    {
        Task<Result<ClientDashboardSummaryDTO>> GetClientDashboardSummaryAsync(string clientId);
        Task<Result<VendorDashboardSummaryDTO>> GetVendorDashboardSummaryAsync(string vendorId);

    }
}
