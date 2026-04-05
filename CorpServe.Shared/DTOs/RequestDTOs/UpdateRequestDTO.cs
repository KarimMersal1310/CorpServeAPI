using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class UpdateRequestDTO
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Description { get; set; } = default!;

        [Required]
        public string CategoryId { get; set; } = default!;

        [Required]
        public DateTime ExpectedDeadline { get; set; }

        [Required]
        public decimal BudgetMin { get; set; }

        [Required]
        public decimal BudgetMax { get; set; }

        public IFormFile[]? NewAttachments { get; set; }

        public string[]? AttachmentIdsToRemove { get; set; }
    }
}
