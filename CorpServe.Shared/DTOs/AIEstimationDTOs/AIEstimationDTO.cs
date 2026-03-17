using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.AIEstimationDTOs
{
    public class AIEstimationDTO
    {
        public decimal EstimatedCost { get; set; }
        public DateTime EstimatedTime { get; set; }
        public int Confidence { get; set; }
    }
}
