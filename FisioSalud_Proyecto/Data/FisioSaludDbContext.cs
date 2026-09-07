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
        }
    }
}
