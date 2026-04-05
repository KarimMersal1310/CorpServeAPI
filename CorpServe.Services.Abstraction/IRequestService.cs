using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Shared;

namespace CorpServe.Services.Abstraction
{
    public interface IRequestService
    {
        Task<Result<RequestDTO>> CreateRequestAsync(string clientId, CreateRequestDTO createRequestDTO);
        Task<Result<RequestDTO>> UpdateRequestAsync(string clientId, string requestId, UpdateRequestDTO updateRequestDTO);
        Task<Result<bool>> DeleteRequestAsync(string clientId, string requestId);
        Task<Result<bool>> VendorUpdateRequestProgressAsync(string vendorId, string requestId, UpdateRequestProgressDTO updateRequestProgressDTO);
        Task<Result<AIEstimationDTO>> GenerateEstimateAsync(string clientId, GenerateRequestEstimateDTO estimateDTO);
        Task<PaginatedResult<RequestDTO>> GetClientRequestsAsync(string clientId, RequestQueryParams queryParams);
        Task<PaginatedResult<VendorRequestViewDTO>> GetRequestsForVendor(string vendorId, RequestQueryParams queryParams);
    }
}
