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
    public class Proposal : BaseEntity<string>
    {
        public ClientStatus ProposalStatus { get; set; }
        public VendorStatus ProposalType { get; set; }
        public string? Message { get; set; }
        public decimal? ProposedPrice { get; set; } // must vendor sent it if accept or negotiate
        public DateTime? ProposedDeadline { get; set; } // must vendor sent it if accept or negotiate
        public DateTime CreatedAt { get; set; }
        public DateTime? ClientResponseAt { get; set; }
        public bool IsSelected { get; set; } = false; // from client side to select the proposal that he want to accept it

        #region RelationShips

        #region Vendor - Proposal  (1-M)

        public string VendorId { get; set; } = default!;
        public ApplicationUser Vendor { get; set; } = default!;

        #endregion

        #region Proposal - Request (1-1)
        public string RequestId { get; set; } = default!;
        public Request Request { get; set; } = default!;

        #endregion

        #region Proposal - SLAContract (1-1)

        public SLAContract? SLAContract { get; set; } = default!;

        #endregion

        #endregion

    }
}
