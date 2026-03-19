using EventHub.Shared;

namespace CorpServe.Shared.DTOs.CategoryDTOs
{
    public class CategoryAdminManageDTO
    {
        public CategoryAdminSummaryDTO Summary { get; set; } = new();
        public PaginatedResult<CategoriesDTO> Categories { get; set; } = new(1, 1, 0, []);
    }
}
