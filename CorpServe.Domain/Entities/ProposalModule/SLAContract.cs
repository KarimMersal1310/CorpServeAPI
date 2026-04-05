using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.ProposalModule
{
    public class SLAContract : BaseEntity<string>
    {
        public decimal ContractPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }
        public SLAStatus SLAStatus { get; set; }

        #region RelationShips

        #region Vendor - SLAContract (1-M)

        public string VendorId { get; set; } = default!;
        public ApplicationUser Vendor { get; set; } = default!;

        #endregion

        #region Client - SLAContract (1-M)

        public string ClientId { get; set; } = default!;
        public ApplicationUser Client { get; set; } = default!;

        #endregion

        #region Request - SLAContract (1-1)

        public string RequestId { get; set; } = default!;
        public Request Request { get; set; } = default!;

        #endregion

        #region SLAContract - Proposal (1-1)

        public string ProposalId { get; set; } = default!;
        public Proposal Proposal { get; set; } = default!;


        #endregion

        #endregion
    }
}
