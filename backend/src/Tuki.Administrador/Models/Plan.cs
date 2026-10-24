namespace Tuki.Administrador.Models;

public class Plan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long PriceCents { get; set; }

    // Duración en días del plan; nulo significa que no tiene vencimiento
    public int? DurationDays { get; set; }

    // Un plan que deja de ofrecerse no se borra: así las suscripciones viejas siguen válidas
    public bool IsAvailable { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
