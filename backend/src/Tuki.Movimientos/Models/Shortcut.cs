namespace Tuki.Movimientos.Models;

public class Shortcut
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Monto positivo; el signo lo decide la lógica al crear el gasto
    public decimal Amount { get; set; }

    // Ids del servicio Cuentas, sin navegación: se validan por API
    public Guid CategoryId { get; set; }
    public Guid AccountId { get; set; }

    public int SortOrder { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
