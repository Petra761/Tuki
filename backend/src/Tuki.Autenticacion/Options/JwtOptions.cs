namespace Tuki.Autenticacion.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Tuki";
    public string Audience { get; set; } = "Tuki";

    // HS256 exige una clave de al menos 32 caracteres
    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}
