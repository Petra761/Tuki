using System.ComponentModel.DataAnnotations;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Dtos;

public class PlanRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // El precio en centavos puede ser 0 para un plan gratuito
    [Range(0, 100000000000)]
    public long PriceCents { get; set; }

    // Duración en días: opcional, donde nulo representa un plan sin vencimiento
    [Range(1, 3650)]
    public int? DurationDays { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public record PlanResponse(
    Guid Id,
    string Name,
    long PriceCents,
    int? DurationDays,
    bool IsAvailable,
    DateTimeOffset CreatedAt
)
{
    public static PlanResponse From(Plan plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.PriceCents,
            plan.DurationDays,
            plan.IsAvailable,
            plan.CreatedAt
        );
}
