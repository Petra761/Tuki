namespace Tuki.Cuentas.Models;

public class Category
{
    public Guid Id { get; set; }

    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }

    public string Name { get; set; } = string.Empty;
    public CategoryKind Kind { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
