using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FisioSalud_Proyecto.Models.Administrador
{
    public class UsuarioListViewModel
    {
        public int UsuarioId { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Identificacion { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Rol { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }

    public class UsuarioFormViewModel
    {
        public int? UsuarioId { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [MaxLength(80)]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [MaxLength(80)]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [MaxLength(20)]
        [Display(Name = "Identificación")]
        public string Identificacion { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [MaxLength(120)]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; }

        [MaxLength(20)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "Seleccione un rol.")]
        [Display(Name = "Rol")]
        public int RolId { get; set; }

        [Display(Name = "Estado activo")]
        public bool Estado { get; set; } = true;

        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe incluir mayúsculas, minúsculas y números.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; }

        public List<RolSelectItem> RolesDisponibles { get; set; } = new List<RolSelectItem>();
    }

    public class RolSelectItem
    {
        public int RolId { get; set; }
        public string Nombre { get; set; }
    }

    public class UsuarioFilterViewModel
    {
        public string Busqueda { get; set; }
        public int? RolId { get; set; }
        public bool? Estado { get; set; }
        public List<UsuarioListViewModel> Usuarios { get; set; } = new List<UsuarioListViewModel>();
        public List<RolSelectItem> RolesDisponibles { get; set; } = new List<RolSelectItem>();
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosBloqueados { get; set; }
        public int TotalClientes { get; set; }
        public int TotalFisioterapeutas { get; set; }
        public List<UsuarioListViewModel> ActividadReciente { get; set; } = new List<UsuarioListViewModel>();
    }
}
