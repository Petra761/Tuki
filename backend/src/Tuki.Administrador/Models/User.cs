namespace Tuki.Administrador.Models;

public class User
{
    public Guid Id { get; set; }

    // Cada usuario pertenece a un único tenant
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Vacío si la persona solo entra con Google
    public string? PasswordHash { get; set; }

    public bool EmailVerified { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
