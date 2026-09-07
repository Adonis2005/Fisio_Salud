using System;
using System.Collections.Generic;

namespace FisioSalud_Proyecto.Models.Cliente
{
    public class CitaClienteViewModel
    {
        public int CitaId { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public int FisioterapeutaId { get; set; }
        public string Fisioterapeuta { get; set; }
        public string FisioterapeutaIniciales { get; set; }
        public string MotivoConsulta { get; set; }
        public string Observaciones { get; set; }
        public string Estado { get; set; }
        public int DuracionMinutos { get; set; }
    }

    public class CitasListViewModel
    {
        public List<CitaClienteViewModel> Citas { get; set; } = new System.Collections.Generic.List<CitaClienteViewModel>();
    }
}
