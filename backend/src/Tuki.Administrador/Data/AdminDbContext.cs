using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ProvisioningEvent> ProvisioningEvents => Set<ProvisioningEvent>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<ProvisioningEvent>(e =>
        {
            e.Property(x => x.Service).HasMaxLength(50);
            e.Property(x => x.DatabaseName).HasMaxLength(63);
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Error).HasMaxLength(2000);

            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Tenant)
                .WithMany(x => x.ProvisioningEvents)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Plan>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);

            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.TenantId);

            // Sin cascada: el historial de suscripciones no se pierde al tocar planes o tenants.
            // La columna user_id queda como identificador simple, sin clave
            // foránea: la tabla de usuarios vive en Autenticación
            e.HasOne(x => x.Plan)
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
