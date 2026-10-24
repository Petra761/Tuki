using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
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

        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.Property(x => x.PasswordHash).HasMaxLength(500);

            e.HasIndex(x => x.Email).IsUnique();

            // Un usuario, un tenant
            e.HasIndex(x => x.TenantId).IsUnique();
            e.HasOne(x => x.Tenant)
                .WithOne(x => x.User)
                .HasForeignKey<User>(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
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

            // Sin cascada: el historial de suscripciones no se pierde al tocar usuarios o planes
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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
