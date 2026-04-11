namespace CorpServe.Shared.QueryParams
{
    public class AdminSlaMonitorQueryParams : BaseQueryParams
    {
        public int? SlaStatus { get; set; }
        public string? CategoryId { get; set; }
        /// <summary>Matches SLAStatus: 1=Inprogress, 2=Delayed, 3=Completed (contract status filter).</summary>
        public int? ContractStatus { get; set; }
    }
}
