namespace CorpServe.Shared.QueryParams
{
    public class AdminRequestMonitorQueryParams : BaseQueryParams
    {
        public string? CategoryId { get; set; }
        public int? RequestStatus { get; set; }
        /// <summary>Legacy: raw SLAStatus enum int when SlaDisplayFilter is not used.</summary>
        public int? SlaStatus { get; set; }
        /// <summary>When set (n/a, active, at-risk, delayed), drives SLA-side filtering instead of SlaStatus.</summary>
        public string? SlaDisplayFilter { get; set; }
    }
}
