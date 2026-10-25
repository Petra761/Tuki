import { StyleSheet, Text, View } from "react-native";

import { colors, fontFamily, fontSize, spacing } from "@/constants/theme";

// Pantalla de prueba del tema: se elimina cuando existan las rutas de login y registro
export default function Index() {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Tuki</Text>
      <Text style={styles.text}>Sabe a dónde se va tu dinero</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: spacing.sm,
    backgroundColor: colors.background,
  },
  title: {
    color: colors.primary,
    fontFamily: fontFamily.title,
    fontSize: fontSize.xl,
  },
  text: {
    color: colors.textMuted,
    fontFamily: fontFamily.body,
    fontSize: fontSize.md,
  },
});
