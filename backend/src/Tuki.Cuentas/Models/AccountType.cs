namespace Tuki.Cuentas.Models;

public class AccountType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsPerson { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
