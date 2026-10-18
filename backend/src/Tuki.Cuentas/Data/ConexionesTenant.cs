using System.Text.RegularExpressions;
using Npgsql;

namespace Tuki.Cuentas.Data;

public sealed class ConexionesTenant
{
    private static readonly Regex NombreSeguro = new("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

    private readonly string _proyecto;
    private readonly string _servicio;
    private readonly NpgsqlConnectionStringBuilder _servidor;

    public ConexionesTenant(IConfiguration configuracion, string servicio)
    {
        _proyecto = Obligatorio(configuracion["Tuki:Proyecto"], "Tuki__Proyecto");
        _servicio = servicio;

        if (!NombreSeguro.IsMatch(_proyecto) || !NombreSeguro.IsMatch(_servicio))
            throw new InvalidOperationException(
                "El proyecto y el servicio solo pueden tener minúsculas, números y guion bajo, y empezar con una letra."
            );

        _servidor = new NpgsqlConnectionStringBuilder
        {
            Host = Obligatorio(configuracion["Tuki:Postgres:Host"], "Tuki__Postgres__Host"),
            Port = int.Parse(
                Obligatorio(configuracion["Tuki:Postgres:Port"], "Tuki__Postgres__Port")
            ),
            Username = Obligatorio(
                configuracion["Tuki:Postgres:Usuario"],
                "Tuki__Postgres__Usuario"
            ),
            Password = Obligatorio(
                configuracion["Tuki:Postgres:Password"],
                "Tuki__Postgres__Password"
            ),
            Database = "postgres",
        };
    }

    public string NombreBase(Guid tenantId) => $"{_proyecto}_{_servicio}_{tenantId:N}";

    public string CadenaConexion(Guid tenantId) =>
        new NpgsqlConnectionStringBuilder(_servidor.ConnectionString)
        {
            Database = NombreBase(tenantId),
        }.ConnectionString;

    public string CadenaConexionServidor() => _servidor.ConnectionString;

    private static string Obligatorio(string? valor, string variable) =>
        string.IsNullOrWhiteSpace(valor)
            ? throw new InvalidOperationException(
                $"Falta {variable}: copia .env.example como .env y completa tus valores."
            )
            : valor;
}
