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
        #endregion
    }
    }
