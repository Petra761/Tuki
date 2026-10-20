using System.Globalization;
using Tuki.Administrador.Models;

namespace Tuki.Administrador.Services;

public enum SubscriptionResult
{
    Active,
    Expired,
    NonExistent,
}

public static class SubscriptionEvaluator
{
    // Vigente desde StartedAt (incluido) hasta antes de ExpiresAt (excluido), todo en UTC.
    // Que el vencimiento quede excluido es lo que hace que, al vencer, deje de estar activa.
    public static bool IsActive(Subscription subscription, DateTimeOffset now) =>
        subscription.Status == SubscriptionStatus.Enabled
        && subscription.StartedAt <= now
        && now < subscription.ExpiresAt;

    public static (SubscriptionResult Result, string Reason) Evaluate(
        Subscription subscription,
        DateTimeOffset now
    )
    {
        if (IsActive(subscription, now))
            return (
                SubscriptionResult.Active,
                $"Vigente hasta el {Format(subscription.ExpiresAt)}."
            );

        // El enunciado solo define activa, vencida o inexistente: una cancelada ya no está
        // habilitada y se informa como vencida, con el motivo en Reason
        if (subscription.Status == SubscriptionStatus.Cancelled)
            return (SubscriptionResult.Expired, "La suscripción fue cancelada.");

        return (
            SubscriptionResult.Expired,
            $"La suscripción venció el {Format(subscription.ExpiresAt)}."
        );
    }

    private static string Format(DateTimeOffset date) =>
        date.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
