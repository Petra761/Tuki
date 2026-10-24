using Microsoft.EntityFrameworkCore;
using Tuki.Movimientos.Models;

namespace Tuki.Movimientos.Data;

public class MovimientosDbContext(DbContextOptions<MovimientosDbContext> options)
    : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionLine> TransactionLines => Set<TransactionLine>();
    public DbSet<Shortcut> Shortcuts => Set<Shortcut>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(200);

            e.HasIndex(x => x.OccurredOn);
            e.HasIndex(x => x.Description);
        });

        modelBuilder.Entity<TransactionLine>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();

            e.HasIndex(x => x.TransactionId);
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.CategoryId);

            // Al eliminar una transacción se eliminan sus líneas
            e.HasOne(x => x.Transaction)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // AccountId y CategoryId no tienen clave foránea: esas tablas están en la base de Cuentas
        });

        modelBuilder.Entity<Shortcut>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Tag>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TransactionTag>(e =>
        {
            // La clave compuesta impide asignar dos veces el mismo marcador a una transacción
            e.HasKey(x => new { x.TransactionId, x.TagId });
            e.HasIndex(x => x.TagId);

            e.HasOne(x => x.Transaction)
                .WithMany(x => x.TransactionTags)
                .HasForeignKey(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Un marcador en uso no se borra: se archiva
            e.HasOne(x => x.Tag)
                .WithMany()
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
