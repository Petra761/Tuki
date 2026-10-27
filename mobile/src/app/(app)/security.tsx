import { useAuth } from "@/auth/AuthContext";
import { Card, MenuRow, Screen, SectionTitle } from "@/components/Layout";
import { Text } from "@/components/Text";
import { formatDateTime } from "@/lib/dates";

export default function SecurityScreen() {
  const { session } = useAuth();

  return (
    <Screen>
      <SectionTitle>Sesión</SectionTitle>
      <Card>
        <Text variant="medium">Sesión abierta en este dispositivo</Text>
        <Text variant="label">
          {session ? `Vence el ${formatDateTime(session.expiresAt)}. Después tendrás que volver a iniciar sesión.` : ""}
        </Text>
      </Card>

      <SectionTitle>Cuenta</SectionTitle>
      <Card>
        <Text variant="medium">Correo</Text>
        <Text variant="label">
          {session?.user.email} · {session?.user.emailVerified ? "verificado" : "sin verificar"}
        </Text>
      </Card>

      <SectionTitle>Próximamente</SectionTitle>
      <MenuRow icon="keypad-outline" label="PIN de la aplicación" hint="Bloquear Tuki al abrirla" />
      <MenuRow icon="key-outline" label="Cambiar contraseña" hint="Aún no está disponible" />
    </Screen>
  );
}
