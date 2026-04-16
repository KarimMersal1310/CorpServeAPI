namespace CorpServe.Shared.DTOs.AdminDTOs
{
    public class AdminUserManagementDTO
    {
        public string UserId { get; set; } = default!;
        public string? ProfilePictureUrl { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Joined { get; set; }
        public int RequestsCreatedCount { get; set; }
        public int RequestsHandledCount { get; set; }
    }
}
