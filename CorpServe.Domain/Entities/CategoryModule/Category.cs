using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Domain.Entities;
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

        #region Category - Admin
        public string AdminId { get; set; } = default!;
        public ApplicationUser AdminUser { get; set; } = default!;
        #endregion

        #region Request - Category (many-to-one)

        public ICollection<Request> Requests { get; set; } = new List<Request>();

        #endregion

        #endregion
    }
}
