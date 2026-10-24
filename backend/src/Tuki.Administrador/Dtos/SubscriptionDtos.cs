using Tuki.Administrador.Models;

namespace Tuki.Administrador.Dtos;

public class SubscribeRequest
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }

    // Opcional: si no se envía, la suscripción empieza ahora
    public DateTimeOffset? StartedAt { get; set; }
}

public record SubscriptionResponse(
    Guid Id,
    Guid UserId,
    Guid PlanId,
    string PlanName,
    Guid TenantId,
    DateTimeOffset StartedAt,
    DateTimeOffset? ExpiresAt,
    string Status
)
{
    public static SubscriptionResponse From(Subscription s, string planName) =>
        new(
            s.Id,
            s.UserId,
            s.PlanId,
            planName,
            s.TenantId,
            s.StartedAt,
            s.ExpiresAt,
            s.Status.ToString()
        );
}
