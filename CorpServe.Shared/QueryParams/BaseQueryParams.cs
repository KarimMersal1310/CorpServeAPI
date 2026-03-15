namespace CorpServe.Shared.QueryParams
{
    public class BaseQueryParams
    {
        public string? Search { get; set; }

        private const int DefaultPageSize = 5;
        private const int MaxPageSize = 10;

        private int _pageIndex = 1;
        private int _pageSize = DefaultPageSize;

        public int PageIndex
        {
            get => _pageIndex;
            set => _pageIndex = value <= 0 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value <= 0 ? DefaultPageSize : value > MaxPageSize ? MaxPageSize : value;
        }
    }
}
