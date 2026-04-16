using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Services.Specifications;

namespace CorpServe.Services.Specifications
{
    public sealed class ClientRequestListSpecification : BaseSpecificactions<Request, string>
    {
        public ClientRequestListSpecification(string clientId, string? search, int? requestStatus, string? categoryId, bool sortByCategory, bool sortDescending, int pageSize, int pageIndex)
            : base(r => r.ClientId == clientId
                && (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search))
                && (string.IsNullOrWhiteSpace(categoryId) || r.CateogryId == categoryId)
                && (!requestStatus.HasValue || (int)r.RequestStatus == requestStatus.Value))
        {
            AddInclude(r => r.Category);
            AddInclude(r => r.RequestAttachments!);
            AddInclude(r => r.AIEstimation!);
            AddInclude(r => r.SLAContract!);
            AddInclude(r => r.RequestProgress!);

            if (sortByCategory)
            {
                if (sortDescending)
                    AddOrderByDescending(r => r.Category.Name);
                else
                    AddOrderBy(r => r.Category.Name);
            }
            else
            {
                if (sortDescending)
                    AddOrderByDescending(r => r.CreatedAt);
                else
                    AddOrderBy(r => r.CreatedAt);
            }

            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class ClientRequestCountSpecification : BaseSpecificactions<Request, string>
    {
        public ClientRequestCountSpecification(string clientId, string? search, int? requestStatus, string? categoryId)
            : base(r => r.ClientId == clientId
                && (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search))
                && (string.IsNullOrWhiteSpace(categoryId) || r.CateogryId == categoryId)
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
            AddInclude(r => r.SLAContract!);
            AddInclude(r => r.Payment!);
        }
    }

    public sealed class RequestByIdWithSlaSpecification : BaseSpecificactions<Request, string>
    {
        public RequestByIdWithSlaSpecification(string requestId)
            : base(r => r.Id == requestId)
        {
            AddInclude(r => r.SLAContract!);
        }
    }

    public sealed class VendorRequestListSpecification : BaseSpecificactions<Request, string>
    {
        public VendorRequestListSpecification(string vendorId, string? search, string? categoryId, bool sortByCategory, bool sortDescending, int pageSize, int pageIndex)
            : base(r => r.RequestStatus == RequestStatus.Pending
                && r.Client.Status == UserStatus.Active
                && r.Category.VendorCategories.Any(vc => vc.VendorId == vendorId)
                && !r.Proposals.Any(p => p.VendorId == vendorId)
                && (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search))
                && (string.IsNullOrWhiteSpace(categoryId) || r.CateogryId == categoryId))
        {
            AddInclude(r => r.Category);
            AddInclude(r => r.Client);
            AddInclude(r => r.RequestAttachments!);

            if (sortByCategory)
            {
                if (sortDescending)
                    AddOrderByDescending(r => r.Category.Name);
                else
                    AddOrderBy(r => r.Category.Name);
            }
            else
            {
                if (sortDescending)
                    AddOrderByDescending(r => r.CreatedAt);
                else
                    AddOrderBy(r => r.CreatedAt);
            }

            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class VendorRequestCountSpecification : BaseSpecificactions<Request, string>
    {
        public VendorRequestCountSpecification(string vendorId, string? search, string? categoryId)
            : base(r => r.RequestStatus == RequestStatus.Pending
                && r.Client.Status == UserStatus.Active
                && r.Category.VendorCategories.Any(vc => vc.VendorId == vendorId)
                && !r.Proposals.Any(p => p.VendorId == vendorId)
                && (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search))
                && (string.IsNullOrWhiteSpace(categoryId) || r.CateogryId == categoryId))
        {
        }
    }

    public sealed class ActiveRequestByVendorSlaSpecification : BaseSpecificactions<Request, string>
    {
        public ActiveRequestByVendorSlaSpecification(string requestId, string vendorId)
            : base(r => r.Id == requestId
                && r.RequestStatus == RequestStatus.Active
                && r.SLAContract != null
                && r.SLAContract.VendorId == vendorId)
        {
            AddInclude(r => r.Category);
            AddInclude(r => r.RequestAttachments!);
            AddInclude(r => r.AIEstimation!);
            AddInclude(r => r.SLAContract!);
        }
    }

    public sealed class CompletedUnpaidRequestsForClientSpecification : BaseSpecificactions<Request, string>
    {
        public CompletedUnpaidRequestsForClientSpecification(string clientId)
            : base(r => r.ClientId == clientId
                && r.RequestStatus == RequestStatus.Completed
                && r.SLAContract != null
                && (r.Payment == null || r.Payment.PaymentStatus != Domain.Entities.PaymentModule.PaymentStatus.Completed))
        {
            AddInclude(r => r.SLAContract!);
            AddInclude(r => r.Payment!);
        }
    }

    public sealed class CompletedPaidRequestsForClientSpecification : BaseSpecificactions<Request, string>
    {
        public CompletedPaidRequestsForClientSpecification(string clientId)
            : base(r => r.ClientId == clientId
                && r.RequestStatus == RequestStatus.Completed
                && r.SLAContract != null
                && r.Payment != null
                && r.Payment.PaymentStatus == Domain.Entities.PaymentModule.PaymentStatus.Completed)
        {
            AddInclude(r => r.SLAContract!);
            AddInclude(r => r.Payment!);
        }
    }
}
