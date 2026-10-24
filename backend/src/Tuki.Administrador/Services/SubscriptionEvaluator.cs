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
    // Vigente desde StartedAt (incluido) hasta antes de ExpiresAt (si tiene vencimiento).
    // Una suscripción sin fecha de vencimiento y no cancelada permanece activa indefinidamente.
    public static bool IsActive(Subscription subscription, DateTimeOffset now) =>
        subscription.Status == SubscriptionStatus.Enabled
        && subscription.StartedAt <= now
        && (!subscription.ExpiresAt.HasValue || now < subscription.ExpiresAt.Value);

    public static (SubscriptionResult Result, string Reason) Evaluate(
        Subscription subscription,
        DateTimeOffset now
    )
    {
        if (IsActive(subscription, now))
        {
            var reason = subscription.ExpiresAt.HasValue
                ? $"Vigente hasta el {Format(subscription.ExpiresAt.Value)}."
                : "Plan sin vencimiento.";
            return (SubscriptionResult.Active, reason);
        }

        // El enunciado solo define activa, vencida o inexistente: una cancelada ya no está
        // habilitada y se informa como vencida, con el motivo en Reason
        if (subscription.Status == SubscriptionStatus.Cancelled)
            return (SubscriptionResult.Expired, "La suscripción fue cancelada.");

        return (
            SubscriptionResult.Expired,
            subscription.ExpiresAt.HasValue
                ? $"La suscripción venció el {Format(subscription.ExpiresAt.Value)}."
                : "La suscripción no está activa."
        );
    }

    private static string Format(DateTimeOffset date) =>
        date.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
