namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminSlaContractMonitorDTO
    {
        public string SlaContractId { get; set; } = default!;
        public string RequestId { get; set; } = default!;
        public string RequestTitle { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }
        public string SlaStatus { get; set; } = string.Empty;
        public string WarningLevel { get; set; } = string.Empty;
    }
}
