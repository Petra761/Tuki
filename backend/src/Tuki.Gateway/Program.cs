using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// El CORS se resuelve en el Gateway porque es el único punto de entrada: los
// microservicios no necesitan repetir esta lógica. Es obligatorio porque todos
// exigen el header X-Tenant-Id, que el navegador trata como "no simple" y por
// eso dispara un preflight OPTIONS antes de cualquier petición real.
var origenesPermitidos =
    builder.Configuration.GetSection("Tuki:Cors:Origenes").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy(
        "tuki",
        politica =>
            politica
                .WithOrigins(origenesPermitidos)
                // X-Tenant-Id es la cabecera con la que cada cliente identifica su base
                .WithHeaders("Content-Type", "X-Tenant-Id")
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                // Necesario para que el probador vea este header al validar el aislamiento
                .WithExposedHeaders("X-Tuki-Gateway")
    )
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .AddDocument("administrador", "Administrador", "/openapi/administrador.json")
            .AddDocument("cuentas", "Cuentas", "/openapi/cuentas.json")
            .AddDocument("movimientos", "Movimientos", "/openapi/movimientos.json")
            .AddDocument("presupuestos", "Presupuestos", "/openapi/presupuestos.json")
            // Toma la URL base de la petición actual, así funciona igual en localhost y en el devtunnel
            .WithDynamicBaseServerUrl();
    });
}

app.MapGet("/", () => "Hello World!");

// Antes del proxy para que también cubra las respuestas enrutadas
app.UseCors("tuki");

app.MapReverseProxy();

app.Run();
