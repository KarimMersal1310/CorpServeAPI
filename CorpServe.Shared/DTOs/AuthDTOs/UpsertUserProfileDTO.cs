using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Shared.DTOs.AuthDTOs
{
    /// <summary>Updates fields on the <c>UserProfiles</c> row for the current user.</summary>
    public class UpsertUserProfileDTO
    {
        /// <summary>Persisted to <c>UserProfiles.CompanyName</c>.</summary>
        [FromForm(Name = "companyName")]
        public string? CompanyName { get; set; }

        [FromForm(Name = "companyLocation")]
        public string? CompanyLocation { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public string? Description { get; set; }
        public ICollection<IFormFile>? DocumentFiles { get; set; }
        public ICollection<UpsertProfileDocumentDTO>? Documents { get; set; }
    }

    public class UpsertProfileDocumentDTO
    {
        public string Name { get; set; } = default!;
        public string DocumentType { get; set; } = default!;
        public string DocumentUrl { get; set; } = default!;
    }
}
