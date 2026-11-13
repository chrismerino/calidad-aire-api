using CalidadAire.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CalidadAire.Api.Data;

public class AireDbContext(DbContextOptions<AireDbContext> options) : DbContext(options)
{
    public DbSet<SensorCalidadAire> Sensores => Set<SensorCalidadAire>();
    public DbSet<LecturaAire> Lecturas => Set<LecturaAire>();
    public DbSet<AlertaAire> Alertas => Set<AlertaAire>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorCalidadAire>(entity =>
        {
            entity.ToTable("Sensores");
            entity.Property(s => s.Ubicacion).HasMaxLength(150).IsRequired();
            entity.Property(s => s.TipoGas).HasMaxLength(50).IsRequired();
            entity.HasMany(s => s.Lecturas).WithOne(l => l.Sensor)
                .HasForeignKey(l => l.SensorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Alertas).WithOne(a => a.Sensor)
                .HasForeignKey(a => a.SensorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LecturaAire>(entity =>
        {
            entity.ToTable("Lecturas");
            entity.Property(l => l.PM2_5).HasPrecision(12, 4);
            entity.Property(l => l.PM10).HasPrecision(12, 4);
            entity.Property(l => l.CO2).HasPrecision(12, 4);
            entity.Property(l => l.FechaHora).HasColumnType("datetime(6)");
            entity.HasIndex(l => new { l.SensorId, l.FechaHora });
            entity.HasIndex(l => l.FechaHora);
        });

        modelBuilder.Entity<AlertaAire>(entity =>
        {
            entity.ToTable("Alertas");
            entity.Property(a => a.Nivel).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Mensaje).HasMaxLength(300).IsRequired();
            entity.Property(a => a.FechaHora).HasColumnType("datetime(6)");
            entity.HasIndex(a => new { a.SensorId, a.FechaHora });
        });
    }
}
