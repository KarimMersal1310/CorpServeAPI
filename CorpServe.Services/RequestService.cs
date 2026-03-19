using AutoMapper;
using CorpServe.Domain.Entities.AIEstimateModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using CorpServe.Shared.QueryParams;
using EventHub.Domain.Contracts;
using EventHub.Services.Specifications;
using EventHub.Shared;

namespace CorpServe.Services
{
    public class RequestService : IRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAIEstimationService _aiEstimationService;
        private readonly IMapper _mapper;

        public RequestService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IAIEstimationService aiEstimationService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _aiEstimationService = aiEstimationService;
            _mapper = mapper;
        }

        public async Task<Result<RequestDTO>> CreateRequestAsync(string clientId, CreateRequestDTO createRequestDTO)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Request.ClientRequired", "Client identity is required.");

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
            var categoryExists = await categoryRepo.AnyAsync(c => c.Id == createRequestDTO.CategoryId);
            if (!categoryExists)
                return Error.NotFound("Request.CategoryNotFound", "Category not found.");

            var request = new Request
            {
                Title = createRequestDTO.Title.Trim(),
                Discription = createRequestDTO.Description.Trim(),
                CateogryId = createRequestDTO.CategoryId,
                ClientId = clientId,
                BudgetMin = createRequestDTO.BudgetMin,
                BudgetMax = createRequestDTO.BudgetMax,
                ExpectedDeadline = createRequestDTO.ExpectedDeadline,
                CreatedAt = DateTime.UtcNow,
                RequestStatus = RequestStatus.Pending,
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

            if (createRequestDTO.Attachments is not null)
            {
                foreach (var file in createRequestDTO.Attachments.Where(f => f is not null && f.Length > 0))
                {
                    var fileUrl = await _fileStorageService.UploadAsync(file, "uploads/request-attachments");
                    request.RequestAttachments!.Add(new RequestAttachment
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

            return _mapper.Map<RequestDTO>(createdRequest);
        }

        public async Task<Result<AIEstimationDTO>> GenerateEstimateAsync(string clientId, GenerateRequestEstimateDTO estimateDTO)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Error.Unauthorized("Request.ClientRequired", "Client identity is required.");

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
    }
}
