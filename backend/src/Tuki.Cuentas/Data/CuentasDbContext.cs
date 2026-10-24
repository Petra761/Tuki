using Microsoft.EntityFrameworkCore;
using Tuki.Cuentas.Models;

namespace Tuki.Cuentas.Data;

public class CuentasDbContext(DbContextOptions<CuentasDbContext> options) : DbContext(options)
{
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();

    // HasData exige valores constantes: ids y fecha fijos, iguales en todos los tenants.
    private static readonly DateTimeOffset SeedDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid CashTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CashAccountId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountType>(e =>
        {
            // El id lo genera el cliente: EF no debe intentar generarlo.
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(
                new AccountType
                {
                    Id = CashTypeId,
                    Name = "Efectivo",
                    IsPerson = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new AccountType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = "Tarjeta bancaria",
                    IsPerson = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new AccountType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    Name = "Cuenta digital",
                    IsPerson = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new AccountType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                    Name = "Otro",
                    IsPerson = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new AccountType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                    Name = "Persona",
                    IsPerson = true,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                }
            );
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.CurrencyCode).HasMaxLength(3);

            e.HasIndex(x => x.IsArchived);
            e.HasIndex(x => x.AccountTypeId);

            // Índice único filtrado: solo puede haber una cuenta predeterminada.
            e.HasIndex(x => x.IsDefault).IsUnique().HasFilter("is_default = true");

            // Un tipo en uso no se borra: se archiva.
            e.HasOne(x => x.AccountType)
                .WithMany()
                .HasForeignKey(x => x.AccountTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasData(
                new Account
                {
                    Id = CashAccountId,
                    AccountTypeId = CashTypeId,
                    Name = "Efectivo",
                    CurrencyCode = "BOB",
                    IsDefault = true,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                }
            );
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);

            // Se guarda como texto ("Expense" o "Income") para que sea legible en la base.
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);

            e.HasIndex(x => x.ParentId);

            // Para listar rápido las categorías disponibles al registrar un movimiento.
            e.HasIndex(x => new { x.Kind, x.IsArchived });

            // Una categoría con subcategorías no se borra: se archiva.
            e.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasData(
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                    Name = "Alimentación",
                    Kind = CategoryKind.Expense,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                    Name = "Transporte",
                    Kind = CategoryKind.Expense,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000103"),
                    Name = "Educación",
                    Kind = CategoryKind.Expense,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000104"),
                    Name = "Salud",
                    Kind = CategoryKind.Expense,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000105"),
                    Name = "Hogar",
                    Kind = CategoryKind.Expense,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000106"),
                    Name = "Ocio",
                    Kind = CategoryKind.Expense,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000107"),
                    Name = "Otros gastos",
                    Kind = CategoryKind.Expense,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000108"),
                    Name = "Sueldo",
                    Kind = CategoryKind.Income,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                },
                new Category
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000109"),
                    Name = "Otros ingresos",
                    Kind = CategoryKind.Income,
                    IsArchived = false,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                }
            );
        });
    }
}
