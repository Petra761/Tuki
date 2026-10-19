using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tuki.Movimientos.Data;

// Solo la usa "dotnet ef". Al generar migraciones no hay petición ni tenant, así que el
// registro de Program.cs lanzaría "No hay un tenant seleccionado". La conexión no se abre:
// la herramienta únicamente necesita saber el proveedor y la convención de nombres.
public class MovimientosDbContextFactory : IDesignTimeDbContextFactory<MovimientosDbContext>
{
    public MovimientosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MovimientosDbContext>()
            .UseNpgsql("Host=localhost")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new MovimientosDbContext(options);
    }
}
