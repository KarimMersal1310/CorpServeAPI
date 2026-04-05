using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.AuthDTOs
{
    public class RefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; } = default!;
    }
}
