import { useState } from "react";
import { KeyboardAvoidingView, Platform, StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { register } from "@/api/auth";
import { ApiError, errorMessage } from "@/api/client";
import { useAuth } from "@/auth/AuthContext";
import { Button } from "@/components/Button";
import { Field } from "@/components/Inputs";
import { Screen } from "@/components/Layout";
import { Text } from "@/components/Text";
import { colors, spacing } from "@/theme";

export default function RegisterScreen() {
  const { signIn } = useAuth();
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit() {
    if (!displayName.trim() || !email.trim() || !password) {
      setError("Completa tu nombre, tu correo y una contraseña.");
      return;
    }
    if (!/^\S+@\S+\.\S+$/.test(email.trim())) {
      setError("Escribe un correo válido.");
      return;
    }
    if (password.length < 8) {
      setError("La contraseña debe tener al menos 8 caracteres.");
      return;
    }
    if (password !== confirm) {
      setError("Las contraseñas no coinciden.");
      return;
    }

    setError(null);
    setLoading(true);
    try {
      await register({ email, displayName: displayName.trim(), password });
      // Con la cuenta lista se inicia sesión directamente
      await signIn(email, password);
    } catch (e) {
      // Un 502 al registrar significa que no se pudo preparar la cuenta; es seguro reintentar
      setError(
        e instanceof ApiError && e.status === 502
          ? "No se pudo preparar tu cuenta. Intenta de nuevo en unos segundos."
          : errorMessage(e),
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === "ios" ? "padding" : undefined}>
      <Screen tab>
        <View style={styles.header}>
          <Text variant="title">Crea tu cuenta</Text>
          <Text variant="label">Prepararla toma unos segundos: Tuki crea tu espacio personal de datos.</Text>
        </View>

        <View style={styles.form}>
          <Field label="Nombre" value={displayName} onChangeText={setDisplayName} autoComplete="name" placeholder="Ana Pérez" />
          <Field
            label="Correo"
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            autoComplete="email"
            keyboardType="email-address"
            placeholder="ana@ejemplo.com"
          />
          <Field label="Contraseña" value={password} onChangeText={setPassword} secureTextEntry autoCapitalize="none" hint="Mínimo 8 caracteres" />
          <Field
            label="Repite la contraseña"
            value={confirm}
            onChangeText={setConfirm}
            secureTextEntry
            autoCapitalize="none"
            onSubmitEditing={submit}
          />
          {error ? (
            <Text variant="medium" color={colors.danger}>
              {error}
            </Text>
          ) : null}
          <Button label="Crear cuenta" onPress={submit} loading={loading} />
          <Button label="Ya tengo una cuenta" variant="ghost" onPress={() => router.back()} />
        </View>
      </Screen>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1, backgroundColor: colors.background },
  header: { gap: spacing.sm, paddingTop: spacing.lg },
  form: { gap: spacing.md },
});
