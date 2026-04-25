using CorpServe.Shared;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AdminDTOs;
using CorpServe.Shared.QueryParams;

namespace CorpServe.Services.Abstraction
{
    public interface IAdminMonitorService
    {
        Task<AdminUsersManageDTO> GetUsersForManagementAsync(AdminUserManagementQueryParams queryParams);
        Task<Result<bool>> SuspendUserAsync(string userId, string? reason = null);
        Task<Result<bool>> ActivateUserAsync(string userId);
        Task<AdminRequestsManageDTO> GetRequestMonitorAsync(AdminRequestMonitorQueryParams queryParams);
        Task<Result<AdminSlaMonitorDTO>> GetSlaMonitorAsync(AdminSlaMonitorQueryParams queryParams);
    }
}
