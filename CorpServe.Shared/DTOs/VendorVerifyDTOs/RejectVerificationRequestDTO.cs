using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.VendorVerify
{
    public class RejectVerificationRequestDTO
    {
        [Required(ErrorMessage = "Reject reason is required.")]
        [StringLength(500, ErrorMessage = "Reject reason cannot exceed 500 characters.")]
        public string RejectReason { get; set; } = default!;
    }
}
