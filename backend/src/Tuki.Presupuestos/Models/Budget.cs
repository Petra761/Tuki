namespace Tuki.Presupuestos.Models;

public class Budget
{
    public Guid Id { get; set; }

    // Id de la categoría en el servicio Cuentas, sin navegación. Nulo es el presupuesto del gasto total
    public Guid? CategoryId { get; set; }

    // Límite de gasto, positivo
    public decimal Amount { get; set; }

    public BudgetPeriod Period { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
