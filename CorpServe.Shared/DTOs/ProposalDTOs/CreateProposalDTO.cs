using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class CreateProposalDTO
    {
        [Required]
        public string RequestId { get; set; } = default!;

        [Required]
        public decimal ProposedPrice { get; set; }

        [Required]
        public DateTime ProposedDeadline { get; set; }

        [StringLength(1000)]
        public string? Message { get; set; }
    }
}
