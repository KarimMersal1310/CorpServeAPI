using CorpServe.Domain.Entities.AIEstimateModule;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.RequestModule
{
    public class Request :BaseEntity<string>
    {
        public string Title { get; set; } = default!;
        public string Discription { get; set; } = default!;
        public decimal BudgetMin { get; set; } 
        public decimal BudgetMax { get; set; }
        public DateTime ExpectedDeadline { get; set; }
        public DateTime CreatedAt { get; set; } 
        public RequestStatus RequestStatus { get; set; } = default!;

        #region RelationShips

        #region Request - RequestAttachment (One-to-Many)

        public ICollection<RequestAttachment>? RequestAttachments { get; set; } = new List<RequestAttachment>();

        #endregion

        #region Request - RequestProgress (One-to-One)

        public RequestProgress RequestProgress { get; set; } = default!;

        #endregion

        #region Request - AIEstimation (one-to-one)

        public AIEstimation? AIEstimation { get; set; } = default!;

        #endregion

        #region Request - Client (many-to-one)

        public string ClientId { get; set; } = default!;
        public ApplicationUser Client { get; set; } = default!;

        #endregion

        #region Request - Category (many-to-one)

        public string CateogryId { get; set; } = default!;
        public Category Category { get; set; } = default!;

        #endregion

        #region Proposal - Request (1-M)
        public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();

        #endregion

        #region SLAContract - Request (1-1)
        public SLAContract? SLAContract { get; set; } = default!;

        #endregion


        #endregion

    }
}
