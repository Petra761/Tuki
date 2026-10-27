import Constants from "expo-constants";
import { API_URL } from "@/api/config";
import { getSubscriptionStatus } from "@/api/resources";
import { useAuth } from "@/auth/AuthContext";
import { Card, ErrorNote, Screen, SectionTitle } from "@/components/Layout";
import { Text } from "@/components/Text";
import { useResource } from "@/hooks/useResource";

export default function SettingsScreen() {
  const { session } = useAuth();
  const userId = session?.user.id ?? "";
  const plan = useResource(() => getSubscriptionStatus(userId), [userId]);

  const planName = plan.data?.result === "Active" ? (plan.data.plan ?? "Activo") : "Gratis";

  return (
    <Screen>
      <SectionTitle>Perfil</SectionTitle>
      <Card>
        <Text variant="medium">{session?.user.displayName}</Text>
        <Text variant="label">{session?.user.email}</Text>
      </Card>

      <SectionTitle>Plan</SectionTitle>
      <Card>
        <Text variant="medium">{plan.data ? planName : "Consultando…"}</Text>
        {plan.data ? (
          <Text variant="label">{plan.data.showAds ? "Con anuncios" : "Sin anuncios"}</Text>
        ) : null}
      </Card>
      {plan.error ? <ErrorNote message={plan.error} onRetry={plan.reload} /> : null}

      <SectionTitle>Aplicación</SectionTitle>
      <Card>
        <Text variant="medium">Servidor</Text>
        <Text variant="label">{API_URL}</Text>
        <Text variant="medium">Versión</Text>
        <Text variant="label">{Constants.expoConfig?.version ?? "—"}</Text>
      </Card>
    </Screen>
  );
}
