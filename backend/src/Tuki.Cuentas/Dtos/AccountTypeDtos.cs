using System.ComponentModel.DataAnnotations;
using Tuki.Cuentas.Models;

namespace Tuki.Cuentas.Dtos;

public class CreateAccountTypeRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsPerson { get; set; }
}

// El PUT reemplaza el tipo completo; IsArchived permite restaurarlo
public class UpdateAccountTypeRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsPerson { get; set; }

    public bool IsArchived { get; set; }
}

public record AccountTypeResponse(
    Guid Id,
    string Name,
    bool IsPerson,
    bool IsArchived,
    DateTimeOffset CreatedAt
)
{
    public static AccountTypeResponse From(AccountType accountType) =>
        new(
            accountType.Id,
            accountType.Name,
            accountType.IsPerson,
            accountType.IsArchived,
            accountType.CreatedAt
        );
}
