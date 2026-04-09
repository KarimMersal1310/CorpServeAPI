namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminSlaMonitorDTO
    {
        public int TotalSlaContracts { get; set; }
        public int InProgressCount { get; set; }
        public int DelayedCount { get; set; }
        public int CompletedCount { get; set; }
        public CorpServe.Shared.PaginatedResult<AdminSlaContractMonitorDTO> Contracts { get; set; } = default!;
    }
}
