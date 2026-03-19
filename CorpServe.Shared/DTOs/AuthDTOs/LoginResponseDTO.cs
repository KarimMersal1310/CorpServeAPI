namespace CorpServe.Shared.DTOs.AuthDTOs
{
    public class LoginResponseDTO
    {
        public string FullName { get; set; } = default!;
        public string Token { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
