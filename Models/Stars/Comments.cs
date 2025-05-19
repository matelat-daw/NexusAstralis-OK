using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NexusAstralis.Models.Stars
{
    public partial class Comments
    {
        [Key]
        public int Id { get; set; }
        public string? UserNick { get; set; }
        public int ConstellationId { get; set; }

        public string? Comment { get; set; }
        // Propiedad de navegación
        [ForeignKey(nameof(ConstellationId))]
        [JsonIgnore]
        public Constellations? Constellation { get; set; }
    }
}