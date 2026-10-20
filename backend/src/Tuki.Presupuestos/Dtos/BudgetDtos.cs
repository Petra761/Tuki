using System.ComponentModel.DataAnnotations;
using Tuki.Presupuestos.Models;

namespace Tuki.Presupuestos.Dtos;

public class CreateBudgetRequest
{
    // Id de la categoría en el servicio Cuentas; vacío es el presupuesto del gasto total
    public Guid? CategoryId { get; set; }

    [Range(0.01, 1000000000)]
    public decimal Amount { get; set; }

    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;
}

public record BudgetResponse(
    Guid Id,
    Guid? CategoryId,
    decimal Amount,
    BudgetPeriod Period,
    bool IsArchived,
    DateTimeOffset CreatedAt
)
{
    public static BudgetResponse From(Budget budget) =>
        new(
            budget.Id,
            budget.CategoryId,
            budget.Amount,
            budget.Period,
            budget.IsArchived,
            budget.CreatedAt
        );
}
