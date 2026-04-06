using CorpServe.Services.Abstraction;
using CorpServe.Shared;
using CorpServe.Shared.DTOs.ProposalDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    public class ProposalsController : ApiBaseController
    {
        private readonly IProposalService _proposalService;

        public ProposalsController(IProposalService proposalService)
        {
            _proposalService = proposalService;
        }

        [Authorize(Roles = "Vendor")]
        [HttpPost("accept")]
        public async Task<ActionResult<ProposalDTO>> VendorAccept([FromBody] CreateProposalDTO request)
        {
            var result = await _proposalService.VendorAcceptProposalAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpPost("negotiate")]
        public async Task<ActionResult<ProposalDTO>> VendorNegotiate([FromBody] NegotiateProposalDTO request)
        {
            var result = await _proposalService.VendorNegotiateProposalAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpPost("reject")]
        public async Task<ActionResult<ProposalDTO>> VendorReject([FromBody] RejectProposalDTO request)
        {
            var result = await _proposalService.VendorRejectProposalAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpGet("submitted")]
        public async Task<ActionResult<PaginatedResult<ProposalDTO>>> GetVendorSubmittedProposals([FromQuery] ProposalQueryParams queryParams)
        {
            var result = await _proposalService.GetVendorSubmittedProposalsAsync(GetUserIdFromToken(), queryParams);
            return Ok(result);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("request/{requestId}/count")]
        public async Task<ActionResult<int>> CountForClient(string requestId)
        {
            var result = await _proposalService.ProposalCountForRequestAsync(GetUserIdFromToken(), requestId);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("request/{requestId}")]
        public async Task<ActionResult<IEnumerable<ProposalDTO>>> GetForClientRequest(string requestId)
        {
            var result = await _proposalService.GetClientRequestProposalsAsync(GetUserIdFromToken(), requestId);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpPost("{proposalId}/client-accept")]
        public async Task<ActionResult<SLAContractDTO>> ClientAccept(string proposalId)
        {
            var result = await _proposalService.ClientAcceptProposalAsync(GetUserIdFromToken(), proposalId);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpPost("{proposalId}/client-reject")]
        public async Task<ActionResult<ProposalDTO>> ClientReject(string proposalId)
        {
            var result = await _proposalService.ClientRejectProposalAsync(GetUserIdFromToken(), proposalId);
            return HandleResult(result);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("client-active-requests")]
        public async Task<ActionResult<PaginatedResult<ActiveRequestDTO>>> GetClientActiveRequests([FromQuery] ProposalQueryParams queryParams)
        {
            var result = await _proposalService.GetClientActiveContractsAsync(GetUserIdFromToken(), queryParams);
            return Ok(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor-active-requests")]
        public async Task<ActionResult<PaginatedResult<ActiveRequestDTO>>> GetVendorActiveRequests([FromQuery] ProposalQueryParams queryParams)
        {
            var result = await _proposalService.GetVendorActiveContractsAsync(GetUserIdFromToken(), queryParams);
            return Ok(result);
        }

        [Authorize(Roles = "Client")]
        [HttpGet("request/{requestId}/sla/client")]
        public async Task<ActionResult<SLAContractDTO>> GetSlaForClientRequest(string requestId)
        {
            var result = await _proposalService.GetSlaContractForClientRequestAsync(GetUserIdFromToken(), requestId);
            return HandleResult(result);
        }

        [Authorize(Roles = "Vendor")]
        [HttpGet("request/{requestId}/sla/vendor")]
        public async Task<ActionResult<SLAContractDTO>> GetSlaForVendorRequest(string requestId)
        {
            var result = await _proposalService.GetSlaContractForVendorRequestAsync(GetUserIdFromToken(), requestId);
            return HandleResult(result);
        }
    }
}
