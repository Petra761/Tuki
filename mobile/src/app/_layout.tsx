import { useEffect } from "react";
import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import * as SplashScreen from "expo-splash-screen";
import { useFonts } from "expo-font";
import { BricolageGrotesque_700Bold } from "@expo-google-fonts/bricolage-grotesque";
import {
  Onest_400Regular,
  Onest_500Medium,
  Onest_700Bold,
} from "@expo-google-fonts/onest";
import { AuthProvider, useAuth } from "@/auth/AuthContext";
import { colors } from "@/theme";

// La pantalla de carga se mantiene hasta tener las fuentes y la sesión guardada
void SplashScreen.preventAutoHideAsync();

function Gate({ fontsLoaded }: { fontsLoaded: boolean }) {
  const { session, loading } = useAuth();
  const ready = fontsLoaded && !loading;

  useEffect(() => {
    if (ready) void SplashScreen.hideAsync();
  }, [ready]);

  if (!ready) return null;

  return (
    <Stack
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: colors.background },
      }}
    >
      <Stack.Protected guard={!!session}>
        <Stack.Screen name="(app)" />
      </Stack.Protected>
      <Stack.Protected guard={!session}>
        <Stack.Screen name="(auth)" />
      </Stack.Protected>
    </Stack>
  );
}

export default function RootLayout() {
  const [fontsLoaded] = useFonts({
    BricolageGrotesque_700Bold,
    Onest_400Regular,
    Onest_500Medium,
    Onest_700Bold,
  });

  return (
    <AuthProvider>
      <StatusBar style="light" />
      <Gate fontsLoaded={fontsLoaded} />
    </AuthProvider>
  );
}
