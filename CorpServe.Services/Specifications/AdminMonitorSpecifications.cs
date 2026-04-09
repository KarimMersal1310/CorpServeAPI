using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;

namespace CorpServe.Services.Specifications
{
    public sealed class AdminRequestMonitorListSpecification : BaseSpecificactions<Request, string>
    {
        public AdminRequestMonitorListSpecification(string? search, string? categoryId, int? requestStatus, int? slaStatus, int pageSize, int pageIndex)
            : base(r => (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search)
                    || r.Client.FullName.Contains(search)
                    || (r.SLAContract != null && r.SLAContract.Vendor.FullName.Contains(search)))
                && (string.IsNullOrWhiteSpace(categoryId) || r.CateogryId == categoryId)
                && (!requestStatus.HasValue || (int)r.RequestStatus == requestStatus.Value)
                && (!slaStatus.HasValue || (r.SLAContract != null && (int)r.SLAContract.SLAStatus == slaStatus.Value)))
        {
            AddInclude(r => r.Client);
            AddInclude(r => r.Category);
            AddInclude(r => r.RequestProgress);
            AddInclude(r => r.SLAContract!);
            AddInclude(r => r.SLAContract!.Vendor);
            AddInclude(r => r.Proposals);

            AddOrderByDescending(r => r.CreatedAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class AdminRequestMonitorCountSpecification : BaseSpecificactions<Request, string>
    {
        public AdminRequestMonitorCountSpecification(string? search, string? categoryId, int? requestStatus, int? slaStatus)
            : base(r => (string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search)
                    || r.Discription.Contains(search)
                    || r.Client.FullName.Contains(search)
                    || (r.SLAContract != null && r.SLAContract.Vendor.FullName.Contains(search)))
                && (string.IsNullOrWhiteSpace(categoryId) || r.CateogryId == categoryId)
                && (!requestStatus.HasValue || (int)r.RequestStatus == requestStatus.Value)
                && (!slaStatus.HasValue || (r.SLAContract != null && (int)r.SLAContract.SLAStatus == slaStatus.Value)))
        {
        }
    }

    public sealed class AdminSlaMonitorListSpecification : BaseSpecificactions<SLAContract, string>
    {
        public AdminSlaMonitorListSpecification(string? search, int? slaStatus, int pageSize, int pageIndex)
            : base(s => (string.IsNullOrWhiteSpace(search)
                    || s.Request.Title.Contains(search)
                    || s.Request.Discription.Contains(search)
                    || s.Client.FullName.Contains(search)
                    || s.Vendor.FullName.Contains(search))
                && (!slaStatus.HasValue || (int)s.SLAStatus == slaStatus.Value))
        {
            AddInclude(s => s.Request);
            AddInclude(s => s.Client);
            AddInclude(s => s.Vendor);
            AddOrderByDescending(s => s.CreatedAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class AdminSlaMonitorCountSpecification : BaseSpecificactions<SLAContract, string>
    {
        public AdminSlaMonitorCountSpecification(string? search, int? slaStatus)
            : base(s => (string.IsNullOrWhiteSpace(search)
                    || s.Request.Title.Contains(search)
                    || s.Request.Discription.Contains(search)
                    || s.Client.FullName.Contains(search)
                    || s.Vendor.FullName.Contains(search))
                && (!slaStatus.HasValue || (int)s.SLAStatus == slaStatus.Value))
        {
        }
    }

    public sealed class RequestCountByClientSpecification : BaseSpecificactions<Request, string>
    {
        public RequestCountByClientSpecification(string clientId)
            : base(r => r.ClientId == clientId)
        {
        }
    }

    public sealed class RequestsForClientsSpecification : BaseSpecificactions<Request, string>
    {
        public RequestsForClientsSpecification(ISet<string> clientIds)
            : base(r => clientIds.Contains(r.ClientId))
        {
        }
    }

    public sealed class SlaCountByVendorSpecification : BaseSpecificactions<SLAContract, string>
    {
        public SlaCountByVendorSpecification(string vendorId)
            : base(s => s.VendorId == vendorId)
        {
        }
    }

    public sealed class SlaContractsForVendorsSpecification : BaseSpecificactions<SLAContract, string>
    {
        public SlaContractsForVendorsSpecification(ISet<string> vendorIds)
            : base(s => vendorIds.Contains(s.VendorId))
        {
        }
    }

    public sealed class SlaCountByStatusSpecification : BaseSpecificactions<SLAContract, string>
    {
        public SlaCountByStatusSpecification(SLAStatus status)
            : base(s => s.SLAStatus == status)
        {
        }
    }

    public sealed class SlaTotalCountSpecification : BaseSpecificactions<SLAContract, string>
    {
        public SlaTotalCountSpecification()
            : base(s => true)
        {
        }
    }
}
