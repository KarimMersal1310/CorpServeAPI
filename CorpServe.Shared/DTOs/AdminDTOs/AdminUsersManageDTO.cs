using CorpServe.Shared;

namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminUsersManageDTO
    {
        public AdminUsersSummaryDTO Summary { get; set; } = new();
        public PaginatedResult<AdminUserManagementDTO> Users { get; set; } = new(1, 1, 0, []);
    }

    public class AdminUsersSummaryDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveCount { get; set; }
        public int SuspendedCount { get; set; }
        public int ClientsCount { get; set; }
        public int VendorsCount { get; set; }
    }
}