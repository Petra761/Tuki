using System.ComponentModel.DataAnnotations;
using Tuki.Presupuestos.Models;

namespace Tuki.Presupuestos.Dtos;

public class CreateBudgetRequest
{
    // Id de la categoría en el servicio Cuentas; vacío es el presupuesto del gasto total
    public Guid? CategoryId { get; set; }

    [Range(1, 100000000000)]
    public long AmountCents { get; set; }

    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;
}

// El PUT reemplaza el presupuesto completo; IsArchived permite restaurar uno dado de baja
public class UpdateBudgetRequest
{
    public Guid? CategoryId { get; set; }

    [Range(1, 100000000000)]
    public long AmountCents { get; set; }

    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;

    public bool IsArchived { get; set; }
}

public record BudgetResponse(
    Guid Id,
    Guid? CategoryId,
    long AmountCents,
    BudgetPeriod Period,
    bool IsArchived,
    DateTimeOffset CreatedAt
)
{
    public static BudgetResponse From(Budget budget) =>
        new(
            budget.Id,
            budget.CategoryId,
            budget.AmountCents,
            budget.Period,
            budget.IsArchived,
            budget.CreatedAt
        );
}
