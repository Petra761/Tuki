using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Administrador.Data;
using Tuki.Administrador.Dtos;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Controllers;

[ApiController]
[Route("api/admin/plans")]
public class PlansController(AdminDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var plans = await db.Plans.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
        return Ok(plans.Select(PlanResponse.From));
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(CancellationToken ct)
    {
        var plans = await db
            .Plans.AsNoTracking()
            .Where(p => p.IsAvailable)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return Ok(plans.Select(PlanResponse.From));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return plan is null
            ? NotFound(new { error = "El plan no existe." })
            : Ok(PlanResponse.From(plan));
    }

    [HttpPost]
    public async Task<IActionResult> Create(PlanRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (await db.Plans.AnyAsync(p => p.Name.ToLower() == name.ToLower(), ct))
            return Conflict(new { error = "Ya existe un plan con ese nombre." });

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = request.Price,
            DurationDays = request.DurationDays,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, PlanResponse.From(plan));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PlanRequest request, CancellationToken ct)
    {
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
            return NotFound(new { error = "El plan no existe." });

        var name = request.Name.Trim();

        if (await db.Plans.AnyAsync(p => p.Id != id && p.Name.ToLower() == name.ToLower(), ct))
            return Conflict(new { error = "Ya existe otro plan con ese nombre." });

        plan.Name = name;
        plan.Price = request.Price;
        plan.DurationDays = request.DurationDays;
        plan.IsAvailable = request.IsAvailable;
        await db.SaveChangesAsync(ct);

        return Ok(PlanResponse.From(plan));
    }
}
