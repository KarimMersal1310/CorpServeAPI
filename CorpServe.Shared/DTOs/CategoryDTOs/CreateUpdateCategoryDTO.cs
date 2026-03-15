namespace CorpServe.Shared.DTOs.CategoryDTOs
{
    public class CreateUpdateCategoryDTO
    {
        public string CategoryName { get; set; } = default!;
        public string? Description { get; set; }
    }
}
