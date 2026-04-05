namespace CorpServe.Shared.DTOs.CategoryDTOs
{
    public class CategoryAdminSummaryDTO
    {
        public int TotalCategories { get; set; }
        public int TotalVendors { get; set; }
        public int AverageRequests { get; set; }
        public string TopCategoryName { get; set; } = string.Empty;
        public int TopCategoryRequestCount { get; set; }
    }
}
