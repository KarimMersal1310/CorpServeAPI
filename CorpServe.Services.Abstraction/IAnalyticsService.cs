using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AnalyticsDTOs;
using System.Threading.Tasks;

namespace CorpServe.Services.Abstraction
{
    public interface IAnalyticsService
    {
        #region Admin Analytics

        Task<Result<AdminAnalyticsDashboardDTO>> GetAdminAnalyticsAsync(AnalyticsFilterDTO filter);

        #endregion

        #region Client Analytics

        Task<Result<ClientAnaltyicsDashboardDTO>> GetClientAnalyticsAsync(string clientId, AnalyticsFilterDTO filter);

        #endregion

        #region Vendor Analytics

        Task<Result<VendorAnalyticsDashboardDTO>> GetVendorAnalyticsAsync(string vendorId, AnalyticsFilterDTO filter);

        #endregion
    }
}
