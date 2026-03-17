using CorpServe.Domain.Entities.RequestModule;
using EventHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.AIEstimateModule
{
    public class AIEstimation : BaseEntity<string>
    {
        public decimal EstimatedCost { get; set; }
        public DateTime EstimatedTime { get; set; }
        public int Confidence { get; set; }
        public DateTime CreatedAt { get; set; }
        #region RealtionShips

        #region Request - AIEstimation

        public string RequestId { get; set; } = default!;
        public Request Request { get; set; } = default!;

        #endregion

        #endregion
    }
}
