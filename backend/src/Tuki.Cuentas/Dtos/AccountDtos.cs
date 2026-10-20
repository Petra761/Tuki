using System.ComponentModel.DataAnnotations;
using Tuki.Cuentas.Models;

namespace Tuki.Cuentas.Dtos;

public class CreateAccountRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public Guid AccountTypeId { get; set; }
}

public record AccountResponse(
    Guid Id,
    Guid AccountTypeId,
    string AccountType,
    string Name,
    string CurrencyCode,
    bool IsDefault,
    bool IsArchived,
    DateTimeOffset CreatedAt
)
{
    public static AccountResponse From(Account account, string accountTypeName) =>
        new(
            account.Id,
            account.AccountTypeId,
            accountTypeName,
            account.Name,
            account.CurrencyCode,
            account.IsDefault,
            account.IsArchived,
            account.CreatedAt
        );
}
