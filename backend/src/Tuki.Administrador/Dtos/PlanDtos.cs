using System.ComponentModel.DataAnnotations;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Dtos;

public class PlanRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // El precio puede ser 0 para un plan gratuito
    [Range(typeof(decimal), "0", "1000000")]
    public decimal Price { get; set; }

    [Range(1, 3650)]
    public int DurationDays { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public record PlanResponse(
    Guid Id,
    string Name,
    decimal Price,
    int DurationDays,
    bool IsAvailable,
    DateTimeOffset CreatedAt
)
{
    public static PlanResponse From(Plan plan) =>
        new(plan.Id, plan.Name, plan.Price, plan.DurationDays, plan.IsAvailable, plan.CreatedAt);
}
