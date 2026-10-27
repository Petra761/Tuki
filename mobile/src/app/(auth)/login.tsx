import { useState } from "react";
import { KeyboardAvoidingView, Platform, StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { getLockedUntil } from "@/api/auth";
import { ApiError, errorMessage } from "@/api/client";
import { useAuth } from "@/auth/AuthContext";
import { Button } from "@/components/Button";
import { Field } from "@/components/Inputs";
import { Screen } from "@/components/Layout";
import { Text } from "@/components/Text";
import { formatTime } from "@/lib/dates";
import { colors, spacing } from "@/theme";

export default function LoginScreen() {
  const { signIn } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit() {
    if (!email.trim() || !password) {
      setError("Escribe tu correo y tu contraseña.");
      return;
    }
    setError(null);
    setLoading(true);
    try {
      await signIn(email, password);
      // La redirección a Inicio la hace el guardián de rutas al detectar la sesión
    } catch (e) {
      const lockedUntil = e instanceof ApiError ? getLockedUntil(e) : null;
      setError(
        lockedUntil
          ? `Cuenta bloqueada por intentos fallidos. Vuelve a intentar a las ${formatTime(lockedUntil)}.`
          : errorMessage(e),
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === "ios" ? "padding" : undefined}>
      <Screen tab>
        <View style={styles.brand}>
          <Text variant="big" color={colors.primary}>
            tuki
          </Text>
          <Text variant="heading">Sabe a dónde se va tu dinero</Text>
        </View>

        <View style={styles.form}>
          <Field
            label="Correo"
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            autoComplete="email"
            keyboardType="email-address"
            placeholder="ana@ejemplo.com"
          />
          <Field
            label="Contraseña"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            autoCapitalize="none"
            autoComplete="password"
            onSubmitEditing={submit}
          />
          {error ? (
            <Text variant="medium" color={colors.danger}>
              {error}
            </Text>
          ) : null}
          <Button label="Iniciar sesión" onPress={submit} loading={loading} />
          <Button label="Crear una cuenta" variant="ghost" onPress={() => router.push("/register")} />
        </View>
      </Screen>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1, backgroundColor: colors.background },
  brand: { gap: spacing.sm, paddingTop: spacing.xl, paddingBottom: spacing.md },
  form: { gap: spacing.md },
});
