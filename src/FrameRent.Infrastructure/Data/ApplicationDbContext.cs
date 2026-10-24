using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FrameRent.Domain.Entities;
using FrameRent.Application.Common.Interfaces;

namespace FrameRent.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Rental> Rentals => Set<Rental>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuración base obligatoria de ASP.NET Core Identity
        base.OnModelCreating(modelBuilder);

        // Configuración de la entidad Equipment
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Tipo de dato óptimo para dinero en PostgreSQL
            entity.Property(e => e.DailyRate).HasColumnType("numeric");
            
            // Relación de 1 a muchos restringida para proteger los datos de alquileres activos
            entity.HasMany(e => e.Rentals)
                  .WithOne(r => r.Equipment)
                  .HasForeignKey(r => r.EquipmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de la entidad Rental
        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ClientId).IsRequired();
            
            // Tipos de datos óptimos para dinero en PostgreSQL
            entity.Property(r => r.TotalCost).HasColumnType("numeric");
            entity.Property(r => r.OverduePenalty).HasColumnType("numeric");
            
            // Almacena el Enum como texto legible en la base de datos de Postgres
            entity.Property(r => r.Status)
                  .HasConversion<string>()
                  .IsRequired()
                  .HasMaxLength(20);
        });
    }
}
