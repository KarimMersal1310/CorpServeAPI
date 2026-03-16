using EventHub.Domain.Entities;
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
        public RequestStatus RequestStatus { get; set; } = default!;

        #region RelationShips

        #region Request - RequestAttachment (One-to-Many)

        public ICollection<RequestAttachment> RequestAttachments { get; set; } = new List<RequestAttachment>();

        #endregion
        #region Request - RequestProgress (One-to-One)

        public RequestProgress RequestProgress { get; set; } = default!;

        #endregion

        #endregion

    }
}
