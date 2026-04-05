using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.RequestDTOs
{
    public class UpdateRequestProgressDTO
    {
        [Range(0, 100)]
        public int Percentage { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = default!;
    }
}
