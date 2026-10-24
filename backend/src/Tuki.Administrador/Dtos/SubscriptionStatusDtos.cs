namespace Tuki.Administrador.Dtos;

public record SubscriptionStatusResponse(
    Guid UserId,
    string Result,
    string Reason,
    string? Plan,
    Guid TenantId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ExpiresAt,
    string? Status,
    // true = se muestran anuncios: solo una suscripción activa de un plan sin
    // anuncios lo apaga
    bool ShowAds,
    DateTimeOffset CheckedAt
);
