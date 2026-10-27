namespace Tuki.Autenticacion.Models;

// Las credenciales viven aquí desde que la autenticación se separó del
// Administrador. El tenant viaja como un identificador simple: entre servicios
// solo viajan ids, nunca se navega a la tabla de tenants de otro servicio.
public class User
{
    public Guid Id { get; set; }

    // Tenant al que pertenece el usuario; aquí no hay clave foránea ni
    // navegación porque la tabla de tenants vive en el Administrador
    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Vacío si la persona solo entra con Google
    public string? PasswordHash { get; set; }

    public bool EmailVerified { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
