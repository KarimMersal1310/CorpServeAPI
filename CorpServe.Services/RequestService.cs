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

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var categoryExists = await categoryRepo.AnyAsync(c => c.Id == estimateDTO.CategoryId);
            if (!categoryExists)
                return Error.NotFound("Request.CategoryNotFound", "Category not found.");

            Request? request = null;
            if (!string.IsNullOrWhiteSpace(estimateDTO.RequestId))
            {
                var requestRepoForValidation = _unitOfWork.GetRepository<Request, string>();
                var requestSpecification = new RequestByIdForClientSpecification(estimateDTO.RequestId, clientId);
                request = await requestRepoForValidation.GetByIdAsync(requestSpecification);
                if (request is null)
                    return Error.NotFound("Request.NotFound", "Request not found for this client.");
            }

            var estimationResult = await _aiEstimationService.GenerateEstimateAsync(estimateDTO);
            if (estimationResult.IsFailure)
                return estimationResult.Errors.ToList();

            if (request is not null)
            {
                var now = DateTime.UtcNow;
                if (request.AIEstimation is null)
                {
                    request.AIEstimation = new AIEstimation
                    {
                        EstimatedCost = estimationResult.Value.EstimatedCost,
                        EstimatedTime = estimationResult.Value.EstimatedTime,
                        Confidence = estimationResult.Value.Confidence,
                        CreatedAt = now,
                        RequestId = request.Id
                    };
                }
                else
                {
                    request.AIEstimation.EstimatedCost = estimationResult.Value.EstimatedCost;
                    request.AIEstimation.EstimatedTime = estimationResult.Value.EstimatedTime;
                    request.AIEstimation.Confidence = estimationResult.Value.Confidence;
                    request.AIEstimation.CreatedAt = now;
                }

                var requestRepo = _unitOfWork.GetRepository<Request, string>();
                requestRepo.Update(request);
                await _unitOfWork.SaveChangesAsync();
            }

            return estimationResult.Value;
        }

        public async Task<PaginatedResult<RequestDTO>> GetClientRequestsAsync(string clientId, RequestQueryParams queryParams)
        {
            var requestRepo = _unitOfWork.GetRepository<Request, string>();

            var listSpecification = new ClientRequestListSpecification(
                clientId,
                queryParams.Search,
                queryParams.RequestStatus,
                queryParams.PageSize,
                queryParams.PageIndex);

            var countSpecification = new ClientRequestCountSpecification(
                clientId,
                queryParams.Search,
                queryParams.RequestStatus);

            var requests = await requestRepo.GetAllAsync(listSpecification);
            var count = await requestRepo.CountAsync(countSpecification);

            var data = _mapper.Map<List<RequestDTO>>(requests);
            return new PaginatedResult<RequestDTO>(queryParams.PageIndex, queryParams.PageSize, count, data);
        }
    }
}
