import { BricolageGrotesque_700Bold } from "@expo-google-fonts/bricolage-grotesque";
import {
  Onest_400Regular,
  Onest_500Medium,
  Onest_700Bold,
} from "@expo-google-fonts/onest";
import { useFonts } from "expo-font";
import { Stack } from "expo-router";
import * as SplashScreen from "expo-splash-screen";
import { StatusBar } from "expo-status-bar";
import { useEffect } from "react";

import { colors } from "@/constants/theme";

// La pantalla de carga se queda visible hasta que las fuentes estén listas,
// para que el texto no aparezca primero con la tipografía del sistema
SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  const [fontsLoaded, fontError] = useFonts({
    BricolageGrotesque_700Bold,
    Onest_400Regular,
    Onest_500Medium,
    Onest_700Bold,
  });

  useEffect(() => {
    // Si una fuente falla se muestra la app igual, con la tipografía del sistema
    if (fontsLoaded || fontError) SplashScreen.hideAsync();
  }, [fontsLoaded, fontError]);

  if (!fontsLoaded && !fontError) return null;

  return (
    <>
      <StatusBar style="light" />
      <Stack
        screenOptions={{
          headerShown: false,
          contentStyle: { backgroundColor: colors.background },
        }}
      />
    </>
  );
}
