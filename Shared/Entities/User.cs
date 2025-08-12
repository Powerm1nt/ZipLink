using Shared.Common;
using System.Text.Json.Serialization;

namespace Shared.Entities
{
    public class User : BaseEntity
    {
        public required string Username { get; set; }

        public required UserRole Role { get; set; }

        [JsonIgnore]
        public string HashedPassword { get; set; }

        public ICollection<Link> Links { get; set; }
        
        public ICollection<Media> MediaFiles { get; set; }
    }
}
