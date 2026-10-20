using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tuki.Administrador.Data;
using Tuki.Administrador.Services;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

string Required(string key) =>
    builder.Configuration[key]
    ?? throw new InvalidOperationException(
        $"Falta {key.Replace(":", "__")}: revisa tu archivo .env."
    );

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddDbContext<AdminDbContext>(options =>
{
    var connection = new NpgsqlConnectionStringBuilder
    {
        Host = Required("Tuki:Postgres:Host"),
        Port = int.Parse(Required("Tuki:Postgres:Port")),
        Username = Required("Tuki:Postgres:Usuario"),
        Password = Required("Tuki:Postgres:Password"),
        Database = Required("Tuki:BaseAdministrador"),
    };

    options.UseNpgsql(connection.ConnectionString).UseSnakeCaseNamingConvention();
});

var app = builder.Build();

// Crea tuki_admin si no existe y aplica sus migraciones
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AdminDbContext>().Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
