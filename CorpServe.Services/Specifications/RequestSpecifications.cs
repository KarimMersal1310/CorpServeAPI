using CorpServe.Domain.Entities.RequestModule;

namespace EventHub.Services.Specifications
{
    public sealed class ClientRequestListSpecification : BaseSpecificactions<Request, string>
    {
        public ClientRequestListSpecification(string clientId, string? search, int? requestStatus, int pageSize, int pageIndex)
            : base(r => r.ClientId == clientId
                && (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search))
                && (!requestStatus.HasValue || (int)r.RequestStatus == requestStatus.Value))
        {
            AddInclude(r => r.Category);
            AddInclude(r => r.RequestAttachments!);
            AddInclude(r => r.AIEstimation!);
            AddOrderByDescending(r => r.CreatedAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class ClientRequestCountSpecification : BaseSpecificactions<Request, string>
    {
        public ClientRequestCountSpecification(string clientId, string? search, int? requestStatus)
            : base(r => r.ClientId == clientId
                && (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search))
                && (!requestStatus.HasValue || (int)r.RequestStatus == requestStatus.Value))
        {
        }
    }

    public sealed class RequestByIdForClientSpecification : BaseSpecificactions<Request, string>
    {
        public RequestByIdForClientSpecification(string requestId, string clientId)
            : base(r => r.Id == requestId && r.ClientId == clientId)
        {
            AddInclude(r => r.Category);
            AddInclude(r => r.RequestAttachments!);
            AddInclude(r => r.AIEstimation!);
        }
    }
}
