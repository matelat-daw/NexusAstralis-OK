using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NexusAstralis.Models.User
{
    public class NexusUser : IdentityUser
    {
        [Required(ErrorMessage = "El Campo {0} es Obligatorio"), StringLength(20, MinimumLength = 3), Display(Name = "Nick: ")]
        public string? Nick { get; set; }
        [Required(ErrorMessage = "El Campo {0} es Obligatorio"), StringLength(24, MinimumLength = 3), Display(Name = "Nombre: ")]
        public string? Name { get; set; }
        [Required(ErrorMessage = "El Campo {0} es Obligatorio"), StringLength(24, MinimumLength = 3), Display(Name = "Apellido 1: ")]
        public string? Surname1 { get; set; }
        [StringLength(24, MinimumLength = 3), Display(Name = "Apellido 2: ")]
        public string? Surname2 { get; set; }
        [Display(Name = "Fecha de Nacimiento: ")]
        public DateOnly Bday { get; set; }
        [Display(Name = "Foto de Perfil: ")]
        public string? ProfileImage { get; set; }
        [Display(Name = "Sobre Mí: ")]
        public string? About { get; set; }
        [Display(Name = "Localización: ")]
        public string? UserLocation { get; set; }
        [Display(Name = "Perfil Público?: ")]
        public bool PublicProfile { get; set; } = false;

        public virtual ICollection<Favorites> Favorites { get; set; } = new List<Favorites>(); // Para la Relación con la Tabla de Favoritos.
        public virtual ICollection<Comments> Comments { get; set; } = new List<Comments>();
    }
}