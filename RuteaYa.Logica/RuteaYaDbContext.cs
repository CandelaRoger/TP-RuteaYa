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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Email único 
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }


    }
}
