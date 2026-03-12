using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.AuthDTOs
{
    public class RegisterDTO
    {
        [Required]
        public string FullName { get; set; } = default!;
        [Required]
        public string Email { get; set; } = default!;
        [Required]
        public string Phone { get; set; } = default!;
        [Required]
        public string Password { get; set; } = default!;
        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = default!;
        [Required]
        public string Role { get; set; } = default!; // Client or Vendor

        // Required only when Role is Vendor.
        public ICollection<string>? CategoryIds { get; set; }
    }
}
