namespace CorpServe.Shared.DTOs.CategoryDTOs
{
    public class CategoriesDTO
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int VendorCount { get; set; }
        public int RequestCount { get; set; }
        public int DemandMeter { get; set; }
        public int DemandRank { get; set; }
    }
}
