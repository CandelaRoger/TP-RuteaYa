using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;

namespace RuteaYa.Logica
{
    public class RuteaYaDbContext : DbContext
    {
        public RuteaYaDbContext(DbContextOptions<RuteaYaDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Viaje> Viajes { get; set; } = null!;
        public DbSet<Parada> Paradas { get; set; } = null!;
        public DbSet<ViajeDia> ViajeDias { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ViajeDia>()
                .HasKey(vd => new { vd.ViajeId, vd.DiaSemana });

            modelBuilder.Entity<Viaje>()
                .HasOne(v => v.Usuario)
                .WithMany(u => u.Viajes)
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Parada>()
                .HasOne(p => p.Viaje)
                .WithMany(v => v.Paradas)
                .HasForeignKey(p => p.ViajeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ViajeDia>()
                .HasOne(vd => vd.Viaje)
                .WithMany(v => v.DiasSemana)
                .HasForeignKey(vd => vd.ViajeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}