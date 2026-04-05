using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.RequestModule
{
    public class RequestProgress : BaseEntity<string>
    {
        public int Percentage { get; set; }
        public string Description { get; set; } = default!;
        public DateTime UpdatedAt { get; set; }
        #region RelationShips

        #region Vendor - RequestProgess (1-M)

        public string VendorId { get; set; } = default!;
        public ApplicationUser Vendor { get; set; } = default!;

        #endregion

        #endregion
    }
}
