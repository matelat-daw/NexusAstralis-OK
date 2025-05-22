using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NexusAstralis.Models.User
{
    public partial class Comments
    {
        [Key]
        public int Id { get; set; }
        public string? UserNick { get; set; }
        public string? ConstellationName { get; set; }
        public string? Comment { get; set; }
        public string? UserId { get; set; }
        public int ConstellationId { get; set; }
        [JsonIgnore]
        public NexusUser? User { get; set; }
    }
}