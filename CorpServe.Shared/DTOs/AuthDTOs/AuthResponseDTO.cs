using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.DTOs.AuthDTOs
{
    public class AuthResponseDTO
    {
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
        public DateTime AccessTokenExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; } = default!;
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
        public string Role { get; set; } = default!;
    }
}
