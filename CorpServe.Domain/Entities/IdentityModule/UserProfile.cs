using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.IdentityModule
{
    public class UserProfile : BaseEntity<string>
    {
        public string CompanyName { get; set; } = default!;
        public string CompanyLocation { get; set; } = default!;
        public string ProfilePictureUrl { get; set; } = default!;
        public decimal? VendorStars { get; set; } // Stars from 5
        public string Description { get; set; } = default!;

        #region RelationShips

        #region User - UserProfile
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;
        #endregion

        #region Document - UserProfile
        public ICollection<ProfileDocument>? Documents { get; set; } = new List<ProfileDocument>();
        #endregion
        #endregion

    }
}
