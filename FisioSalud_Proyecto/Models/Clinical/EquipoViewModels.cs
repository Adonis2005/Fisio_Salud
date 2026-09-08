using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FisioSalud_Proyecto.Models.Entities;

namespace FisioSalud_Proyecto.Models.Clinical
{
    public class EquiposPageViewModel
    {
        public List<EquipoTerapeutico> Equipos { get; set; } = new List<EquipoTerapeutico>();
        public List<UsoEquipo> Usos { get; set; } = new List<UsoEquipo>();
        public List<Paciente> Pacientes { get; set; } = new List<Paciente>();
        public List<Cita> Citas { get; set; } = new List<Cita>();
    }

    public class ProgramarUsoEquipoFormModel
    {
        [Range(1, int.MaxValue)] public int EquipoId { get; set; }
        [Range(1, int.MaxValue)] public int PacienteId { get; set; }
        public int? CitaId { get; set; }
        [Required, DataType(DataType.Date)] public DateTime Fecha { get; set; } = DateTime.Today;
        [Required] public TimeSpan HoraInicio { get; set; }
        [Required] public TimeSpan HoraFin { get; set; }
        [Range(0.1, 50)] public decimal? Velocidad { get; set; }
        [Range(0, 50)] public decimal? Inclinacion { get; set; }
        [MaxLength(1000)] public string Indicaciones { get; set; }
    }
}
