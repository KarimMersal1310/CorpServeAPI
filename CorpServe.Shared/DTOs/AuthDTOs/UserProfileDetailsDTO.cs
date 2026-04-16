namespace CorpServe.Shared.DTOs.AuthDTOs
{
    public class UserProfileDetailsDTO
    {
        public string UserId { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string AccountStatus { get; set; } = default!;
        public bool IsOwner { get; set; }

        /// <summary>From <c>UserProfiles.CompanyName</c> only (not verification org name or other sources).</summary>
        public string CompanyName { get; set; } = string.Empty;
        /// <summary>From <c>UserProfiles.CompanyLocation</c> only.</summary>
        public string CompanyLocation { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? VendorStars { get; set; }

        /// <summary>Vendor: number of ratings received.</summary>
        public int? RatingCount { get; set; }

        /// <summary>Vendor total net earnings (paid). Only populated for the vendor viewing their own profile.</summary>
        public decimal? TotalEarnings { get; set; }

        /// <summary>Client average of (BudgetMin+BudgetMax)/2 across requests. Only populated for the client viewing their own profile.</summary>
        public decimal? AverageBudget { get; set; }

        public string? PhoneNumber { get; set; }
        public DateTime JoinedAt { get; set; }

        /// <summary>True when vendor verification is approved (for served categories UI).</summary>
        public bool IsVendorVerified { get; set; }

        public int TotalRequestsCount { get; set; }
        public int CompletedRequestsCount { get; set; }
        public int InProgressRequestsCount { get; set; }

        public int WorkingWithCount { get; set; }

        public ICollection<UserProfileRecentRequestDTO> RecentRequests { get; set; } = new List<UserProfileRecentRequestDTO>();
        public ICollection<UserProfileCategoryStatDTO> PreferredCategories { get; set; } = new List<UserProfileCategoryStatDTO>();
        public ICollection<string> ServedCategories { get; set; } = new List<string>();
        public ICollection<UserProfileDocumentDTO> Documents { get; set; } = new List<UserProfileDocumentDTO>();
    }

    public class UserProfileRecentRequestDTO
    {
        public string RequestId { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string RequestTitle { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = default!;
        public decimal? BudgetMin { get; set; }
        public decimal? BudgetMax { get; set; }
        public decimal? Price { get; set; }

        /// <summary>Counterparty display name (client when viewer is vendor; vendor when viewer is client).</summary>
        public string? ClientName { get; set; }
        public string? VendorName { get; set; }

        public int? Rating { get; set; }
        public string? RatingComment { get; set; }
    }

    public class UserProfileCategoryStatDTO
    {
        public string CategoryId { get; set; } = default!;
        public string CategoryName { get; set; } = default!;
        public int RequestsCount { get; set; }
    }

    public class UserProfileDocumentDTO
    {
        public string Name { get; set; } = default!;
        public string DocumentType { get; set; } = default!;
        public string DocumentUrl { get; set; } = default!;
    }
}
