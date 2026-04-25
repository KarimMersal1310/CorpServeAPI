using AutoMapper;
using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.ChatModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Mapping;
using CorpServe.Services.EmailTemplates;
using CorpServe.Services.Specifications;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.ProposalDTOs;
using CorpServe.Shared.Notifications;
using CorpServe.Shared.QueryParams;
using CorpServe.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CorpServe.Services
{
    public class ProposalService : IProposalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<ProposalService> _logger;

        public ProposalService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService, UserManager<ApplicationUser> userManager, INotificationService notificationService, IPaymentService paymentService, ILogger<ProposalService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailService = emailService;
            _userManager = userManager;
            _notificationService = notificationService;
            _paymentService = paymentService;
            _logger = logger;
        }

        #region Client Operations

        public async Task<Result<int>> ProposalCountForRequestAsync(string clientId, string requestId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Proposal.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Proposal.RequestRequired", "Request ID is required.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var ownsRequest = await requestRepo.AnyAsync(r => r.Id == requestId && r.ClientId == clientId);
            if (!ownsRequest)
                return Error.NotFound("Request.NotFound", "Request not found.");

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var count = await proposalRepo.CountAsync(new ProposalCountForClientRequestSpecification(clientId, requestId));
            return count;
        }

        public async Task<Result<IEnumerable<ProposalDTO>>> GetClientRequestProposalsAsync(string clientId, string requestId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Proposal.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Proposal.RequestRequired", "Request ID is required.");

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var proposals = (await proposalRepo.Query(new ClientRequestProposalsSpecification(clientId, requestId)).ToListAsync())
                .Where(p => p.Vendor.Status == UserStatus.Active)
                .ToList();

            var list = _mapper.Map<List<ProposalDTO>>(proposals);
            await EnrichProposalDtosAsync(list);
            return list;
        }

        public async Task<Result<ProposalDTO>> ClientRejectProposalAsync(string clientId, string proposalId, ClientRejectProposalDTO dto)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Proposal.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(proposalId))
                return Error.Validation("Proposal.IdRequired", "Proposal ID is required.");

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var proposal = await proposalRepo.GetByIdAsync(new ClientOwnedProposalByIdSpecification(clientId, proposalId));
            if (proposal is null)
                return Error.NotFound("Proposal.NotFound", "Proposal not found.");

            if (proposal.Request.RequestStatus != RequestStatus.Pending)
                return Error.Validation("Proposal.RequestNotPending", "Proposal can only be rejected while request is pending.");

            if (proposal.ProposalType == VendorStatus.Reject)
                return Error.Validation("Proposal.VendorRejected", "Vendor already rejected this request.");

            if (proposal.ProposalStatus != ClientStatus.Pending)
                return Error.Validation("Proposal.AlreadyResponded", "Proposal already has a client response.");

            if (proposal.Vendor.Status == UserStatus.Suspended)
                return Error.Validation("Proposal.VendorSuspended", "This proposal cannot be processed because vendor is suspended.");

            proposal.ProposalStatus = ClientStatus.Rejected;
            proposal.ClientRejectionReason = dto.Reason;
            proposal.IsSelected = false;
            proposal.ClientResponseAt = DateTime.UtcNow;
            proposalRepo.Update(proposal);
            await _unitOfWork.SaveChangesAsync();

            var rejectedNotification = await _notificationService.SendNotificationAsync(
                proposal.VendorId,
                NotificationTitles.ProposalRejected,
                $"Your proposal for request '{proposal.Request.Title}' was rejected by the client. Reason: {dto.Reason}",
                NotificationTypes.Warning,
                proposal.RequestId,
                "Request",
                sendEmail: false);

            if (rejectedNotification.IsFailure)
                LogNotificationFailure("ClientRejectProposal", rejectedNotification.Errors);

            await TrySendClientRejectEmailAsync(proposal, dto.Reason);

            var rejectedDto = _mapper.Map<ProposalDTO>(proposal);
            await EnrichProposalDtosAsync(new List<ProposalDTO> { rejectedDto });
            return rejectedDto;
        }

        public async Task<Result<SLAContractDTO>> ClientAcceptProposalAsync(string clientId, string proposalId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Proposal.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            var hasUnpaidCompleted = await _paymentService.HasUnpaidCompletedRequestsAsync(clientId);
            if (hasUnpaidCompleted.IsFailure)
                return hasUnpaidCompleted.Errors.ToList();

            if (hasUnpaidCompleted.Value)
                return Error.Conflict("Payment.UnpaidCompletedRequestExists", "You must complete payment for previous completed requests before accepting a new proposal.");

            if (string.IsNullOrWhiteSpace(proposalId))
                return Error.Validation("Proposal.IdRequired", "Proposal ID is required.");

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var selectedProposal = await proposalRepo.GetByIdAsync(new ClientOwnedProposalByIdSpecification(clientId, proposalId));
            if (selectedProposal is null)
                return Error.NotFound("Proposal.NotFound", "Proposal not found.");

            if (selectedProposal.Request.RequestStatus != RequestStatus.Pending)
                return Error.Validation("Proposal.RequestNotPending", "Only pending requests can have proposal acceptance.");

            if (selectedProposal.ProposalType == VendorStatus.Reject)
                return Error.Validation("Proposal.VendorRejected", "Vendor already rejected this request.");

            if (selectedProposal.ProposalStatus != ClientStatus.Pending)
                return Error.Validation("Proposal.AlreadyResponded", "Proposal already has a client response.");

            if (!selectedProposal.ProposedPrice.HasValue || !selectedProposal.ProposedDeadline.HasValue)
                return Error.Validation("Proposal.InvalidForAcceptance", "Rejected proposal cannot be accepted.");

            if (selectedProposal.Vendor.Status == UserStatus.Suspended)
                return Error.Validation("Proposal.VendorSuspended", "This proposal cannot be accepted because vendor is suspended.");

            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var existingSla = await slaRepo.AnyAsync(s => s.RequestId == selectedProposal.RequestId);
            if (existingSla)
                return Error.Conflict("SLA.AlreadyExists", "SLA contract already exists for this request.");
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var requestRepo = _unitOfWork.GetRepository<Request, string>();
                var request = selectedProposal.Request;
                request.RequestStatus = RequestStatus.Active;
                requestRepo.Update(request);

                foreach (var proposal in request.Proposals)
                {
                    proposal.ProposalStatus = proposal.Id == selectedProposal.Id ? ClientStatus.Accepted : ClientStatus.Rejected;
                    proposal.IsSelected = proposal.Id == selectedProposal.Id;
                    proposal.ClientResponseAt = DateTime.UtcNow;
                    proposalRepo.Update(proposal);
                }

                var slaContract = new SLAContract
                {
                    ProposalId = selectedProposal.Id,
                    RequestId = selectedProposal.RequestId,
                    ClientId = clientId,
                    VendorId = selectedProposal.VendorId,
                    ContractPrice = selectedProposal.ProposedPrice.Value,
                    Deadline = selectedProposal.ProposedDeadline.Value,
                    CreatedAt = DateTime.UtcNow,
                    SLAStatus = SLAStatus.Inprogress
                };

                await slaRepo.AddAsync(slaContract);

                var chatRoomRepo = _unitOfWork.GetRepository<ChatRoom, string>();
                var existingRoom = await chatRoomRepo.AnyAsync(
                    cr => cr.ClientId == clientId && cr.VendorId == selectedProposal.VendorId);
                if (!existingRoom)
                {
                    var chatRoom = new ChatRoom
                    {
                        ClientId = clientId,
                        VendorId = selectedProposal.VendorId,
                        CreatedAt = DateTime.UtcNow,
                        Status = ChatRoomStatus.Active
                    };
                    await chatRoomRepo.AddAsync(chatRoom);
                }

                await _unitOfWork.SaveChangesAsync();

                var createdSla = await slaRepo.GetByIdAsync(new SlaContractByClientRequestSpecification(clientId, selectedProposal.RequestId));
                if (createdSla is null)
                    throw new InvalidOperationException("SLA created but failed to load data.");

                var selectedVendorId = selectedProposal.VendorId;
                var otherVendorIds = request.Proposals
                    .Where(p => p.Id != selectedProposal.Id)
                    .Select(p => p.VendorId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var selectedVendorNotification = await _notificationService.SendNotificationAsync(
                    selectedVendorId,
                    NotificationTitles.ProposalAccepted,
                    $"Your proposal for request '{request.Title}' was accepted and SLA contract was created.",
                    NotificationTypes.Success,
                    request.Id,
                    "Request",
                    sendEmail: false);

                ThrowIfNotificationFailed(selectedVendorNotification, "ClientAcceptProposal.SelectedVendor");

                if (otherVendorIds.Count > 0)
                {
                    var rejectedVendorsNotification = await _notificationService.SendNotificationToManyAsync(
                        otherVendorIds,
                        NotificationTitles.ProposalRejected,
                        $"Your proposal for request '{request.Title}' was automatically rejected because another proposal was accepted.",
                        NotificationTypes.Info,
                        request.Id,
                        "Request",
                        sendEmail: false);

                    ThrowIfNotificationFailed(rejectedVendorsNotification, "ClientAcceptProposal.OtherVendors");
                }

                var clientNotification = await _notificationService.SendNotificationAsync(
                    clientId,
                    NotificationTitles.SlaCreated,
                    $"SLA contract for request '{request.Title}' is now active.",
                    NotificationTypes.Success,
                    request.Id,
                    "Request",
                    sendEmail: false);

                ThrowIfNotificationFailed(clientNotification, "ClientAcceptProposal.Client");

                await TrySendClientAcceptedEmailToVendorAsync(
                    selectedProposal.Vendor,
                    request.Client,
                    request.Title,
                    selectedProposal.ProposedPrice.Value,
                    selectedProposal.ProposedDeadline.Value);

                await _unitOfWork.CommitTransactionAsync();
                var createdDto = _mapper.Map<SLAContractDTO>(createdSla);
                await EnrichSlaContractDtoAsync(createdDto);
                return createdDto;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to complete proposal acceptance transaction for proposal {ProposalId}.", proposalId);
                return Error.Failure("Proposal.AcceptTransactionFailed", "Failed to complete proposal acceptance flow. Please try again.");
            }
        }

        private async Task TrySendClientAcceptedEmailToVendorAsync(
            ApplicationUser? vendor,
            ApplicationUser? client,
            string requestTitle,
            decimal contractPrice,
            DateTime deadline)
        {
            try
            {
                var vendorEmail = vendor?.Email;
                if (string.IsNullOrWhiteSpace(vendorEmail))
                    return;

                if (!IsEmailNotificationEnabled(vendor))
                    return;

                var template = CorpServeEmailTemplateFactory.BuildClientAcceptedProposal(
                    vendor?.FullName ?? "Vendor",
                    client?.FullName ?? "Client",
                    requestTitle,
                    contractPrice,
                    deadline);

                await _emailService.SendEmailAsync(vendorEmail, template.Subject, template.Body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send client-accepted email to vendor.");
            }
        }

        public async Task<Result<SLAContractDTO>> GetSlaContractForClientRequestAsync(string clientId, string requestId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Proposal.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Proposal.RequestRequired", "Request ID is required.");

            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var contract = await slaRepo.GetByIdAsync(new SlaContractByClientRequestSpecification(clientId, requestId));
            if (contract is null)
                return Error.NotFound("SLA.NotFound", "SLA contract not found.");

            var slaDto = _mapper.Map<SLAContractDTO>(contract);
            await EnrichSlaContractDtoAsync(slaDto);
            return slaDto;
        }

        public async Task<PaginatedResult<ActiveRequestDTO>> GetClientActiveContractsAsync(string clientId, ProposalQueryParams queryParams)
        {
            if (string.IsNullOrWhiteSpace(clientId) || await IsUserSuspendedAsync(clientId))
                return new PaginatedResult<ActiveRequestDTO>(queryParams.PageIndex, queryParams.PageSize, 0, []);

            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var listSpecification = new ClientActiveSlaContractsListSpecification(clientId, queryParams.Search, queryParams.SlaLabel, queryParams.TaskState, queryParams.PageSize, queryParams.PageIndex);
            var countSpecification = new ClientActiveSlaContractsCountSpecification(clientId, queryParams.Search, queryParams.SlaLabel, queryParams.TaskState);

            var contracts = await slaRepo.Query(listSpecification).ToListAsync();
            var count = await slaRepo.CountAsync(countSpecification);
            var userIds = contracts.SelectMany(c => new[] { c.ClientId, c.VendorId }).Distinct().ToList();
            var pics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, userIds);
            var data = contracts.Select(c => ActiveContractDisplay.ToActiveRequestDto(
                c,
                pics.TryGetValue(c.ClientId, out var cp) ? cp : null,
                pics.TryGetValue(c.VendorId, out var vp) ? vp : null)).ToList();

            foreach (var item in data)
                item.ClientName = null;

            return new PaginatedResult<ActiveRequestDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }

        #endregion

        #region Vendor Operations

        public Task<Result<ProposalDTO>> VendorAcceptProposalAsync(string vendorId, CreateProposalDTO createProposalDTO)
            => CreateProposalAsync(vendorId, createProposalDTO.RequestId, createProposalDTO.ProposedPrice, createProposalDTO.ProposedDeadline, createProposalDTO.Message, VendorStatus.Accept, ClientStatus.Pending);

        public Task<Result<ProposalDTO>> VendorNegotiateProposalAsync(string vendorId, NegotiateProposalDTO negotiateProposalDTO)
            => CreateProposalAsync(vendorId, negotiateProposalDTO.RequestId, negotiateProposalDTO.ProposedPrice, negotiateProposalDTO.ProposedDeadline, negotiateProposalDTO.Message, VendorStatus.Negotiate, ClientStatus.Pending);

        public Task<Result<ProposalDTO>> VendorRejectProposalAsync(string vendorId, RejectProposalDTO rejectProposalDTO)
            => CreateProposalAsync(vendorId, rejectProposalDTO.RequestId, null, null, rejectProposalDTO.Message, VendorStatus.Reject, ClientStatus.Rejected);

        public async Task<PaginatedResult<ProposalDTO>> GetVendorSubmittedProposalsAsync(string vendorId, ProposalQueryParams queryParams)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return new PaginatedResult<ProposalDTO>(queryParams.PageIndex, queryParams.PageSize, 0, []);

            if (await IsUserSuspendedAsync(vendorId))
                return new PaginatedResult<ProposalDTO>(queryParams.PageIndex, queryParams.PageSize, 0, []);

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var listSpecification = new VendorSubmittedProposalsListSpecification(vendorId, queryParams.Search, queryParams.PageSize, queryParams.PageIndex);
            var countSpecification = new VendorSubmittedProposalsCountSpecification(vendorId, queryParams.Search);

            var proposals = await proposalRepo.Query(listSpecification).ToListAsync();
            var count = await proposalRepo.CountAsync(countSpecification);

            var data = _mapper.Map<List<ProposalDTO>>(proposals);
            await EnrichProposalDtosAsync(data);
            return new PaginatedResult<ProposalDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }

        public async Task<Result<SLAContractDTO>> GetSlaContractForVendorRequestAsync(string vendorId, string requestId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Error.Unauthorized("Proposal.VendorRequired", "Vendor identity is required.");

            if (await IsUserSuspendedAsync(vendorId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Proposal.RequestRequired", "Request ID is required.");

            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var contract = await slaRepo.GetByIdAsync(new SlaContractByVendorRequestSpecification(vendorId, requestId));
            if (contract is null)
                return Error.NotFound("SLA.NotFound", "SLA contract not found.");

            var slaDto = _mapper.Map<SLAContractDTO>(contract);
            await EnrichSlaContractDtoAsync(slaDto);
            return slaDto;
        }

        public async Task<PaginatedResult<ActiveRequestDTO>> GetVendorActiveContractsAsync(string vendorId, ProposalQueryParams queryParams)
        {
            if (string.IsNullOrWhiteSpace(vendorId) || await IsUserSuspendedAsync(vendorId))
                return new PaginatedResult<ActiveRequestDTO>(queryParams.PageIndex, queryParams.PageSize, 0, []);

            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var listSpecification = new VendorActiveSlaContractsListSpecification(vendorId, queryParams.Search, queryParams.SlaLabel, queryParams.TaskState, queryParams.PageSize, queryParams.PageIndex);
            var countSpecification = new VendorActiveSlaContractsCountSpecification(vendorId, queryParams.Search, queryParams.SlaLabel, queryParams.TaskState);

            var contracts = await slaRepo.Query(listSpecification).ToListAsync();
            var count = await slaRepo.CountAsync(countSpecification);
            var userIds = contracts.SelectMany(c => new[] { c.ClientId, c.VendorId }).Distinct().ToList();
            var pics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, userIds);
            var data = contracts.Select(c => ActiveContractDisplay.ToActiveRequestDto(
                c,
                pics.TryGetValue(c.ClientId, out var cp) ? cp : null,
                pics.TryGetValue(c.VendorId, out var vp) ? vp : null)).ToList();

            foreach (var item in data)
                item.VendorName = null;

            return new PaginatedResult<ActiveRequestDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }

        public async Task<Result<IEnumerable<VendorCompletedRequestDTO>>> GetVendorCompletedContractsAsync(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Error.Unauthorized("Proposal.VendorRequired", "Vendor identity is required.");

            if (await IsUserSuspendedAsync(vendorId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var contracts = await slaRepo.Query(new VendorCompletedContractsSpecification(vendorId)).ToListAsync();

            var data = contracts.Select(c =>
            {
                var payment = c.Request.Payment;
                var rating = c.Request.Rating;
                return new VendorCompletedRequestDTO
                {
                    RequestId = c.RequestId,
                    Title = c.Request.Title,
                    ClientId = c.ClientId,
                    ClientName = c.Request.Client?.FullName ?? c.Request.Client?.UserName ?? c.ClientId,
                    Amount = c.ContractPrice,
                    CompletedAt = payment?.PaidAt ?? c.Request.CreatedAt,
                    Rating = rating?.Stars ?? 0,
                    Feedback = rating?.Comment,
                    PaymentStatus = payment?.PaymentStatus.ToString() ?? "Pending",
                    PayoutStatus = payment?.PayoutStatus.ToString() ?? "NotStarted"
                };
            }).ToList();

            var clientIds = data.Select(d => d.ClientId).Distinct().ToList();
            var pics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, clientIds);
            foreach (var row in data)
            {
                if (pics.TryGetValue(row.ClientId, out var url) && !string.IsNullOrWhiteSpace(url))
                    row.ClientProfilePictureUrl = url;
            }

            return data;
        }

        #endregion

        #region Shared Helpers

        private async Task<Result<ProposalDTO>> CreateProposalAsync(
            string vendorId,
            string requestId,
            decimal? proposedPrice,
            DateTime? proposedDeadline,
            string? message,
            VendorStatus proposalType,
            ClientStatus proposalStatus)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Error.Unauthorized("Proposal.VendorRequired", "Vendor identity is required.");

            if (await IsUserSuspendedAsync(vendorId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            requestId = requestId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Proposal.RequestRequired", "Request ID is required.");

            if (proposalType != VendorStatus.Reject)
            {
                if (!proposedPrice.HasValue || proposedPrice <= 0)
                    return Error.Validation("Proposal.InvalidPrice", "Proposed price must be greater than 0.");

                if (!proposedDeadline.HasValue)
                    return Error.Validation("Proposal.DeadlineRequired", "Proposed deadline is required.");
            }

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(requestId);
            if (request is null)
                return Error.NotFound("Request.NotFound", "Request not found.");

            if (await IsUserSuspendedAsync(request.ClientId))
                return Error.Validation("Proposal.ClientSuspended", "Cannot submit proposal for suspended client request.");

            if (request.RequestStatus != RequestStatus.Pending)
                return Error.Validation("Proposal.RequestNotPending", "Only pending requests can receive proposals.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var isVendorAssignedToCategory = await categoryRepo.AnyAsync(c => c.Id == request.CateogryId && c.VendorCategories.Any(vc => vc.VendorId == vendorId));
            if (!isVendorAssignedToCategory)
                return Error.Validation("Proposal.CategoryNotAssigned", "Vendor is not assigned to this request category.");

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var hasActiveProposal = await proposalRepo.AnyAsync(p =>
                p.RequestId == requestId
                && p.VendorId == vendorId
                && p.ProposalType != VendorStatus.Reject
                && p.ProposalStatus != ClientStatus.Rejected);
            if (hasActiveProposal)
                return Error.Conflict("Proposal.AlreadyExists", "You already submitted a proposal for this request.");

            var hasPriceWithinClientBudget = proposedPrice is { } candidatePrice
                && candidatePrice >= request.BudgetMin
                && candidatePrice <= request.BudgetMax;

            var hasMatchingClientDeadline = proposedDeadline is { } candidateDeadline
                && candidateDeadline.Date == request.ExpectedDeadline.Date;

            if (proposalType != VendorStatus.Reject)
            {
                proposalType = hasPriceWithinClientBudget && hasMatchingClientDeadline
                    ? VendorStatus.Accept
                    : VendorStatus.Negotiate;
            }

            var proposal = new Proposal
            {
                VendorId = vendorId,
                RequestId = requestId,
                ProposalStatus = proposalStatus,
                ProposalType = proposalType,
                ProposedPrice = proposedPrice,
                ProposedDeadline = proposedDeadline,
                Message = message?.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsSelected = false
            };

            await proposalRepo.AddAsync(proposal);
            await _unitOfWork.SaveChangesAsync();

            var createdProposal = await proposalRepo.GetByIdAsync(new ProposalByIdSpecification(proposal.Id));
            if (createdProposal is null)
                return Error.Failure("Proposal.CreateFailed", "Proposal created but failed to load its data.");

            if (!string.Equals(createdProposal.RequestId, requestId, StringComparison.OrdinalIgnoreCase))
                return Error.Failure("Proposal.RequestMismatch", "Proposal was not linked to the requested request.");

            await TrySendProposalEmailAsync(createdProposal);

            var clientNewProposalNotification = await _notificationService.SendNotificationAsync(
                createdProposal.Request.ClientId,
                NotificationTitles.NewProposalReceived,
                $"Vendor '{createdProposal.Vendor.FullName}' submitted a '{createdProposal.ProposalType}' proposal for request '{createdProposal.Request.Title}'.",
                NotificationTypes.Info,
                createdProposal.RequestId,
                "Request",
                sendEmail: false);

            if (clientNewProposalNotification.IsFailure)
                LogNotificationFailure("CreateProposal.Client", clientNewProposalNotification.Errors);

            var createdDto = _mapper.Map<ProposalDTO>(createdProposal);
            await EnrichProposalDtosAsync(new List<ProposalDTO> { createdDto });
            return createdDto;
        }

        private async Task TrySendProposalEmailAsync(Proposal proposal)
        {
            try
            {
                var clientName = proposal.Request.Client?.FullName ?? "Client";
                var vendorName = proposal.Vendor?.FullName ?? "Vendor";
                var requestTitle = proposal.Request.Title;
                var clientEmail = proposal.Request.Client?.Email;

                if (string.IsNullOrWhiteSpace(clientEmail))
                    return;

                if (!IsEmailNotificationEnabled(proposal.Request.Client))
                    return;

                (string Subject, string Body) template = proposal.ProposalType switch
                {
                    VendorStatus.Accept => CorpServeEmailTemplateFactory.BuildVendorAcceptProposal(clientName, vendorName, requestTitle, proposal.ProposedPrice, proposal.ProposedDeadline, proposal.Message),
                    VendorStatus.Negotiate => CorpServeEmailTemplateFactory.BuildVendorNegotiateProposal(clientName, vendorName, requestTitle, proposal.ProposedPrice, proposal.ProposedDeadline, proposal.Message),
                    _ => CorpServeEmailTemplateFactory.BuildVendorRejectProposal(clientName, vendorName, requestTitle, proposal.Message)
                };

                await _emailService.SendEmailAsync(clientEmail, template.Subject, template.Body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send proposal email for proposal {ProposalId}.", proposal.Id);
            }
        }

        private async Task TrySendClientRejectEmailAsync(Proposal proposal, string reason)
        {
            try
            {
                var vendorName = proposal.Vendor?.FullName ?? "Vendor";
                var clientName = proposal.Request.Client?.FullName ?? "Client";
                var requestTitle = proposal.Request.Title;
                var vendorEmail = proposal.Vendor?.Email;

                if (string.IsNullOrWhiteSpace(vendorEmail))
                    return;

                if (!IsEmailNotificationEnabled(proposal.Vendor))
                    return;

                var template = CorpServeEmailTemplateFactory.BuildClientRejectProposal(vendorName, clientName, requestTitle, reason);
                await _emailService.SendEmailAsync(vendorEmail, template.Subject, template.Body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send client-reject email for proposal {ProposalId}.", proposal.Id);
            }
        }

        private Task<bool> IsUserSuspendedAsync(string userId) =>
            _userManager.Users.AnyAsync(u => u.Id == userId && u.Status == UserStatus.Suspended);

        private static bool IsEmailNotificationEnabled(ApplicationUser? user)
            => user?.UserPreference?.EmailNotification ?? true;

        private void LogNotificationFailure(string flow, IReadOnlyList<Error> errors)
        {
            _logger.LogWarning(
                "Notification failed in {Flow}. Errors: {Errors}",
                flow,
                string.Join(" | ", errors.Select(e => $"{e.Code}:{e.Description}")));
        }

        private static void ThrowIfNotificationFailed(Result<bool> result, string flow)
        {
            if (result.IsSuccess)
                return;

            throw new InvalidOperationException($"Notification failed in {flow}: {string.Join(" | ", result.Errors.Select(e => $"{e.Code}:{e.Description}"))}");
        }

        private async Task EnrichProposalDtosAsync(IReadOnlyList<ProposalDTO> items)
        {
            if (items.Count == 0)
                return;

            var ids = items
                .SelectMany(p => new[] { p.VendorId, p.ClientId })
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var pics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, ids);
            foreach (var p in items)
            {
                if (!string.IsNullOrWhiteSpace(p.VendorId) && pics.TryGetValue(p.VendorId, out var vp) && !string.IsNullOrWhiteSpace(vp))
                    p.VendorProfilePictureUrl = vp;
                if (!string.IsNullOrWhiteSpace(p.ClientId) && pics.TryGetValue(p.ClientId, out var cp) && !string.IsNullOrWhiteSpace(cp))
                    p.ClientProfilePictureUrl = cp;
            }
        }

        private async Task EnrichSlaContractDtoAsync(SLAContractDTO dto)
        {
            var pics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, new[] { dto.ClientId, dto.VendorId });
            if (pics.TryGetValue(dto.ClientId, out var c) && !string.IsNullOrWhiteSpace(c))
                dto.ClientProfilePictureUrl = c;
            if (pics.TryGetValue(dto.VendorId, out var v) && !string.IsNullOrWhiteSpace(v))
                dto.VendorProfilePictureUrl = v;
        }

        #endregion
    }
}
