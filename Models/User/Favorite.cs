using System.ComponentModel.DataAnnotations;

namespace NexusAstralis.Models.User
{
    public class Favorite
    {
        [Key]
        public string UserId { get; set; } = null!;
        [Key]
        public int ConstellationId { get; set; } = 0;
    }
}