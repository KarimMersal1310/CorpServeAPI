using AutoMapper;
using CorpServe.Domain.Entities.AIEstimateModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Domain.Contracts;
using CorpServe.Services.Specifications;
using CorpServe.Shared;
using CorpServe.Shared.Notifications;
using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class RequestService : IRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAIEstimationService _aiEstimationService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<RequestService> _logger;

        public RequestService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IAIEstimationService aiEstimationService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IPaymentService paymentService,
            ILogger<RequestService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _aiEstimationService = aiEstimationService;
            _mapper = mapper;
            _userManager = userManager;
            _notificationService = notificationService;
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<Result<RequestDTO>> CreateRequestAsync(string clientId, CreateRequestDTO createRequestDTO)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Request.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            var hasUnpaidCompleted = await _paymentService.HasUnpaidCompletedRequestsAsync(clientId);
            if (hasUnpaidCompleted.IsFailure)
                return hasUnpaidCompleted.Errors.ToList();

            if (hasUnpaidCompleted.Value)
                return Error.Conflict("Payment.UnpaidCompletedRequestExists", "You must complete payment for previous completed requests before creating a new request.");

            var trimmedTitle = createRequestDTO.Title?.Trim() ?? string.Empty;
            var trimmedDescription = createRequestDTO.Description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedTitle) || string.IsNullOrWhiteSpace(trimmedDescription))
                return Error.Validation("Request.MissingRequestData", "Title and Description are required.");

            if (trimmedTitle.Length > 200)
                return Error.Validation("Request.TitleTooLong", "Title must be 200 characters or less.");

            if (trimmedDescription.Length > 500)
                return Error.Validation("Request.DescriptionTooLong", "Description must be 500 characters or less.");

            if (string.IsNullOrWhiteSpace(createRequestDTO.CategoryId))
                return Error.Validation("Request.CategoryRequired", "CategoryId is required.");

            if (createRequestDTO.BudgetMin <= 0 || createRequestDTO.BudgetMax <= 0 || createRequestDTO.BudgetMin > createRequestDTO.BudgetMax)
                return Error.Validation("Request.InvalidBudget", "Budget range is invalid.");

            if (createRequestDTO.ExpectedDeadline <= DateTime.UtcNow)
                return Error.Validation("Request.InvalidDeadline", "Expected deadline must be in the future.");

            var hasEstimateData = createRequestDTO.EstimatedCost.HasValue
                || createRequestDTO.EstimatedTime.HasValue
                || createRequestDTO.Confidence.HasValue;

            if (hasEstimateData && (!createRequestDTO.EstimatedCost.HasValue || !createRequestDTO.EstimatedTime.HasValue || !createRequestDTO.Confidence.HasValue))
                return Error.Validation("Request.IncompleteEstimate", "EstimatedCost, EstimatedTime and Confidence must be provided together.");

            if (createRequestDTO.EstimatedCost.HasValue && createRequestDTO.EstimatedCost.Value <= 0)
                return Error.Validation("Request.InvalidEstimatedCost", "Estimated cost must be greater than zero.");

            if (createRequestDTO.Confidence.HasValue && (createRequestDTO.Confidence.Value < 0 || createRequestDTO.Confidence.Value > 100))
                return Error.Validation("Request.InvalidConfidence", "Confidence must be between 0 and 100.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var category = await categoryRepo.GetByIdAsync(new CategoryByIdSpecification(createRequestDTO.CategoryId));
            if (category is null)
                return Error.NotFound("Request.CategoryNotFound", "Category not found.");

            // Mandatory AI clarity review before creating a request to ensure vendors receive actionable details.
            var clarityReview = await _aiEstimationService.ReviewRequestClarityAsync(new GenerateRequestEstimateDTO
            {
                Title = trimmedTitle,
                Description = trimmedDescription,
                CategoryId = createRequestDTO.CategoryId,
                BudgetMin = createRequestDTO.BudgetMin,
                BudgetMax = createRequestDTO.BudgetMax,
                ExpectedDeadline = createRequestDTO.ExpectedDeadline
            });

            if (clarityReview.IsFailure)
                return clarityReview.Errors.ToList();

            var request = new Request
            {
                Title = trimmedTitle,
                Discription = trimmedDescription,
                CateogryId = createRequestDTO.CategoryId,
                ClientId = clientId,
                BudgetMin = createRequestDTO.BudgetMin,
                BudgetMax = createRequestDTO.BudgetMax,
                ExpectedDeadline = createRequestDTO.ExpectedDeadline,
                CreatedAt = DateTime.UtcNow,
                RequestStatus = RequestStatus.Pending,
                RequestAttachments = new List<RequestAttachment>(),
                RequestProgress = new RequestProgress
                {
                    Percentage = 0,
                    Description = "Request created",
                    UpdatedAt = DateTime.UtcNow
                }
            };

            if (createRequestDTO.EstimatedCost.HasValue)
            {
                request.AIEstimation = new AIEstimation
                {
                    EstimatedCost = createRequestDTO.EstimatedCost.Value,
                    EstimatedTime = createRequestDTO.EstimatedTime!.Value,
                    Confidence = createRequestDTO.Confidence!.Value,
                    CreatedAt = DateTime.UtcNow
                };
            }

            if (createRequestDTO.Attachments is { Length: > 0 })
            {
                foreach (var file in createRequestDTO.Attachments.Where(f => f is not null && f.Length > 0))
                {
                    var fileUrl = await _fileStorageService.UploadAsync(file, "uploads/request-attachments");
                    request.RequestAttachments.Add(new RequestAttachment
                    {
                        FileUrl = fileUrl
                    });
                }
            }

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            await requestRepo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            var requestSpecification = new RequestByIdForClientSpecification(request.Id, clientId);
            var createdRequest = await requestRepo.GetByIdAsync(requestSpecification);
            if (createdRequest is null)
                return Error.Failure("Request.CreateFailed", "Request created but failed to load its data.");

            var clientNotification = await _notificationService.SendNotificationAsync(
                clientId,
                NotificationTitles.RequestCreated,
                $"Your request '{createdRequest.Title}' was created and is now pending vendor proposals.",
                NotificationTypes.Success,
                createdRequest.Id,
                "Request");

            if (clientNotification.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to notify client {ClientId} for request creation {RequestId}. Errors: {Errors}",
                    clientId,
                    createdRequest.Id,
                    string.Join(" | ", clientNotification.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }

            var candidateVendorIds = category.VendorCategories
                .Select(vc => vc.VendorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (candidateVendorIds.Count > 0)
            {
                var activeVendorIds = await _userManager.Users
                    .Where(u => candidateVendorIds.Contains(u.Id) && u.Status == UserStatus.Active)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (activeVendorIds.Count > 0)
                {
                    var notifyVendorsResult = await _notificationService.SendNotificationToManyAsync(
                        activeVendorIds,
                        NotificationTitles.NewRequestAvailable,
                        $"New request '{createdRequest.Title}' is available in your category.",
                        NotificationTypes.Info,
                        createdRequest.Id,
                        "Request");

                    if (notifyVendorsResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "Failed to notify some vendors for request {RequestId}. Errors: {Errors}",
                            createdRequest.Id,
                            string.Join(" | ", notifyVendorsResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    }
                }
            }

            return _mapper.Map<RequestDTO>(createdRequest);
        }

        public async Task<Result<RequestDTO>> UpdateRequestAsync(string clientId, string requestId, UpdateRequestDTO updateRequestDTO)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Request.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Request.IdRequired", "Request ID is required.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(new RequestByIdForClientSpecification(requestId, clientId));
            if (request is null)
                return Error.NotFound("Request.NotFound", "Request not found.");

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var hasAnyProposals = await proposalRepo.AnyAsync(p => p.RequestId == requestId);
            if (hasAnyProposals)
                return Error.Conflict("Request.HasProposals", "Request cannot be edited after receiving proposals.");

            var trimmedTitle = updateRequestDTO.Title?.Trim() ?? string.Empty;
            var trimmedDescription = updateRequestDTO.Description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedTitle) || string.IsNullOrWhiteSpace(trimmedDescription))
                return Error.Validation("Request.MissingRequestData", "Title and Description are required.");

            if (trimmedTitle.Length > 200)
                return Error.Validation("Request.TitleTooLong", "Title must be 200 characters or less.");

            if (trimmedDescription.Length > 500)
                return Error.Validation("Request.DescriptionTooLong", "Description must be 500 characters or less.");

            if (string.IsNullOrWhiteSpace(updateRequestDTO.CategoryId))
                return Error.Validation("Request.CategoryRequired", "CategoryId is required.");

            if (updateRequestDTO.BudgetMin <= 0 || updateRequestDTO.BudgetMax <= 0 || updateRequestDTO.BudgetMin > updateRequestDTO.BudgetMax)
                return Error.Validation("Request.InvalidBudget", "Budget range is invalid.");

            if (updateRequestDTO.ExpectedDeadline <= DateTime.UtcNow)
                return Error.Validation("Request.InvalidDeadline", "Expected deadline must be in the future.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var categoryExists = await categoryRepo.AnyAsync(c => c.Id == updateRequestDTO.CategoryId);
            if (!categoryExists)
                return Error.NotFound("Request.CategoryNotFound", "Category not found.");

            request.Title = trimmedTitle;
            request.Discription = trimmedDescription;
            request.CateogryId = updateRequestDTO.CategoryId;
            request.BudgetMin = updateRequestDTO.BudgetMin;
            request.BudgetMax = updateRequestDTO.BudgetMax;
            request.ExpectedDeadline = updateRequestDTO.ExpectedDeadline;

            var attachmentRepo = _unitOfWork.GetRepository<RequestAttachment, string>();
            var attachmentIdsToRemove = updateRequestDTO.AttachmentIdsToRemove?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            if (attachmentIdsToRemove.Count > 0)
            {
                var attachmentsToRemove = request.RequestAttachments?
                    .Where(a => attachmentIdsToRemove.Contains(a.Id, StringComparer.OrdinalIgnoreCase))
                    .ToList() ?? [];

                if (attachmentsToRemove.Count != attachmentIdsToRemove.Count)
                    return Error.Validation("Request.InvalidAttachmentIds", "One or more attachments to remove were not found on this request.");

                foreach (var attachment in attachmentsToRemove)
                {
                    request.RequestAttachments!.Remove(attachment);
                    attachmentRepo.Remove(attachment);

                    try
                    {
                        await _fileStorageService.DeleteAsync(attachment.FileUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete attachment file {FileUrl} for request {RequestId}.", attachment.FileUrl, requestId);
                    }
                }
            }

            if (updateRequestDTO.NewAttachments is not null)
            {
                request.RequestAttachments ??= new List<RequestAttachment>();

                foreach (var file in updateRequestDTO.NewAttachments.Where(f => f is not null && f.Length > 0))
                {
                    var fileUrl = await _fileStorageService.UploadAsync(file, "uploads/request-attachments");
                    request.RequestAttachments.Add(new RequestAttachment
                    {
                        FileUrl = fileUrl
                    });
                }
            }

            requestRepo.Update(request);
            await _unitOfWork.SaveChangesAsync();

            var updatedRequest = await requestRepo.GetByIdAsync(new RequestByIdForClientSpecification(requestId, clientId));
            if (updatedRequest is null)
                return Error.Failure("Request.UpdateFailed", "Request updated but failed to load its data.");

            var updateNotification = await _notificationService.SendNotificationAsync(
                clientId,
                NotificationTitles.RequestUpdated,
                $"Your request '{updatedRequest.Title}' was updated successfully.",
                NotificationTypes.Info,
                updatedRequest.Id,
                "Request");

            if (updateNotification.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to notify client {ClientId} for request update {RequestId}. Errors: {Errors}",
                    clientId,
                    updatedRequest.Id,
                    string.Join(" | ", updateNotification.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }

            var categoryForVendors = await categoryRepo.GetByIdAsync(new CategoryByIdSpecification(updatedRequest.CateogryId));
            if (categoryForVendors is not null)
            {
                var candidateVendorIds = categoryForVendors.VendorCategories
                    .Select(vc => vc.VendorId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (candidateVendorIds.Count > 0)
                {
                    var activeVendorIds = new List<string>();
                    foreach (var vendorId in candidateVendorIds)
                    {
                        var vendor = await _userManager.FindByIdAsync(vendorId);
                        if (vendor?.Status == UserStatus.Active)
                            activeVendorIds.Add(vendorId);
                    }

                    if (activeVendorIds.Count > 0)
                    {
                        var notifyVendorsResult = await _notificationService.SendNotificationToManyAsync(
                            activeVendorIds,
                            NotificationTitles.RequestUpdated,
                            $"Request '{updatedRequest.Title}' was updated by the client. Review the latest details.",
                            NotificationTypes.Info,
                            updatedRequest.Id,
                            "Request");

                        if (notifyVendorsResult.IsFailure)
                        {
                            _logger.LogWarning(
                                "Failed to notify some vendors for request update {RequestId}. Errors: {Errors}",
                                updatedRequest.Id,
                                string.Join(" | ", notifyVendorsResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
                        }
                    }
                }
            }

            return _mapper.Map<RequestDTO>(updatedRequest);
        }

        public async Task<Result<bool>> DeleteRequestAsync(string clientId, string requestId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Request.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Request.IdRequired", "Request ID is required.");

            var proposalRepo = _unitOfWork.GetRepository<Proposal, string>();
            var hasAnyProposals = await proposalRepo.AnyAsync(p => p.RequestId == requestId);
            if (hasAnyProposals)
                return Error.Conflict("Request.HasProposals", "Request cannot be deleted after receiving proposals.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(new RequestByIdForClientSpecification(requestId, clientId));
            if (request is null)
                return Error.NotFound("Request.NotFound", "Request not found.");

            if (request.RequestAttachments is not null)
            {
                foreach (var attachment in request.RequestAttachments)
                {
                    try
                    {
                        await _fileStorageService.DeleteAsync(attachment.FileUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete attachment file {FileUrl} while deleting request {RequestId}.", attachment.FileUrl, requestId);
                    }
                }
            }

            requestRepo.Remove(request);
            await _unitOfWork.SaveChangesAsync();

            var deleteNotification = await _notificationService.SendNotificationAsync(
                clientId,
                NotificationTitles.RequestDeleted,
                "Your request was deleted successfully.",
                NotificationTypes.Info,
                requestId,
                "Request");

            if (deleteNotification.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to notify client {ClientId} for request delete {RequestId}. Errors: {Errors}",
                    clientId,
                    requestId,
                    string.Join(" | ", deleteNotification.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }

            return true;
        }

        public async Task<Result<bool>> VendorUpdateRequestProgressAsync(string vendorId, string requestId, UpdateRequestProgressDTO updateRequestProgressDTO)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return Error.Unauthorized("Request.VendorRequired", "Vendor identity is required.");

            if (await IsUserSuspendedAsync(vendorId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Request.IdRequired", "Request ID is required.");

            var trimmedDescription = updateRequestProgressDTO.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedDescription))
                return Error.Validation("Request.ProgressDescriptionRequired", "Progress description is required.");

            if (trimmedDescription.Length > 500)
                return Error.Validation("Request.ProgressDescriptionTooLong", "Progress description must be 500 characters or less.");

            if (updateRequestProgressDTO.Percentage < 0 || updateRequestProgressDTO.Percentage > 100)
                return Error.Validation("Request.InvalidProgress", "Progress percentage must be between 0 and 100.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var request = await requestRepo.GetByIdAsync(new ActiveRequestByVendorSlaSpecification(requestId, vendorId));
            if (request is null)
                return Error.NotFound("Request.ActiveSlaNotFound", "Active request with SLA for this vendor was not found.");

            if (request.SLAContract is null)
                return Error.Validation("Request.SlaRequired", "Request must have an SLA contract.");

            request.RequestProgress.Percentage = updateRequestProgressDTO.Percentage;
            request.RequestProgress.Description = trimmedDescription;
            request.RequestProgress.UpdatedAt = DateTime.UtcNow;
            request.RequestProgress.VendorId = vendorId;

            if (updateRequestProgressDTO.Percentage == 100)
            {
                request.RequestStatus = RequestStatus.Completed;
                request.SLAContract.SLAStatus = SLAStatus.Completed;
            }

            requestRepo.Update(request);
            await _unitOfWork.SaveChangesAsync();

            var clientNotificationResult = await _notificationService.SendNotificationAsync(
                request.ClientId,
                NotificationTitles.RequestProgressUpdated,
                $"Vendor updated progress for request '{request.Title}' to {updateRequestProgressDTO.Percentage}%.",
                NotificationTypes.Info,
                request.Id,
                "Request");

            if (clientNotificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to notify client {ClientId} for request progress update {RequestId}. Errors: {Errors}",
                    request.ClientId,
                    request.Id,
                    string.Join(" | ", clientNotificationResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }

            if (updateRequestProgressDTO.Percentage == 100)
            {
                var completionNotificationResult = await _notificationService.SendNotificationToManyAsync(
                    [request.ClientId, vendorId],
                    NotificationTitles.SlaCompleted,
                    $"Request '{request.Title}' is completed and SLA contract is marked completed.",
                    NotificationTypes.Success,
                    request.Id,
                    "Request");

                if (completionNotificationResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to notify completion for request {RequestId}. Errors: {Errors}",
                        request.Id,
                        string.Join(" | ", completionNotificationResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
                }

                var paymentPreparationResult = await _paymentService.PreparePaymentForCompletedRequestAsync(request.Id, request.ClientId);
                if (paymentPreparationResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to prepare payment for completed request {RequestId}. Errors: {Errors}",
                        request.Id,
                        string.Join(" | ", paymentPreparationResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
                }
            }

            return true;
        }

        public async Task<Result<AIEstimationDTO>> GenerateEstimateAsync(string clientId, GenerateRequestEstimateDTO estimateDTO)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Request.ClientRequired", "Client identity is required.");

            if (await IsUserSuspendedAsync(clientId))
                return Error.Unauthorized("User.Suspended", "Your account is suspended.");

            if (estimateDTO.BudgetMin <= 0 || estimateDTO.BudgetMax <= 0 || estimateDTO.BudgetMin > estimateDTO.BudgetMax)
                return Error.Validation("Request.InvalidBudget", "Budget range is invalid.");

            if (estimateDTO.ExpectedDeadline <= DateTime.UtcNow)
                return Error.Validation("Request.InvalidDeadline", "Expected deadline must be in the future.");

            if (string.IsNullOrWhiteSpace(estimateDTO.CategoryId))
                return Error.Validation("Request.CategoryRequired", "CategoryId is required.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var categoryExists = await categoryRepo.AnyAsync(c => c.Id == estimateDTO.CategoryId);
            if (!categoryExists)
                return Error.NotFound("Request.CategoryNotFound", "Category not found.");

            if (string.IsNullOrWhiteSpace(estimateDTO.Title) || string.IsNullOrWhiteSpace(estimateDTO.Description))
                return Error.Validation("Request.MissingEstimationData", "Title and Description are required to generate estimate.");

            var estimationResult = await _aiEstimationService.GenerateEstimateAsync(estimateDTO);
            if (estimationResult.IsFailure)
                return estimationResult.Errors.ToList();

            return estimationResult.Value;
        }

        public async Task<PaginatedResult<RequestDTO>> GetClientRequestsAsync(string clientId, RequestQueryParams queryParams)
        {
            if (string.IsNullOrWhiteSpace(clientId) || await IsUserSuspendedAsync(clientId))
                return new PaginatedResult<RequestDTO>(queryParams.PageIndex, queryParams.PageSize, 0, []);

            var requestRepo = _unitOfWork.GetRepository<Request, string>();

            var listSpecification = new ClientRequestListSpecification(
                clientId,
                queryParams.Search,
                queryParams.RequestStatus,
                queryParams.CategoryId,
                queryParams.SortByCategory,
                queryParams.SortDescending,
                queryParams.PageSize,
                queryParams.PageIndex);

            var countSpecification = new ClientRequestCountSpecification(
                clientId,
                queryParams.Search,
                queryParams.RequestStatus,
                queryParams.CategoryId);

            var requests = await requestRepo.GetAllAsync(listSpecification);
            var count = await requestRepo.CountAsync(countSpecification);

            var data = _mapper.Map<List<RequestDTO>>(requests);
            return new PaginatedResult<RequestDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }

        public async Task<PaginatedResult<VendorRequestViewDTO>> GetRequestsForVendor(string vendorId, RequestQueryParams queryParams)
        {
            if (string.IsNullOrWhiteSpace(vendorId) || await IsUserSuspendedAsync(vendorId))
                return new PaginatedResult<VendorRequestViewDTO>(queryParams.PageIndex, queryParams.PageSize, 0, []);

            var requestRepo = _unitOfWork.GetRepository<Request, string>();

            var listSpecification = new VendorRequestListSpecification(
                vendorId,
                queryParams.Search,
                queryParams.CategoryId,
                queryParams.SortByCategory,
                queryParams.SortDescending,
                queryParams.PageSize,
                queryParams.PageIndex);

            var countSpecification = new VendorRequestCountSpecification(
                vendorId,
                queryParams.Search,
                queryParams.CategoryId);

            var requests = await requestRepo.GetAllAsync(listSpecification);
            var count = await requestRepo.CountAsync(countSpecification);

            var data = _mapper.Map<List<VendorRequestViewDTO>>(requests);

            return new PaginatedResult<VendorRequestViewDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }

        private Task<bool> IsUserSuspendedAsync(string userId) =>
            _userManager.Users.AnyAsync(u => u.Id == userId && u.Status == UserStatus.Suspended);
    }
}
