namespace CorpServe.Shared.DTOs.CategoryDTOs
{
    public class CategoriesDTO
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int VendorCount { get; set; }
    }
}
