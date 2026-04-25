using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class SuspendUserDTO
    {
        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = default!;
    }
}
