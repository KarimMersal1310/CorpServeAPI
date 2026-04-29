using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.AuthDTOs
{
    /// <summary>Updates fields on the <c>UserProfiles</c> row for the current user.</summary>
    public class UpsertUserProfileDTO
    {
        [FromForm(Name = "companyName")]
        public string? CompanyName { get; set; }

        [FromForm(Name = "companyLocation")]
        public string? CompanyLocation { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePicture { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
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
