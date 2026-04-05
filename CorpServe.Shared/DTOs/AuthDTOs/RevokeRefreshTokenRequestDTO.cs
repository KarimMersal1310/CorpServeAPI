using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.AuthDTOs
{
    public class RevokeRefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; } = default!;
    }
}
