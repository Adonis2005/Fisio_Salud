using System;
using System.ComponentModel.DataAnnotations;

namespace FisioSalud_Proyecto.Models.Contacto
{
    public class ReservarCitaViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [Display(Name = "Apellido")]
        [MaxLength(100)]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "Seleccione un servicio")]
        [Display(Name = "Servicio de interés")]
        public string Servicio { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha preferida")]
        public DateTime? Fecha { get; set; }

        [Required(ErrorMessage = "El horario es obligatorio")]
        [Display(Name = "Horario preferido")]
        public string Hora { get; set; }

        [Display(Name = "Mensaje adicional")]
        [MaxLength(1000)]
        public string Mensaje { get; set; }

        public static readonly string[] ServiciosDisponibles =
        {
            "Fisioterapia Manual",
            "Electroterapia",
            "Rehabilitación Deportiva",
            "Masajes Terapéuticos",
            "Pilates Terapéutico",
            "Neurorrehabilitación"
        };
    }
}
