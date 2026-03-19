using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class CreateRequestDTO
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = default!;

        [Required]
        public string CategoryId { get; set; } = default!;

        [Required]
        public DateTime ExpectedDeadline { get; set; }

        [Required]
        public decimal BudgetMin { get; set; }

        [Required]
        public decimal BudgetMax { get; set; }

        public decimal? EstimatedCost { get; set; }

        public DateTime? EstimatedTime { get; set; }

        [Range(0, 100)]
        public int? Confidence { get; set; }

        public IFormFile[]? Attachments { get; set; }

    }
}
