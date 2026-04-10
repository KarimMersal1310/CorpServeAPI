using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CorpServe.Shared.DTOs.ChatDTOs
{
    public class SendAttachmentDTO
    {
        [Required(ErrorMessage = "File is required.")]
        public IFormFile File { get; set; } = default!;
        public string? Content { get; set; }
    }
}
