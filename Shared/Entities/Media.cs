using Shared.Common;

namespace Shared.Entities
{
    public class Media : BaseEntity
    {
        public required string UserId { get; set; }
        
        public required string FileName { get; set; }
        
        public required string OriginalFileName { get; set; }
        
        public required string ContentType { get; set; }
        
        public required long FileSize { get; set; }
        
        public required string FilePath { get; set; }
        
        public required string ApiHostName { get; set; }
        
        public string? Description { get; set; }

        public User User { get; set; }
    }
}