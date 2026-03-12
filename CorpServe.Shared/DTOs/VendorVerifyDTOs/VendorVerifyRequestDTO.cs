using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.VendorVerify
{
    public class VendorVerifyRequestDTO
    {
        [Required(ErrorMessage = "Organization Name is required.")]
        [StringLength(200, ErrorMessage = "Organization Name cannot exceed 200 characters.")]
        public string OrganizationName { get; set; } = default!;

        [Required(ErrorMessage = "At least one document is required.")]
        public IFormFile[] Documents { get; set; } = default!;
    }
}
