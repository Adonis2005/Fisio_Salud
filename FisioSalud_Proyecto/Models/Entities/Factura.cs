using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("Facturas")]
    public class Factura
    {
        [Key]
        public int FacturaId { get; set; }

        [Required, MaxLength(50)]
        public string NumeroFactura { get; set; }

        public int PacienteId { get; set; }

        public int? FisioterapeutaId { get; set; }

        public int? UsuarioId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "PENDIENTE"; // PAGADA, PENDIENTE, VENCIDA

        public DateTime FechaRegistro { get; set; }

        [ForeignKey(nameof(PacienteId))]
        public Paciente Paciente { get; set; }

        [ForeignKey(nameof(FisioterapeutaId))]
        public Usuario Fisioterapeuta { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; }
        public ICollection<DetalleFactura> DetallesFactura { get; set; } = new List<DetalleFactura>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
