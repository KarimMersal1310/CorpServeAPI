using CorpServe.Shared;

namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminRequestsManageDTO
    {
        public AdminRequestsSummaryDTO Summary { get; set; } = new();
        public PaginatedResult<AdminRequestMonitorDTO> Requests { get; set; } = new(1, 1, 0, []);
    }

    public class AdminRequestsSummaryDTO
    {
        public int TotalRequests { get; set; }
        public int ActiveCount { get; set; }
        public int PendingCount { get; set; }
        public int DelayedSlaCount { get; set; }
        public int AvgProgress { get; set; }
        public decimal TotalBudgetMin { get; set; }
        public decimal TotalBudgetMax { get; set; }
    }
}