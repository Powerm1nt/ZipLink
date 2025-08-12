using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs
{
    public class MediaUpdateDto
    {
        public string? Description { get; set; }

        [Required(ErrorMessage = "ApiHostName is required.")]
        public required string ApiHostName { get; set; }
    }
}