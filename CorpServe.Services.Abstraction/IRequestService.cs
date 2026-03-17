using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using CorpServe.Shared.QueryParams;
using EventHub.Shared;

namespace CorpServe.Services.Abstraction
{
    public interface IRequestService
    {
        Task<Result<RequestDTO>> CreateRequestAsync(string clientId, CreateRequestDTO createRequestDTO);
        Task<Result<AIEstimationDTO>> GenerateEstimateAsync(string clientId, GenerateRequestEstimateDTO estimateDTO);
        Task<PaginatedResult<RequestDTO>> GetClientRequestsAsync(string clientId, RequestQueryParams queryParams);
    }
}
