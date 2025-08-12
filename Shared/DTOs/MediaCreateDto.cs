using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs
{
    public class MediaCreateDto
    {
        [Required(ErrorMessage = "UserId cannot be null.")]
        public required string UserId { get; set; }

        [Required(ErrorMessage = "FileName cannot be null.")]
        public required string FileName { get; set; }

        [Required(ErrorMessage = "OriginalFileName cannot be null.")]
        public required string OriginalFileName { get; set; }

        [Required(ErrorMessage = "ContentType cannot be null.")]
        public required string ContentType { get; set; }

        [Required(ErrorMessage = "FileSize is required.")]
        public required long FileSize { get; set; }

        [Required(ErrorMessage = "FilePath cannot be null.")]
        public required string FilePath { get; set; }

        [Required(ErrorMessage = "ApiHostName is required.")]
        public required string ApiHostName { get; set; }

        public string? Description { get; set; }
    }
}