using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace CorpServe.Domain.Entities.IdentityModule
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = default!;
        public UserStatus Status { get; set; }

        #region RelationShips
        #region Vendor - VendorCategory
        public ICollection<VendorCategory> VendorCategories { get; set; } = new List<VendorCategory>();

        #endregion

        #region User - UserPreference
        public UserPreference UserPreference { get; set; } = new();
        #endregion

        #region Admin - Category

        public ICollection<Category> Categories { get; set; } = new List<Category>();

        #endregion

        #region Request - Client (many-to-one)

        public ICollection<Request> Requests { get; set; } = new List<Request>();

        #endregion
        #endregion
    }
}
