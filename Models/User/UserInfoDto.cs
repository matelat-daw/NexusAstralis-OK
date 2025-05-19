using NexusAstralis.Models.Stars;
namespace NexusAstralis.Models.User
{
    public class UserInfoDto
    {
        public string? Nick { get; set; }
        public string? Name { get; set; }
        public string? Surname1 { get; set; }
        public string? Surname2 { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfileImage { get; set; }
        public DateOnly Bday { get; set; }
        public string? About { get; set; }
        public string? UserLocation { get; set; }
        public bool PublicProfile { get; set; }
        public virtual ICollection<Constellations> Constellations { get; set; } = new List<Constellations>();
        public virtual ICollection<Favorites> Favorites { get; set; } = new List<Favorites>();
    }
}