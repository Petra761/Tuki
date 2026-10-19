using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tuki.Presupuestos.Data;

// Solo la usa "dotnet ef". Al generar migraciones no hay petición ni tenant, así que el
// registro de Program.cs lanzaría "No hay un tenant seleccionado". La conexión no se abre:
// la herramienta únicamente necesita saber el proveedor y la convención de nombres.
public class PresupuestosDbContextFactory : IDesignTimeDbContextFactory<PresupuestosDbContext>
{
    public PresupuestosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PresupuestosDbContext>()
            .UseNpgsql("Host=localhost")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PresupuestosDbContext(options);
    }
}
