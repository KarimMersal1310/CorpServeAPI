using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.ProposalDTOs
{
    public class ClientRejectProposalDTO
    {
        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = default!;
    }
}
