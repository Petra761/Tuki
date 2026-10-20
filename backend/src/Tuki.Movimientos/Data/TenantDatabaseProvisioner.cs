using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Tuki.Movimientos.Data;

public record ProvisionOutcome(string Database, string Result, int MigrationsApplied);

public class TenantDatabaseProvisioner(ConexionesTenant conexiones)
{
    // Evita que dos llamadas simultáneas intenten crear la misma base a la vez
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task<ProvisionOutcome> PrepareAsync(Guid tenantId, CancellationToken ct)
    {
        var database = conexiones.NombreBase(tenantId);

        await Lock.WaitAsync(ct);
        try
        {
            var existed = await DatabaseExistsAsync(database, ct);

            var options = new DbContextOptionsBuilder<MovimientosDbContext>()
                .UseNpgsql(conexiones.CadenaConexion(tenantId))
                .UseSnakeCaseNamingConvention()
                .Options;

            await using var context = new MovimientosDbContext(options);
            var pending = (await context.Database.GetPendingMigrationsAsync(ct)).Count();

            // Crea la base si no existe y aplica las migraciones pendientes
            await context.Database.MigrateAsync(ct);

            return new ProvisionOutcome(database, existed ? "AlreadyExisted" : "Created", pending);
        }
        finally
        {
            Lock.Release();
        }
    }

    private async Task<bool> DatabaseExistsAsync(string database, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(conexiones.CadenaConexionServidor());
        await connection.OpenAsync(ct);

        await using var command = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name",
            connection
        );
        command.Parameters.AddWithValue("name", database);

        return await command.ExecuteScalarAsync(ct) is not null;
    }
}
