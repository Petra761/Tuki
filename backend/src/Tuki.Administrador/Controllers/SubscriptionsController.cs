using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Data;
using Tuki.Administrador.Dtos;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Controllers;

[ApiController]
[Route("api/admin/subscriptions")]
public class SubscriptionsController(AdminDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Subscribe(SubscribeRequest request, CancellationToken ct)
    {
        // La tabla de usuarios vive en Autenticación: el tenant del que se
        // suscribe llega en la cabecera, no se deduce de un usuario
        if (!CabeceraTenant.TryGet(HttpContext, out var tenantId))
            return BadRequest(new { error = "Debes enviar un X-Tenant-Id valido." });

        if (!await db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenantId, ct))
            return NotFound(new { error = "El tenant no existe." });

        var plan = await db
            .Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, ct);
        if (plan is null)
            return NotFound(new { error = "El plan no existe." });

        if (!plan.IsAvailable)
            return Conflict(new { error = "El plan no admite nuevas suscripciones." });

        var now = DateTimeOffset.UtcNow;

        // Npgsql solo acepta fechas con offset UTC al guardar
        var startedAt = (request.StartedAt ?? now).ToUniversalTime();
        if (startedAt > now)
            return BadRequest(new { error = "La fecha de inicio no puede ser futura." });

        DateTimeOffset? expiresAt = plan.DurationDays.HasValue
            ? startedAt.AddDays(plan.DurationDays.Value)
            : null;

        if (expiresAt.HasValue && expiresAt <= startedAt)
            return BadRequest(new { error = "El vencimiento debe ser posterior al inicio." });

        var hasActive = await db.Subscriptions.AnyAsync(
            s =>
                s.UserId == request.UserId
                && s.Status == SubscriptionStatus.Enabled
                && s.StartedAt <= now
                && (s.ExpiresAt == null || s.ExpiresAt > now),
            ct
        );

        if (hasActive)
            return Conflict(new { error = "El usuario ya tiene una suscripción activa." });

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            PlanId = plan.Id,
            TenantId = tenantId,
            StartedAt = startedAt,
            ExpiresAt = expiresAt,
            Status = SubscriptionStatus.Enabled,
        };

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = subscription.Id },
            SubscriptionResponse.From(subscription, plan.Name)
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var subscription = await db
            .Subscriptions.AsNoTracking()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return subscription is null
            ? NotFound(new { error = "La suscripción no existe." })
            : Ok(SubscriptionResponse.From(subscription, subscription.Plan.Name));
    }

    [HttpGet("~/api/admin/users/{userId:guid}/subscriptions")]
    public async Task<IActionResult> GetByUser(Guid userId, CancellationToken ct)
    {
        // Sin la tabla de usuarios no se puede distinguir un usuario inexistente
        // de uno sin suscripciones: siempre se responde la lista (vacía si no hay)
        var subscriptions = await db
            .Subscriptions.AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);

        return Ok(subscriptions.Select(s => SubscriptionResponse.From(s, s.Plan.Name)));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var subscription = await db
            .Subscriptions.Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (subscription is null)
            return NotFound(new { error = "La suscripción no existe." });

        if (subscription.Status == SubscriptionStatus.Cancelled)
            return Conflict(new { error = "La suscripción ya está cancelada." });

        subscription.Status = SubscriptionStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Ok(SubscriptionResponse.From(subscription, subscription.Plan.Name));
    }
}
