using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class GenerateRequestEstimateDTO
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Description { get; set; } = default!;

        [Required]
        public string CategoryId { get; set; } = default!;

        public DateTime ExpectedDeadline { get; set; }
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
    }
}
