using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FisioSalud_Proyecto.Models.Entities
{
    [Table("DetallesFactura")]
    public class DetalleFactura
    {
        [Key]
        public int DetalleFacturaId { get; set; }

        public int FacturaId { get; set; }

        public int? CitaId { get; set; }

        [Required, MaxLength(250)]
        public string Concepto { get; set; }

        public int Cantidad { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        public DateTime FechaRegistro { get; set; }

        [ForeignKey(nameof(FacturaId))]
        public Factura Factura { get; set; }

        [ForeignKey(nameof(CitaId))]
        public Cita Cita { get; set; }
    }
}
