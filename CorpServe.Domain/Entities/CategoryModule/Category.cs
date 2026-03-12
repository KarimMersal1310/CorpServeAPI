using CorpServe.Domain.Entities.VendorVerifyModule;
using EventHub.Domain.Entities;
using System.Collections.Generic;

namespace CorpServe.Domain.Entities.SpecializedCategoryModule
{
    public class Category : BaseEntity<string>
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        #region RelationShips

        #region Category - VendorCategory
        public ICollection<VendorCategory> VendorCategories { get; set; } = new List<VendorCategory>();

        #endregion  

        #endregion
    }
}
