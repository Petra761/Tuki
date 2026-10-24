using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuki.Presupuestos.Data;
using Tuki.Presupuestos.Dtos;
using Tuki.Presupuestos.Models;

namespace Tuki.Presupuestos.Controllers;

[ApiController]
[Route("api/presupuestos/budgets")]
public class BudgetsController(PresupuestosDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BudgetPeriod? period,
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default
    )
    {
        var query = db.Budgets.AsNoTracking().AsQueryable();

        if (period is not null)
        {
            query = query.Where(b => b.Period == period);
        }

        // Los archivados se ocultan del listado por defecto porque siguen siendo
        // consultables por id: un PUT los restaura sin crear un registro nuevo
        if (!includeArchived)
        {
            query = query.Where(b => !b.IsArchived);
        }

        var budgets = await query.OrderBy(b => b.CreatedAt).ToListAsync(ct);
        return Ok(budgets.Select(BudgetResponse.From));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var budget = await db.Budgets.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        return budget is null
            ? NotFound(new { error = "El presupuesto no existe." })
            : Ok(BudgetResponse.From(budget));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBudgetRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Period))
        {
            return BadRequest(new { error = "El período no es un valor válido." });
        }

        // La base permite varios nulos en el índice único, así que el presupuesto
        // del gasto total (sin categoría) se controla aquí
        var exists = await db.Budgets.AnyAsync(
            b => b.CategoryId == request.CategoryId && b.Period == request.Period,
            ct
        );

        if (exists)
        {
            return Conflict(RepeatedBudget(request.CategoryId));
        }

        var now = DateTimeOffset.UtcNow;
        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            AmountCents = request.AmountCents,
            Period = request.Period,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = budget.Id },
            BudgetResponse.From(budget)
        );
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateBudgetRequest request,
        CancellationToken ct
    )
    {
        if (!Enum.IsDefined(request.Period))
        {
            return BadRequest(new { error = "El período no es un valor válido." });
        }

        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (budget is null)
        {
            return NotFound(new { error = "El presupuesto no existe." });
        }

        // Al mover el presupuesto a otra categoría o período no debe chocar
        // con el registro existente (se ignora el propio id)
        var repeated = await db.Budgets.AnyAsync(
            b => b.Id != id && b.CategoryId == request.CategoryId && b.Period == request.Period,
            ct
        );

        if (repeated)
        {
            return Conflict(RepeatedBudget(request.CategoryId));
        }

        budget.CategoryId = request.CategoryId;
        budget.AmountCents = request.AmountCents;
        budget.Period = request.Period;
        budget.IsArchived = request.IsArchived;
        budget.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(BudgetResponse.From(budget));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (budget is null)
        {
            return NotFound(new { error = "El presupuesto no existe." });
        }

        // La baja es por archivado: el historial de gastos puede seguir
        // consultando el presupuesto y un PUT puede restaurarlo
        if (!budget.IsArchived)
        {
            budget.IsArchived = true;
            budget.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    private static object RepeatedBudget(Guid? categoryId) =>
        new
        {
            error = categoryId is null
                ? "Ya existe un presupuesto del gasto total para ese período."
                : "Ya existe un presupuesto para esa categoría y período.",
        };
}
