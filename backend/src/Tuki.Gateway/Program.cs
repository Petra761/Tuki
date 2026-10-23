using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
            .AddServer("https://localhost:5000", "Gateway");
    });
}

app.MapGet("/", () => "Hello World!");

app.MapReverseProxy();

app.Run();
