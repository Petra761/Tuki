using Microsoft.EntityFrameworkCore;
using Tuki.Autenticacion.Models;

namespace Tuki.Autenticacion.Data;

// Base global del servicio (tuki_autenticacion): guarda las credenciales de
// todos los usuarios, no está particionada por tenant como las de negocio.
// Los nombres de tabla y columnas son idénticos a los del Administrador para
// que la migración de datos existentes pueda copiar la tabla tal cual.
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.Property(x => x.PasswordHash).HasMaxLength(500);

            e.HasIndex(x => x.Email).IsUnique();

            // Un usuario, un tenant: la restricción se conserva aunque aquí no
            // exista la clave foránea, para que los datos copiados mantengan la
            // misma regla que tenían en tuki_admin
            e.HasIndex(x => x.TenantId).IsUnique();
        });
    }
}
