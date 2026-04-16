using CorpServe.Shared.DTOs.AIEstimationDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class RequestDTO
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string CategoryId { get; set; } = default!;
        public string CategoryName { get; set; } = default!;
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
        public DateTime ExpectedDeadline { get; set; }
        public string CreatedAt { get; set; } = default!;
        public int ProgressPercentage { get; set; }
        public string RequestStatus { get; set; } = default!;
        /// <summary>Optional vendor assigned via SLA or progress (for client views).</summary>
        public string? AssignedVendorId { get; set; }
        public string? AssignedVendorName { get; set; }
        public string? VendorProfilePictureUrl { get; set; }
        public AIEstimationDTO? AIEstimation { get; set; }
        public ICollection<RequestAttachmentDTO> RequestAttachments { get; set; } = new List<RequestAttachmentDTO>();
    }
}
