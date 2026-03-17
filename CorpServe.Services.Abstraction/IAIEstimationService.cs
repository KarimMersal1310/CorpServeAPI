using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;

namespace CorpServe.Services.Abstraction
{
    public interface IAIEstimationService
    {
        Task<Result<AIEstimationDTO>> GenerateEstimateAsync(GenerateRequestEstimateDTO estimateDTO);
    }
}
