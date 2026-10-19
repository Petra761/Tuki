using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tuki.Cuentas.Data;

public class CuentasDbContextFactory : IDesignTimeDbContextFactory<CuentasDbContext>
{
    public CuentasDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CuentasDbContext>()
            .UseNpgsql("Host=localhost")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CuentasDbContext(options);
    }
}
