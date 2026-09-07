using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("PasswordResetTokens")]
    public class PasswordResetToken
    {
        [Key]
        public int TokenId { get; set; }

        public int UsuarioId { get; set; }

        [Required, MaxLength(255)]
        public string Token { get; set; }

        public DateTime FechaExpiracion { get; set; }

        public bool Usado { get; set; }

        public DateTime FechaCreacion { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; }
    }
}
