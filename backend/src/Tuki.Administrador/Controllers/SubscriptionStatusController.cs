using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Data;
using Tuki.Administrador.Dtos;
using Tuki.Administrador.Services;

namespace Tuki.Administrador.Controllers;

[ApiController]
public class SubscriptionStatusController(AdminDbContext db) : ControllerBase
{
    [HttpGet("api/admin/users/{userId:guid}/subscription-status")]
    public async Task<IActionResult> GetStatus(
        Guid userId,
        [FromQuery] DateTimeOffset? at,
        CancellationToken ct
    )
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return NotFound(new { error = "El usuario no existe." });

        // Una sola lectura del reloj, en UTC, para toda la evaluación
        var now = (at ?? DateTimeOffset.UtcNow).ToUniversalTime();

        // Solo las suscripciones del usuario consultado
        var subscriptions = await db
            .Subscriptions.AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.StartedAt <= now)
            .ToListAsync(ct);

        var current =
            subscriptions.FirstOrDefault(s => SubscriptionEvaluator.IsActive(s, now))
            ?? subscriptions.OrderByDescending(s => s.ExpiresAt).FirstOrDefault();

        if (current is null)
        {
            return Ok(
                new SubscriptionStatusResponse(
                    user.Id,
                    nameof(SubscriptionResult.NonExistent),
                    "El usuario no tiene ninguna suscripción hasta la fecha consultada.",
                    null,
                    user.TenantId,
                    null,
                    null,
                    null,
                    now
                )
            );
        }

        var (result, reason) = SubscriptionEvaluator.Evaluate(current, now);

        return Ok(
            new SubscriptionStatusResponse(
                user.Id,
                result.ToString(),
                reason,
                current.Plan.Name,
                current.TenantId,
                current.StartedAt,
                current.ExpiresAt,
                current.Status.ToString(),
                now
            )
        );
    }
}
