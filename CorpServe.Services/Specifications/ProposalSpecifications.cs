using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.IdentityModule;

namespace CorpServe.Services.Specifications
{
    public sealed class ProposalByIdSpecification : BaseSpecificactions<Proposal, string>
    {
        public ProposalByIdSpecification(string proposalId)
            : base(p => p.Id == proposalId)
        {
            AddInclude(p => p.Vendor);
            AddInclude(p => p.Request);
            AddInclude(p => p.Request.Client);
        }
    }

    public sealed class ProposalByRequestSpecification : BaseSpecificactions<Proposal, string>
    {
        public ProposalByRequestSpecification(string requestId)
            : base(p => p.RequestId == requestId)
        {
            AddInclude(p => p.Vendor);
            AddInclude(p => p.Request);
            AddInclude(p => p.Request.Client);
        }
    }

    public sealed class ProposalCountForClientRequestSpecification : BaseSpecificactions<Proposal, string>
    {
        public ProposalCountForClientRequestSpecification(string clientId, string requestId)
            : base(p => p.RequestId == requestId
                && p.Request.ClientId == clientId
                && p.ProposalStatus == ClientStatus.Pending)
        {
        }
    }

    public sealed class VendorSubmittedProposalsListSpecification : BaseSpecificactions<Proposal, string>
    {
        public VendorSubmittedProposalsListSpecification(string vendorId, string? search, int pageSize, int pageIndex)
            : base(p => p.VendorId == vendorId
                && (string.IsNullOrWhiteSpace(search)
                    || p.Request.Title.Contains(search)
                    || (p.Message != null && p.Message.Contains(search))))
        {
            AddInclude(p => p.Vendor);
            AddInclude(p => p.Request);
            AddInclude(p => p.Request.Client);
            AddOrderByDescending(p => p.CreatedAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class VendorSubmittedProposalsCountSpecification : BaseSpecificactions<Proposal, string>
    {
        public VendorSubmittedProposalsCountSpecification(string vendorId, string? search)
            : base(p => p.VendorId == vendorId
                && (string.IsNullOrWhiteSpace(search)
                    || p.Request.Title.Contains(search)
                    || (p.Message != null && p.Message.Contains(search))))
        {
        }
    }

    public sealed class ClientRequestProposalsSpecification : BaseSpecificactions<Proposal, string>
    {
        public ClientRequestProposalsSpecification(string clientId, string requestId)
            : base(p => p.RequestId == requestId
                && p.Request.ClientId == clientId
                && p.ProposalStatus != ClientStatus.Rejected)
        {
            AddInclude(p => p.Vendor);
            AddInclude(p => p.Request);
            AddOrderByDescending(p => p.CreatedAt);
        }
    }

    public sealed class ClientOwnedProposalByIdSpecification : BaseSpecificactions<Proposal, string>
    {
        public ClientOwnedProposalByIdSpecification(string clientId, string proposalId)
            : base(p => p.Id == proposalId && p.Request.ClientId == clientId)
        {
            AddInclude(p => p.Vendor);
            AddInclude(p => p.Request);
            AddInclude(p => p.Request.Client);
            AddInclude(p => p.Request.Proposals);
            ApplyTracking();
        }
    }

    public sealed class ProposalByRequestWithRequestClientSpecification : BaseSpecificactions<Proposal, string>
    {
        public ProposalByRequestWithRequestClientSpecification(string requestId)
            : base(p => p.RequestId == requestId)
        {
            AddInclude(p => p.Request);
            AddInclude(p => p.Request.Client);
            AddInclude(p => p.Vendor);
        }
    }

    public sealed class SlaContractByClientRequestSpecification : BaseSpecificactions<SLAContract, string>
    {
        public SlaContractByClientRequestSpecification(string clientId, string requestId)
            : base(s => s.ClientId == clientId && s.RequestId == requestId)
        {
            AddInclude(s => s.Client);
            AddInclude(s => s.Vendor);
            AddInclude(s => s.Request);
        }
    }

    public sealed class SlaContractByVendorRequestSpecification : BaseSpecificactions<SLAContract, string>
    {
        public SlaContractByVendorRequestSpecification(string vendorId, string requestId)
            : base(s => s.VendorId == vendorId && s.RequestId == requestId)
        {
            AddInclude(s => s.Client);
            AddInclude(s => s.Vendor);
            AddInclude(s => s.Request);
        }
    }

    public sealed class ClientActiveSlaContractsListSpecification : BaseSpecificactions<SLAContract, string>
    {
        public ClientActiveSlaContractsListSpecification(string clientId, string? search, string? slaLabel, string? taskState, int pageSize, int pageIndex)
            : base(s => s.ClientId == clientId
                && s.Request.RequestStatus == RequestStatus.Active
                && s.SLAStatus != SLAStatus.Completed
                && (string.IsNullOrWhiteSpace(search)
                    || s.Request.Title.Contains(search)
                    || s.Request.Discription.Contains(search)
                    || s.Vendor.FullName.Contains(search))
                && (string.IsNullOrWhiteSpace(taskState)
                    || (taskState.ToLower() == "inprogress" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "delayed" && (s.SLAStatus == SLAStatus.Delayed || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow))))
                && (string.IsNullOrWhiteSpace(slaLabel)
                    || (slaLabel.ToLower() == "on track" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "ontrack" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "warning" && ((s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow.AddHours(48) && s.Deadline > DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (slaLabel.ToLower() == "delayed" && ((s.SLAStatus == SLAStatus.Delayed && s.Client.Status == UserStatus.Active && s.Vendor.Status == UserStatus.Active) || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "blocked" && s.SLAStatus == SLAStatus.Delayed && (s.Client.Status == UserStatus.Suspended || s.Vendor.Status == UserStatus.Suspended))))
        {
            AddInclude(s => s.Client);
            AddInclude(s => s.Vendor);
            AddInclude(s => s.Request);
            AddOrderByDescending(s => s.CreatedAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class ClientActiveSlaContractsCountSpecification : BaseSpecificactions<SLAContract, string>
    {
        public ClientActiveSlaContractsCountSpecification(string clientId, string? search, string? slaLabel, string? taskState)
            : base(s => s.ClientId == clientId
                && s.Request.RequestStatus == RequestStatus.Active
                && s.SLAStatus != SLAStatus.Completed
                && (string.IsNullOrWhiteSpace(search)
                    || s.Request.Title.Contains(search)
                    || s.Request.Discription.Contains(search)
                    || s.Vendor.FullName.Contains(search))
                && (string.IsNullOrWhiteSpace(taskState)
                    || (taskState.ToLower() == "inprogress" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "delayed" && (s.SLAStatus == SLAStatus.Delayed || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow))))
                && (string.IsNullOrWhiteSpace(slaLabel)
                    || (slaLabel.ToLower() == "on track" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "ontrack" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "warning" && ((s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow.AddHours(48) && s.Deadline > DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (slaLabel.ToLower() == "delayed" && ((s.SLAStatus == SLAStatus.Delayed && s.Client.Status == UserStatus.Active && s.Vendor.Status == UserStatus.Active) || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "blocked" && s.SLAStatus == SLAStatus.Delayed && (s.Client.Status == UserStatus.Suspended || s.Vendor.Status == UserStatus.Suspended))))
        {
        }
    }

    public sealed class VendorActiveSlaContractsListSpecification : BaseSpecificactions<SLAContract, string>
    {
        public VendorActiveSlaContractsListSpecification(string vendorId, string? search, string? slaLabel, string? taskState, int pageSize, int pageIndex)
            : base(s => s.VendorId == vendorId
                && s.Request.RequestStatus == RequestStatus.Active
                && s.SLAStatus != SLAStatus.Completed
                && (string.IsNullOrWhiteSpace(search)
                    || s.Request.Title.Contains(search)
                    || s.Request.Discription.Contains(search)
                    || s.Client.FullName.Contains(search))
                && (string.IsNullOrWhiteSpace(taskState)
                    || (taskState.ToLower() == "inprogress" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "delayed" && (s.SLAStatus == SLAStatus.Delayed || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow))))
                && (string.IsNullOrWhiteSpace(slaLabel)
                    || (slaLabel.ToLower() == "on track" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "ontrack" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "warning" && ((s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow.AddHours(48) && s.Deadline > DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (slaLabel.ToLower() == "delayed" && ((s.SLAStatus == SLAStatus.Delayed && s.Client.Status == UserStatus.Active && s.Vendor.Status == UserStatus.Active) || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "blocked" && s.SLAStatus == SLAStatus.Delayed && (s.Client.Status == UserStatus.Suspended || s.Vendor.Status == UserStatus.Suspended))))
        {
            AddInclude(s => s.Client);
            AddInclude(s => s.Vendor);
            AddInclude(s => s.Request);
            AddOrderByDescending(s => s.CreatedAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class VendorActiveSlaContractsCountSpecification : BaseSpecificactions<SLAContract, string>
    {
        public VendorActiveSlaContractsCountSpecification(string vendorId, string? search, string? slaLabel, string? taskState)
            : base(s => s.VendorId == vendorId
                && s.Request.RequestStatus == RequestStatus.Active
                && s.SLAStatus != SLAStatus.Completed
                && (string.IsNullOrWhiteSpace(search)
                    || s.Request.Title.Contains(search)
                    || s.Request.Discription.Contains(search)
                    || s.Client.FullName.Contains(search))
                && (string.IsNullOrWhiteSpace(taskState)
                    || (taskState.ToLower() == "inprogress" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (taskState.ToLower() == "delayed" && (s.SLAStatus == SLAStatus.Delayed || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow))))
                && (string.IsNullOrWhiteSpace(slaLabel)
                    || (slaLabel.ToLower() == "on track" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "ontrack" && s.SLAStatus == SLAStatus.Inprogress && s.Deadline > DateTime.UtcNow.AddHours(48))
                    || (slaLabel.ToLower() == "warning" && ((s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow.AddHours(48) && s.Deadline > DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "breached" && s.SLAStatus == SLAStatus.Breached && s.Deadline > DateTime.UtcNow)
                    || (slaLabel.ToLower() == "delayed" && ((s.SLAStatus == SLAStatus.Delayed && s.Client.Status == UserStatus.Active && s.Vendor.Status == UserStatus.Active) || (s.SLAStatus == SLAStatus.Inprogress && s.Deadline <= DateTime.UtcNow) || (s.SLAStatus == SLAStatus.Breached && s.Deadline <= DateTime.UtcNow)))
                    || (slaLabel.ToLower() == "blocked" && s.SLAStatus == SLAStatus.Delayed && (s.Client.Status == UserStatus.Suspended || s.Vendor.Status == UserStatus.Suspended))))
        {
        }
    }

    public sealed class VendorCompletedContractsSpecification : BaseSpecificactions<SLAContract, string>
    {
        public VendorCompletedContractsSpecification(string vendorId)
            : base(s => s.VendorId == vendorId && s.Request.RequestStatus == RequestStatus.Completed)
        {
            AddInclude(s => s.Request);
            AddInclude(s => s.Request.Client);
            AddInclude(s => s.Request.Payment!);
            AddInclude(s => s.Request.Rating!);
            AddOrderByDescending(s => s.Request.CreatedAt);
        }
    }

    public sealed class ExpiredInProgressSlaContractsSpecification : BaseSpecificactions<SLAContract, string>
    {
        public ExpiredInProgressSlaContractsSpecification(DateTime utcNow)
            : base(s => (s.SLAStatus == SLAStatus.Inprogress || s.SLAStatus == SLAStatus.Breached) && s.Deadline <= utcNow)
        {
            AddInclude(s => s.Request);
        }
    }

    public sealed class SlaContractByRequestIdSpecification : BaseSpecificactions<SLAContract, string>
    {
        public SlaContractByRequestIdSpecification(string requestId)
            : base(s => s.RequestId == requestId)
        {
        }
    }

    public sealed class ActiveSlaContractsForMonitoringSpecification : BaseSpecificactions<SLAContract, string>
    {
        public ActiveSlaContractsForMonitoringSpecification()
            : base(s => s.SLAStatus != SLAStatus.Completed)
        {
            AddInclude(s => s.Request);
            AddInclude(s => s.Client);
            AddInclude(s => s.Vendor);
        }
    }
}
