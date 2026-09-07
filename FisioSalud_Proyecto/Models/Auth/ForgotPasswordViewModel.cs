using System.ComponentModel.DataAnnotations;

namespace FisioSalud_Proyecto.Models.Auth
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; }
    }
}
