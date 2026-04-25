using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class RejectProposalDTO
    {
        [Required]
        public string RequestId { get; set; } = default!;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = default!;
    }
}
