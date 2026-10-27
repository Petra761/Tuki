import { StyleSheet, View, type ColorValue } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { Tabs, useRouter } from "expo-router";
import { PressableScale } from "@/components/Button";
import { colors, fontFamily, radius } from "@/theme";

type IconName = keyof typeof Ionicons.glyphMap;

function tabIcon(name: IconName, focusedName: IconName) {
  return ({ color, focused }: { color: ColorValue; focused: boolean }) => (
    <Ionicons name={focused ? focusedName : name} size={24} color={color as string} />
  );
}

// Botón central: registrar un movimiento tiene que estar siempre a un toque
function RegisterButton() {
  const router = useRouter();
  return (
    <View style={styles.center}>
      <PressableScale onPress={() => router.push("/transaction-form")} accessibilityLabel="Registrar movimiento" style={styles.fab}>
        <Ionicons name="add" size={30} color={colors.onPrimary} />
      </PressableScale>
    </View>
  );
}

export default function TabsLayout() {
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.textDim,
        tabBarStyle: { backgroundColor: colors.backgroundDeep, borderTopColor: colors.border },
        tabBarLabelStyle: { fontFamily: fontFamily.bodyMedium, fontSize: 11 },
      }}
    >
      <Tabs.Screen name="index" options={{ title: "Inicio", tabBarIcon: tabIcon("home-outline", "home") }} />
      <Tabs.Screen name="transactions" options={{ title: "Movimientos", tabBarIcon: tabIcon("list-outline", "list") }} />
      <Tabs.Screen name="new" options={{ title: "Registrar", tabBarButton: () => <RegisterButton /> }} />
      <Tabs.Screen name="accounts" options={{ title: "Cuentas", tabBarIcon: tabIcon("wallet-outline", "wallet") }} />
      <Tabs.Screen name="more" options={{ title: "Más", tabBarIcon: tabIcon("ellipsis-horizontal", "ellipsis-horizontal") }} />
    </Tabs>
  );
}

const styles = StyleSheet.create({
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  fab: {
    width: 54,
    height: 54,
    borderRadius: radius.full,
    backgroundColor: colors.primary,
    alignItems: "center",
    justifyContent: "center",
  },
});
