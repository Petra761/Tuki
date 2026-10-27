import { useEffect, useRef, type ReactNode } from "react";
import {
  ActivityIndicator, Animated, RefreshControl, ScrollView, StyleSheet, View,
  type StyleProp, type ViewStyle,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { colors, radius, spacing } from "@/theme";
import { Button, PressableScale } from "./Button";
import { Text } from "./Text";

type ScreenProps = {
  children: ReactNode;
  // Las pantallas de las pestañas no tienen encabezado nativo, así que reservan el espacio de arriba
  tab?: boolean;
  refreshing?: boolean;
  onRefresh?: () => void;
};

export function Screen({ children, tab, refreshing, onRefresh }: ScreenProps) {
  const insets = useSafeAreaInsets();
  return (
    <ScrollView
      style={styles.screen}
      keyboardShouldPersistTaps="handled"
      contentContainerStyle={{
        padding: spacing.md,
        paddingTop: tab ? insets.top + spacing.md : spacing.md,
        paddingBottom: tab ? spacing.xl : insets.bottom + spacing.xl,
        gap: spacing.md,
      }}
      refreshControl={
        onRefresh ? (
          <RefreshControl
            refreshing={!!refreshing}
            onRefresh={onRefresh}
            tintColor={colors.primary}
            colors={[colors.primary]}
            progressBackgroundColor={colors.surface}
          />
        ) : undefined
      }
    >
      {children}
    </ScrollView>
  );
}

export function TabHeader({ title, right }: { title: string; right?: ReactNode }) {
  return (
    <View style={styles.tabHeader}>
      <Text variant="title">{title}</Text>
      {right}
    </View>
  );
}

export function Card({ children, style }: { children: ReactNode; style?: StyleProp<ViewStyle> }) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function SectionTitle({ children }: { children: ReactNode }) {
  return (
    <Text variant="label" style={styles.sectionTitle}>
      {children}
    </Text>
  );
}

type MenuRowProps = {
  icon: keyof typeof Ionicons.glyphMap;
  label: string;
  hint?: string;
  onPress?: () => void;
  danger?: boolean;
};

export function MenuRow({ icon, label, hint, onPress, danger }: MenuRowProps) {
  const color = danger ? colors.danger : colors.text;
  return (
    <PressableScale onPress={onPress} disabled={!onPress} style={styles.menuRow}>
      <View style={styles.menuIcon}>
        <Ionicons name={icon} size={20} color={danger ? colors.danger : colors.primary} />
      </View>
      <View style={styles.flex}>
        <Text variant="medium" color={color}>
          {label}
        </Text>
        {hint ? <Text variant="label">{hint}</Text> : null}
      </View>
      {danger ? null : <Ionicons name="chevron-forward" size={18} color={colors.textDim} />}
    </PressableScale>
  );
}

type EmptyProps = {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  text: string;
  actionLabel?: string;
  onAction?: () => void;
};

// Una pantalla vacía explica qué falta y ofrece el siguiente paso
export function Empty({ icon, title, text, actionLabel, onAction }: EmptyProps) {
  return (
    <View style={styles.empty}>
      <Ionicons name={icon} size={36} color={colors.textDim} />
      <Text variant="heading" style={styles.center}>
        {title}
      </Text>
      <Text variant="label" style={styles.center}>
        {text}
      </Text>
      {actionLabel && onAction ? (
        <View style={styles.emptyAction}>
          <Button label={actionLabel} onPress={onAction} />
        </View>
      ) : null}
    </View>
  );
}

export function ErrorNote({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <View style={styles.error}>
      <Text variant="medium" color={colors.danger}>
        {message}
      </Text>
      {onRetry ? <Button label="Reintentar" variant="secondary" onPress={onRetry} /> : null}
    </View>
  );
}

export function Loading() {
  return (
    <View style={styles.loading}>
      <ActivityIndicator color={colors.primary} />
    </View>
  );
}

// Entrada escalonada: se usa una sola vez, en la pantalla de inicio
export function FadeIn({ children, delay = 0 }: { children: ReactNode; delay?: number }) {
  const progress = useRef(new Animated.Value(0)).current;
  useEffect(() => {
    Animated.timing(progress, { toValue: 1, duration: 380, delay, useNativeDriver: true }).start();
  }, [progress, delay]);
  return (
    <Animated.View
      style={{
        opacity: progress,
        transform: [{ translateY: progress.interpolate({ inputRange: [0, 1], outputRange: [14, 0] }) }],
      }}
    >
      {children}
    </Animated.View>
  );
}

export function MonthSwitcher({ label, onPrev, onNext }: { label: string; onPrev: () => void; onNext: () => void }) {
  return (
    <View style={styles.monthSwitcher}>
      <PressableScale onPress={onPrev} accessibilityLabel="Mes anterior" style={styles.monthButton}>
        <Ionicons name="chevron-back" size={18} color={colors.text} />
      </PressableScale>
      <Text variant="medium" style={styles.monthLabel}>
        {label}
      </Text>
      <PressableScale onPress={onNext} accessibilityLabel="Mes siguiente" style={styles.monthButton}>
        <Ionicons name="chevron-forward" size={18} color={colors.text} />
      </PressableScale>
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  center: { textAlign: "center" },
  screen: { flex: 1, backgroundColor: colors.background },
  tabHeader: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", minHeight: 44 },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    gap: spacing.sm,
  },
  sectionTitle: { marginTop: spacing.sm },
  menuRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm + 4,
  },
  menuIcon: {
    width: 36,
    height: 36,
    borderRadius: radius.sm + 2,
    backgroundColor: colors.surfaceAlt,
    alignItems: "center",
    justifyContent: "center",
  },
  empty: { alignItems: "center", gap: spacing.sm, paddingVertical: spacing.xl, paddingHorizontal: spacing.lg },
  emptyAction: { alignSelf: "stretch", marginTop: spacing.sm },
  error: { gap: spacing.sm, padding: spacing.md, borderRadius: radius.md, borderWidth: 1, borderColor: colors.danger },
  loading: { paddingVertical: spacing.xl, alignItems: "center" },
  monthSwitcher: { flexDirection: "row", alignItems: "center", justifyContent: "space-between" },
  monthButton: {
    width: 36,
    height: 36,
    borderRadius: radius.full,
    backgroundColor: colors.surface,
    alignItems: "center",
    justifyContent: "center",
  },
  monthLabel: { textTransform: "capitalize" },
});
