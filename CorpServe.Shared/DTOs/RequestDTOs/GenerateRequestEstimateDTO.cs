using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class GenerateRequestEstimateDTO
    {
        [StringLength(10)]
        public string? RequestId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = default!;

        [Required]
        public string CategoryId { get; set; } = default!;

        public DateTime ExpectedDeadline { get; set; }
        public decimal BudgetMin { get; set; }
        public decimal BudgetMax { get; set; }
    }
}
