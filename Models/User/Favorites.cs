using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NexusAstralis.Models.User
{
    public class Favorites
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int ConstellationId { get; set; }
        [JsonIgnore]
        public NexusUser? User { get; set; }
    }
}