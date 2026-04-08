namespace CorpServe.Shared.CommonResult
{
    public enum ErrorType
    {
        Failure = 0,
        Validation = 1,
        NotFound = 2,
        Unauthorized = 3,
        Forbidden = 4,
        InvalidCrendentials = 5,
        Conflict = 6,
        None = 7,
        /// <summary>Upstream provider (e.g. payment gateway) error; maps to HTTP 502.</summary>
        BadGateway = 8
    }
}
