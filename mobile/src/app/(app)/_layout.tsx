import { Stack } from "expo-router";
import { colors, fontFamily } from "@/theme";

export default function AppLayout() {
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colors.background },
        headerTintColor: colors.text,
        headerTitleStyle: { fontFamily: fontFamily.title },
        headerShadowVisible: false,
        headerBackButtonDisplayMode: "minimal",
        contentStyle: { backgroundColor: colors.background },
      }}
    >
      <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
      <Stack.Screen name="transaction-form" options={{ title: "Movimiento", presentation: "modal" }} />
      <Stack.Screen name="budgets" options={{ title: "Presupuestos" }} />
      <Stack.Screen name="tags" options={{ title: "Marcadores" }} />
      <Stack.Screen name="shortcuts" options={{ title: "Atajos" }} />
      <Stack.Screen name="account-types" options={{ title: "Tipos de cuenta" }} />
      <Stack.Screen name="categories" options={{ title: "Categorías" }} />
      <Stack.Screen name="security" options={{ title: "Seguridad" }} />
      <Stack.Screen name="help" options={{ title: "Ayuda" }} />
      <Stack.Screen name="settings" options={{ title: "Configuración" }} />
    </Stack>
  );
}
