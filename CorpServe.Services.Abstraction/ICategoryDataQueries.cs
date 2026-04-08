using CorpServe.Shared.DTOs.CategoryDTOs;
using CorpServe.Shared.QueryParams;

namespace CorpServe.Services.Abstraction
{
    public interface ICategoryDataQueries
    {
        Task<CategoryAdminManageDTO> GetAdminManageAsync(CategoryQuaryParams queryParams, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetInvalidCategoryIdsAsync(IReadOnlyList<string> candidateIds, CancellationToken cancellationToken = default);
    }
}
