namespace CorpServe.Shared.QueryParams
{
    public class RequestQueryParams : BaseQueryParams
    {
        public int? RequestStatus { get; set; }
        public string? CategoryId { get; set; }
        public bool SortByCategory { get; set; }
        public bool SortDescending { get; set; }
    }
}
