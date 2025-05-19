using System.ComponentModel.DataAnnotations;

namespace NexusAstralis.Models.User
{
    public class Login
    {
        [Required(ErrorMessage = "El Campo {0} es Obligatorio"), DataType(DataType.EmailAddress), Display(Name = "E-mail: ")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "El Campo {0} es Obligatorio"), DataType(DataType.Password), Display(Name = "Contraseña: ")]
        public string? Password { get; set; }
        [Display(Name = "Recuérdame!: ")]
        public bool RememberMe { get; set; }
    }
}