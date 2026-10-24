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

    // true si el plan no muestra anuncios; los planes de pago lo traen y el
    // plan Gratis no (false por defecto es el valor que quiere el dominio)
    public bool RemovesAds { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
