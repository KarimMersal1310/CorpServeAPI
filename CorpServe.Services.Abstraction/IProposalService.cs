using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.ProposalDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Shared;

namespace CorpServe.Services.Abstraction
{
    public interface IProposalService
    {
        #region Client Operations

        Task<Result<int>> ProposalCountForRequestAsync(string clientId, string requestId);
        Task<Result<IEnumerable<ProposalDTO>>> GetClientRequestProposalsAsync(string clientId, string requestId);
        Task<Result<ProposalDTO>> ClientRejectProposalAsync(string clientId, string proposalId, ClientRejectProposalDTO dto);
        Task<Result<SLAContractDTO>> ClientAcceptProposalAsync(string clientId, string proposalId);
        Task<Result<SLAContractDTO>> GetSlaContractForClientRequestAsync(string clientId, string requestId);
        Task<PaginatedResult<ActiveRequestDTO>> GetClientActiveContractsAsync(string clientId, ProposalQueryParams queryParams);

        #endregion

        #region Vendor Operations

        Task<Result<ProposalDTO>> VendorAcceptProposalAsync(string vendorId, CreateProposalDTO createProposalDTO);
        Task<Result<ProposalDTO>> VendorNegotiateProposalAsync(string vendorId, NegotiateProposalDTO negotiateProposalDTO);
        Task<Result<ProposalDTO>> VendorRejectProposalAsync(string vendorId, RejectProposalDTO rejectProposalDTO);
        Task<PaginatedResult<ProposalDTO>> GetVendorSubmittedProposalsAsync(string vendorId, ProposalQueryParams queryParams);
        Task<PaginatedResult<ActiveRequestDTO>> GetVendorActiveContractsAsync(string vendorId, ProposalQueryParams queryParams);
        Task<Result<IEnumerable<VendorCompletedRequestDTO>>> GetVendorCompletedContractsAsync(string vendorId);
        Task<Result<SLAContractDTO>> GetSlaContractForVendorRequestAsync(string vendorId, string requestId);

        #endregion
    }
}
