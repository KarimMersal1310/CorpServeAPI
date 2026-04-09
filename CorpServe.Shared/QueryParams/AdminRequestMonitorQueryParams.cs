namespace CorpServe.Shared.QueryParams
{
    public class AdminRequestMonitorQueryParams : BaseQueryParams
    {
        public string? CategoryId { get; set; }
        public int? RequestStatus { get; set; }
        public int? SlaStatus { get; set; }
    }
}
