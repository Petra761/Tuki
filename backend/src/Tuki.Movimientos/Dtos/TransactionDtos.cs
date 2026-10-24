using System.ComponentModel.DataAnnotations;
using Tuki.Movimientos.Models;

namespace Tuki.Movimientos.Dtos;

public class TransactionLineInput
{
    // Cuenta y categoría viven en el servicio Cuentas: se guardan como identificador
    public Guid AccountId { get; set; }

    // Nula en traslados y aperturas: nadie consumió nada
    public Guid? CategoryId { get; set; }

    // Con signo: negativo resta de la cuenta y positivo suma; nunca puede ser 0
    public long AmountCents { get; set; }
}

// Cuerpo común de POST y PUT: el PUT reemplaza la transacción completa
public class TransactionBody
{
    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    // DateOnly no admite [Required]: la obligatoriedad se comprueba por valor
    // por defecto en el controlador
    public DateOnly OccurredOn { get; set; }

    public List<TransactionLineInput> Lines { get; set; } = [];

    public List<Guid> TagIds { get; set; } = [];
}

public class CreateTransactionRequest : TransactionBody;

public class UpdateTransactionRequest : TransactionBody;

public record TransactionLineResponse(Guid Id, Guid AccountId, Guid? CategoryId, long AmountCents)
{
    public static TransactionLineResponse From(TransactionLine line) =>
        new(line.Id, line.AccountId, line.CategoryId, line.AmountCents);
}

public record TransactionResponse(
    Guid Id,
    string Type,
    DateOnly OccurredOn,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<TransactionLineResponse> Lines,
    List<Guid> TagIds
)
{
    public static TransactionResponse From(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.OccurredOn,
            transaction.Description,
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.Lines.Select(TransactionLineResponse.From).ToList(),
            transaction.TransactionTags.Select(t => t.TagId).Order().ToList()
        );
}

public record TransactionListResponse(
    List<TransactionResponse> Items,
    int Page,
    int PageSize,
    int Total
);
