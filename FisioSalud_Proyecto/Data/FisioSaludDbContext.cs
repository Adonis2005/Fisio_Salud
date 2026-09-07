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
        }
    }
}
