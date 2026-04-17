namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminSlaContractMonitorDTO
    {
        public string SlaContractId { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string ClientId { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string RequestTitle { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string? ClientProfilePictureUrl { get; set; }
        public string? VendorProfilePictureUrl { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }
        public string SlaStatus { get; set; } = string.Empty;
        public string WarningLevel { get; set; } = string.Empty;
        /// <summary>none | low | medium | high — for dashboards</summary>
        public string WarningLevelUi { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int RequestProgress { get; set; }
        public int DaysRemaining { get; set; }
        /// <summary>in-progress | breached | delayed | completed</summary>
        public string ContractStatus { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>active | breached | delayed | completed — aligns with SLA Monitor UI</summary>
        public string SlaUiStatus { get; set; } = string.Empty;
    }
}
