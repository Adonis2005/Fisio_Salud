using FisioSalud_Proyecto.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FisioSalud_Proyecto.Data
{
    public class FisioSaludDbContext : DbContext
    {
        public FisioSaludDbContext(DbContextOptions<FisioSaludDbContext> options)
            : base(options)
        {
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetallesFactura { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Patologia> Patologias { get; set; }
        public DbSet<Diagnostico> Diagnosticos { get; set; }
        public DbSet<PlanTratamiento> PlanesTratamiento { get; set; }
        public DbSet<Ejercicio> Ejercicios { get; set; }
        public DbSet<TratamientoEjercicio> TratamientoEjercicios { get; set; }
        public DbSet<SesionRehabilitacion> SesionesRehabilitacion { get; set; }
        public DbSet<Seguimiento> Seguimientos { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<EvaluacionInicial> EvaluacionesIniciales { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<TratamientoAplicado> TratamientosAplicados { get; set; }
        public DbSet<SesionTratamiento> SesionTratamientos { get; set; }
        public DbSet<EjercicioRealizado> EjerciciosRealizados { get; set; }
        public DbSet<DisponibilidadFisioterapeuta> DisponibilidadesFisioterapeuta { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            });

            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("SYSDATETIME()");
            });

            modelBuilder.Entity<Cita>(entity =>
            {
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("SYSDATETIME()");
                entity.HasOne(e => e.Servicio).WithMany().HasForeignKey(e => e.ServicioId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Diagnostico>(entity =>
            {
                entity.Property(e => e.DiagnosticoTexto).HasColumnName("Diagnostico");
                entity.Property(e => e.FechaDiagnostico).HasColumnType("date");
                entity.HasOne(e => e.Paciente).WithMany().HasForeignKey(e => e.PacienteId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Fisioterapeuta).WithMany().HasForeignKey(e => e.FisioterapeutaId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PlanTratamiento>(entity =>
            {
                entity.Property(e => e.FechaInicio).HasColumnType("date");
                entity.HasOne(e => e.Diagnostico).WithMany(d => d.PlanesTratamiento).HasForeignKey(e => e.DiagnosticoId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.FechaFinEstimada).HasColumnType("date");
                entity.HasOne(e => e.Paciente).WithMany().HasForeignKey(e => e.PacienteId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Fisioterapeuta).WithMany().HasForeignKey(e => e.FisioterapeutaId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SesionRehabilitacion>(entity =>
            {
                entity.Property(e => e.FechaSesion).HasColumnType("date");
                entity.HasOne(e => e.Fisioterapeuta).WithMany().HasForeignKey(e => e.FisioterapeutaId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Seguimiento>(entity =>
            {
                entity.Property(e => e.NivelDolor).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MovilidadArticular).HasColumnType("decimal(18,2)");
                entity.Property(e => e.FuerzaMuscular).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GradoRecuperacion).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Mensaje>(entity =>
            {
                entity.HasOne(e => e.Remitente).WithMany().HasForeignKey(e => e.RemitenteId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Destinatario).WithMany().HasForeignKey(e => e.DestinatarioId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.FechaEnvio).HasDefaultValueSql("SYSDATETIME()");
            });

            modelBuilder.Entity<DetalleFactura>(entity =>
            {
                entity.HasOne(e => e.Factura).WithMany(f => f.DetallesFactura).HasForeignKey(e => e.FacturaId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Cita).WithMany().HasForeignKey(e => e.CitaId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Servicio).WithMany(s => s.DetallesFactura).HasForeignKey(e => e.ServicioId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Factura>(entity =>
            {
                entity.HasOne(e => e.Paciente).WithMany().HasForeignKey(e => e.PacienteId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(e => e.Pagos).WithOne(p => p.Factura).HasForeignKey(p => p.FacturaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Pago>(entity => entity.Property(e => e.Monto).HasColumnType("decimal(18,2)"));
            modelBuilder.Entity<EvaluacionInicial>(entity =>
            {
                entity.Property(e => e.DolorInicial).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Paciente).WithMany().HasForeignKey(e => e.PacienteId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Fisioterapeuta).WithMany().HasForeignKey(e => e.FisioterapeutaId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Cita).WithMany().HasForeignKey(e => e.CitaId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SesionTratamiento>(entity =>
            {
                entity.HasOne(e => e.Sesion).WithMany().HasForeignKey(e => e.SesionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.TratamientoAplicado).WithMany(t => t.Sesiones).HasForeignKey(e => e.TratamientoAplicadoId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EjercicioRealizado>(entity =>
            {
                entity.Property(e => e.Dolor).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.TratamientoEjercicio).WithMany().HasForeignKey(e => e.TratamientoEjercicioId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Sesion).WithMany().HasForeignKey(e => e.SesionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Paciente).WithMany().HasForeignKey(e => e.PacienteId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DisponibilidadFisioterapeuta>(entity =>
                entity.HasOne(e => e.Fisioterapeuta).WithMany().HasForeignKey(e => e.FisioterapeutaId).OnDelete(DeleteBehavior.Restrict));
        }
    }
}
