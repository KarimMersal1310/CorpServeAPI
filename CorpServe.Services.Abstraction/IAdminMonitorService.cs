using CorpServe.Shared;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AdminDTOs;
using CorpServe.Shared.QueryParams;

namespace CorpServe.Services.Abstraction
{
    public interface IAdminMonitorService
    {
        Task<PaginatedResult<AdminUserManagementDTO>> GetUsersForManagementAsync(AdminUserManagementQueryParams queryParams);
        Task<Result<bool>> SuspendUserAsync(string userId);
        Task<Result<bool>> ActivateUserAsync(string userId);
        Task<PaginatedResult<AdminRequestMonitorDTO>> GetRequestMonitorAsync(AdminRequestMonitorQueryParams queryParams);
        Task<Result<AdminSlaMonitorDTO>> GetSlaMonitorAsync(AdminSlaMonitorQueryParams queryParams);
    }
}
