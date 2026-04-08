namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class ActiveRequestDTO
    {
        public string RequestId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
        public DateTime Deadline { get; set; }
        /// <summary>Human-readable remaining / overdue text (e.g. "5 days left", "38 hours left", "Overdue by 1 day").</summary>
        public string RemainingTimeDisplay { get; set; } = default!;
        public int ProgressPercentage { get; set; }
        public string? ClientName { get; set; }
        public string? VendorName { get; set; }
        /// <summary>Display label for the task (e.g. In Progress, Delayed).</summary>
        public string TaskState { get; set; } = default!;
        /// <summary>SLA health label aligned with list filters (On Track, Warning, Delayed, Blocked).</summary>
        public string SlaLabel { get; set; } = default!;
        /// <summary>Most recent vendor progress note, if any.</summary>
        public string? LatestWorkUpdate { get; set; }
    }
}
