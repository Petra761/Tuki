namespace Tuki.Cuentas.Models;

public class Account
{
    public Guid Id { get; set; }

    public Guid AccountTypeId { get; set; }
    public AccountType AccountType { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "BOB";

    public bool IsDefault { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
