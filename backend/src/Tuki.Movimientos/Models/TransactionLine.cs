namespace Tuki.Movimientos.Models;

public class TransactionLine
{
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    // Ids del servicio Cuentas, sin navegación: se validan por API
    public Guid AccountId { get; set; }
    public Guid? CategoryId { get; set; }

    // Con signo: negativo resta de la cuenta y positivo suma
    public long AmountCents { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
