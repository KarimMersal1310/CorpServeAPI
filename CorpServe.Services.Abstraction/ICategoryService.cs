using CorpServe.Shared.DTOs.CategoryDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Shared.CommonResult;
using EventHub.Shared;

namespace CorpServe.Services.Abstraction
{
    public interface ICategoryService
    {
        Task<Result<IEnumerable<CategoryLookupDTO>>> GetAllCategoriesAsync();
        Task<PaginatedResult<CategoriesDTO>> GetAllCategoriesAsync(CategoryQuaryParams quaryParams);
        Task<Result<CategoriesDTO>> CreateCategoryAsync(string adminId, CreateUpdateCategoryDTO createCategoryDTO);
        Task<Result<bool>> UpdateCategoryAsync(string categoryId, CreateUpdateCategoryDTO updateCategoryDTO);
        Task<Result<bool>> DeleteCategoryAsync(string categoryId);
    }
}
