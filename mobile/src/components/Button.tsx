import { useRef, type ReactNode } from "react";
import { ActivityIndicator, Animated, Pressable, StyleSheet, View, type PressableProps, type StyleProp, type ViewStyle } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { colors, radius, spacing } from "@/theme";
import { Text } from "./Text";

// La escala al tocar confirma la acción; es el único movimiento que responde directamente al dedo
export function PressableScale({
  style,
  containerStyle,
  children,
  onPressIn,
  onPressOut,
  ...rest
}: PressableProps & { style?: StyleProp<ViewStyle>; containerStyle?: StyleProp<ViewStyle>; children?: ReactNode }) {
  const scale = useRef(new Animated.Value(1)).current;
  const to = (value: number) =>
    Animated.spring(scale, { toValue: value, useNativeDriver: true, speed: 40, bounciness: 0 }).start();

  return (
    <Pressable
      {...rest}
      style={containerStyle}
      onPressIn={(e) => {
        to(0.97);
        onPressIn?.(e);
      }}
      onPressOut={(e) => {
        to(1);
        onPressOut?.(e);
      }}
    >
      <Animated.View style={[style, { transform: [{ scale }] }]}>{children}</Animated.View>
    </Pressable>
  );
}

type Variant = "primary" | "secondary" | "danger" | "ghost";

type ButtonProps = {
  label: string;
  onPress: () => void;
  variant?: Variant;
  loading?: boolean;
  disabled?: boolean;
  icon?: keyof typeof Ionicons.glyphMap;
};

export function Button({ label, onPress, variant = "primary", loading, disabled, icon }: ButtonProps) {
  const palette = variants[variant];
  const blocked = disabled || loading;
  return (
    <PressableScale
      onPress={onPress}
      disabled={blocked}
      style={[styles.base, { backgroundColor: palette.bg, borderColor: palette.border, opacity: blocked ? 0.5 : 1 }]}
    >
      {loading ? (
        <ActivityIndicator color={palette.text} />
      ) : (
        <View style={styles.content}>
          {icon ? <Ionicons name={icon} size={18} color={palette.text} /> : null}
          <Text variant="bold" color={palette.text}>
            {label}
          </Text>
        </View>
      )}
    </PressableScale>
  );
}

export function IconButton({ icon, onPress, label }: { icon: keyof typeof Ionicons.glyphMap; onPress: () => void; label: string }) {
  return (
    <PressableScale onPress={onPress} accessibilityLabel={label} style={styles.iconButton}>
      <Ionicons name={icon} size={22} color={colors.onPrimary} />
    </PressableScale>
  );
}

const variants: Record<Variant, { bg: string; text: string; border: string }> = {
  primary: { bg: colors.primary, text: colors.onPrimary, border: colors.primary },
  secondary: { bg: colors.surface, text: colors.text, border: colors.border },
  danger: { bg: "transparent", text: colors.danger, border: colors.danger },
  ghost: { bg: "transparent", text: colors.textMuted, border: "transparent" },
};

const styles = StyleSheet.create({
  base: {
    minHeight: 52,
    borderRadius: radius.md,
    borderWidth: 1,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.md,
  },
  content: { flexDirection: "row", alignItems: "center", gap: spacing.sm },
  iconButton: {
    width: 40,
    height: 40,
    borderRadius: radius.full,
    backgroundColor: colors.primary,
    alignItems: "center",
    justifyContent: "center",
  },
});
