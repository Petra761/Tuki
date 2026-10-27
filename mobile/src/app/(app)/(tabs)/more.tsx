import { Alert, StyleSheet, View, useWindowDimensions } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useRouter } from "expo-router";
import { useAuth } from "@/auth/AuthContext";
import { Button, PressableScale } from "@/components/Button";
import { Screen, SectionTitle, TabHeader } from "@/components/Layout";
import { Text } from "@/components/Text";
import { colors, radius, spacing } from "@/theme";

type MenuItem = {
  icon: keyof typeof Ionicons.glyphMap;
  label: string;
  hint: string;
  onPress: () => void;
};

const COLUMNS = 3;
const GRID_GAP = spacing.xs + 2;
const TILE_HEIGHT = 100;

function MenuTile({ item, width }: { item: MenuItem; width: number }) {
  return (
    <PressableScale
      onPress={item.onPress}
      accessibilityRole="button"
      accessibilityLabel={item.label}
      accessibilityHint={item.hint}
      containerStyle={[styles.tileContainer, { width }]}
      style={styles.tile}
    >
      <View style={styles.tileIcon}>
        <Ionicons name={item.icon} size={19} color={colors.primary} />
      </View>

      <Text variant="medium" numberOfLines={2} style={styles.tileLabel}>
        {item.label}
      </Text>
    </PressableScale>
  );
}

export default function MoreScreen() {
  const router = useRouter();
  const { session, signOut } = useAuth();
  const { width: screenWidth } = useWindowDimensions();

  const user = session?.user;

  // Ajusta el ancho de las tarjetas a la pantalla.
  const horizontalPadding = spacing.md * 2;
  const availableWidth = screenWidth - horizontalPadding;
  const tileWidth = (availableWidth - GRID_GAP * (COLUMNS - 1)) / COLUMNS;

  const menuSections: { title: string; items: MenuItem[] }[] = [
    {
      title: "Registro",
      items: [
        {
          icon: "flash-outline",
          label: "Atajos",
          hint: "Gastos frecuentes",
          onPress: () => router.push("/shortcuts"),
        },
        {
          icon: "pricetag-outline",
          label: "Marcadores",
          hint: "Agrupa movimientos",
          onPress: () => router.push("/tags"),
        },
        {
          icon: "pie-chart-outline",
          label: "Presupuestos",
          hint: "Límites de gasto",
          onPress: () => router.push("/budgets"),
        },
      ],
    },
    {
      title: "Organización",
      items: [
        {
          icon: "albums-outline",
          label: "Tipos de cuenta",
          hint: "Organiza tus cuentas",
          onPress: () => router.push("/account-types"),
        },
        {
          icon: "apps-outline",
          label: "Categorías",
          hint: "Clasifica tus gastos",
          onPress: () => router.push("/categories"),
        },
      ],
    },
    {
      title: "Aplicación",
      items: [
        {
          icon: "shield-checkmark-outline",
          label: "Seguridad",
          hint: "Protege tu cuenta",
          onPress: () => router.push("/security"),
        },
        {
          icon: "help-circle-outline",
          label: "Ayuda",
          hint: "Guía y preguntas",
          onPress: () => router.push("/help"),
        },
        {
          icon: "settings-outline",
          label: "Configuración",
          hint: "Tu cuenta y plan",
          onPress: () => router.push("/settings"),
        },
      ],
    },
  ];

  function confirmSignOut() {
    Alert.alert(
      "Cerrar sesión",
      "Tendrás que iniciar sesión otra vez para ver tus datos.",
      [
        { text: "Cancelar", style: "cancel" },
        {
          text: "Cerrar sesión",
          style: "destructive",
          onPress: () => void signOut(),
        },
      ],
    );
  }

  return (
    <Screen tab>
      <TabHeader title="Más" />

      <View style={styles.profile}>
        <View style={styles.avatar}>
          <Text variant="medium" color={colors.onPrimary}>
            {(user?.displayName ?? user?.email ?? "?")
              .trim()
              .charAt(0)
              .toUpperCase()}
          </Text>
        </View>

        <View style={styles.profileText}>
          <Text variant="bold">{user?.displayName || "Tu perfil"}</Text>
          {user?.email ? <Text variant="label">{user.email}</Text> : null}
        </View>

        <Ionicons
          name="person-circle-outline"
          size={22}
          color={colors.textDim}
        />
      </View>

      <View style={styles.menuGroups}>
        {menuSections.map((section) => (
          <View key={section.title} style={styles.section}>
            <SectionTitle>{section.title}</SectionTitle>

            <View style={styles.grid}>
              {section.items.map((item) => (
                <MenuTile
                  key={item.label}
                  item={item}
                  width={tileWidth}
                />
              ))}
            </View>
          </View>
        ))}
      </View>

      <Button
        label="Cerrar sesión"
        variant="danger"
        icon="log-out-outline"
        onPress={confirmSignOut}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  profile: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.sm + 4,
  },

  avatar: {
    width: 40,
    height: 40,
    borderRadius: radius.full,
    backgroundColor: colors.primary,
    alignItems: "center",
    justifyContent: "center",
  },

  profileText: {
    flex: 1,
    gap: spacing.xs,
  },

  menuGroups: {
    gap: spacing.md,
  },

  section: {
    gap: spacing.xs,
  },

  grid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: GRID_GAP,
  },

  tileContainer: {
    flexGrow: 0,
    flexShrink: 0,
  },

  tile: {
    width: "100%",
    height: TILE_HEIGHT,
    alignItems: "center",
    justifyContent: "center",
    gap: spacing.xs,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.xs,
    paddingVertical: spacing.sm,
  },

  tileIcon: {
    width: 28,
    height: 28,
    borderRadius: radius.sm,
    alignItems: "center",
    justifyContent: "center",
  },

  tileLabel: {
    width: "100%",
    minHeight: 30,
    fontSize: 12,
    lineHeight: 15,
    textAlign: "center",
  },
});
