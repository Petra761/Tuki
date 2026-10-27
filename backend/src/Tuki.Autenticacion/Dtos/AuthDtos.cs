namespace Tuki.Autenticacion.Dtos;

public record UserSummary(
    Guid Id,
    Guid TenantId,
    string Email,
    string DisplayName,
    bool EmailVerified
);

public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserSummary User);
