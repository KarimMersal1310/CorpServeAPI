using CorpServe.Domain.Entities.IdentityModule;

namespace CorpServe.Domain.Entities.SpecializedCategoryModule
{
    public class VendorCategory
    {
        public string VendorId { get; set; } = default!;
        public string CategoryId { get; set; } = default!;

        // Navigation properties
        public ApplicationUser Vendor { get; set; } = default!;
        public Category Category { get; set; } = default!;
    }
}
