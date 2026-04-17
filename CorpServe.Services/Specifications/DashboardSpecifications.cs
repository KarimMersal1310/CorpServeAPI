using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.RatingModule;
using CorpServe.Domain.Entities.VendorVerifyModule;

namespace CorpServe.Services.Specifications
{
    public sealed class ClientDashboardRequestsSpecification : BaseSpecificactions<Request, string>
    {
        public ClientDashboardRequestsSpecification(string clientId)
            : base(r => r.ClientId == clientId)
        {
            AddInclude(r => r.Category);
            AddInclude(r => r.Proposals);
            AddOrderByDescending(r => r.CreatedAt);
        }
    }

    public sealed class ClientPendingProposalsForDashboardSpecification : BaseSpecificactions<Proposal, string>
    {
        public ClientPendingProposalsForDashboardSpecification(string clientId)
            : base(p => p.Request.ClientId == clientId
                && p.ProposalStatus == ClientStatus.Pending)
        {
        }
    }

    public sealed class ClientCompletedPaymentsForDashboardSpecification : BaseSpecificactions<Payment, string>
    {
        public ClientCompletedPaymentsForDashboardSpecification(string clientId)
            : base(p => p.ClientId == clientId
                && p.PaymentStatus == PaymentStatus.Completed)
        {
        }
    }

    public sealed class ClientPendingPaymentsForDashboardSpecification : BaseSpecificactions<Payment, string>
    {
        public ClientPendingPaymentsForDashboardSpecification(string clientId)
            : base(p => p.ClientId == clientId
                && p.PaymentStatus == PaymentStatus.Pending)
        {
            AddInclude(p => p.Request);
            AddOrderByDescending(p => p.CreatedAt);
        }
    }

    public sealed class VendorDashboardContractsSpecification : BaseSpecificactions<SLAContract, string>
    {
        public VendorDashboardContractsSpecification(string vendorId)
            : base(c => c.VendorId == vendorId)
        {
            AddInclude(c => c.Request);
            AddInclude(c => c.Request.Client);
            AddOrderBy(c => c.Deadline);
        }
    }

    public sealed class VendorDashboardProposalsSpecification : BaseSpecificactions<Proposal, string>
    {
        public VendorDashboardProposalsSpecification(string vendorId)
            : base(p => p.VendorId == vendorId)
        {
        }
    }

    public sealed class VendorDashboardRatingsSpecification : BaseSpecificactions<Rating, string>
    {
        public VendorDashboardRatingsSpecification(string vendorId)
            : base(r => r.VendorId == vendorId)
        {
        }
    }

    public sealed class VendorDashboardPaymentsSpecification : BaseSpecificactions<Payment, string>
    {
        public VendorDashboardPaymentsSpecification(string vendorId)
            : base(p => p.VendorId == vendorId)
        {
        }
    }

    public sealed class AdminDashboardRequestsSpecification : BaseSpecificactions<Request, string>
    {
        public AdminDashboardRequestsSpecification()
            : base(_ => true)
        {
            AddInclude(r => r.Category);
            AddOrderByDescending(r => r.CreatedAt);
        }
    }

    public sealed class AdminDashboardCompletedPaymentsSpecification : BaseSpecificactions<Payment, string>
    {
        public AdminDashboardCompletedPaymentsSpecification()
            : base(p => p.PaymentStatus == PaymentStatus.Completed)
        {
        }
    }

    public sealed class AdminDashboardSlaContractsSpecification : BaseSpecificactions<SLAContract, string>
    {
        public AdminDashboardSlaContractsSpecification()
            : base(_ => true)
        {
        }
    }

    public sealed class AdminDashboardPendingVendorVerificationsSpecification : BaseSpecificactions<VendorVerify, string>
    {
        public AdminDashboardPendingVendorVerificationsSpecification()
            : base(v => v.Status == VerifyStatus.Pending)
        {
            AddInclude(v => v.Vendor);
            AddInclude(v => v.Vendor.VendorCategories);
            AddOrderByDescending(v => v.SubmittedAt);
        }
    }
}
