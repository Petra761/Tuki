namespace Tuki.Movimientos.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }

    // Fecha sin hora: el balance se agrupa por día y por mes
    public DateOnly OccurredOn { get; set; }

    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<TransactionLine> Lines { get; set; } = [];
    public List<TransactionTag> TransactionTags { get; set; } = [];
}
