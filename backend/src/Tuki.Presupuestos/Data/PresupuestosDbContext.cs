using Microsoft.EntityFrameworkCore;
using Tuki.Presupuestos.Models;

namespace Tuki.Presupuestos.Data;

public class PresupuestosDbContext(DbContextOptions<PresupuestosDbContext> options)
    : DbContext(options)
{
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Period).HasConversion<string>().HasMaxLength(20);

            // PostgreSQL permite varios nulos en un índice único, así que el límite del
            // presupuesto total (category_id nulo) se valida en la lógica del servicio
            e.HasIndex(x => new { x.CategoryId, x.Period }).IsUnique();
        });
    }
}
