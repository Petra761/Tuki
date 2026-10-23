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
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var budgets = await db.Budgets.AsNoTracking().OrderBy(b => b.CreatedAt).ToListAsync(ct);
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
        // La base permite varios nulos en el índice único, así que el presupuesto
        // del gasto total (sin categoría) se controla aquí
        var exists = await db.Budgets.AnyAsync(
            b => b.CategoryId == request.CategoryId && b.Period == request.Period,
            ct
        );

        if (exists)
        {
            return Conflict(
                new
                {
                    error = request.CategoryId is null
                        ? "Ya existe un presupuesto del gasto total para ese período."
                        : "Ya existe un presupuesto para esa categoría y período.",
                }
            );
        }

        var now = DateTimeOffset.UtcNow;
        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Amount = request.Amount,
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
}
