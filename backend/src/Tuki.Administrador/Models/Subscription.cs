namespace Tuki.Administrador.Models;

public class Subscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // Fechas en UTC: vigente desde StartedAt hasta antes de ExpiresAt; nulo indica sin vencimiento
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public SubscriptionStatus Status { get; set; }
}
