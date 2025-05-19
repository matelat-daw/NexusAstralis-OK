using System.ComponentModel.DataAnnotations;

namespace NexusAstralis.Models.User
{
    public class ResetPassword
    {
        [Required(ErrorMessage = "El Campo {0} es Obligatorio"), DataType(DataType.EmailAddress), Display(Name = "E-mail: ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "El Campo {0} es Obligatorio"), DataType(DataType.Password), Display(Name = "Nueva Contraseña: ")]
        public string? Password { get; set; }

        [DataType(DataType.Password), Compare("Password", ErrorMessage = "Las Contraseñas no Coinciden, Por Favor Escribelas de Nuevo."), Display(Name = "Confirmar Contraseña: ")]
        public string? Password2 { get; set; }

        public string? Token { get; set; }
    }
}