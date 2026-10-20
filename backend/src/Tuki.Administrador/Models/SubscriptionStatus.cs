namespace Tuki.Administrador.Models;

// "Vencida" no es un estado guardado: se calcula comparando la fecha actual con ExpiresAt
public enum SubscriptionStatus
{
    Enabled,
    Cancelled,
}
