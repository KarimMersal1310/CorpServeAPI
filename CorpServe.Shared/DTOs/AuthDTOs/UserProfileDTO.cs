namespace CorpServe.Shared.DTOs.AuthDTOs
{
    public class UserProfileDTO
    {
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string Role { get; set; } = default!;

        /// <summary>From <c>UserProfiles.CompanyName</c> when present.</summary>
        public string CompanyName { get; set; } = string.Empty;
    }
}
