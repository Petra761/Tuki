import { StyleSheet, Text as RNText, type TextProps } from "react-native";
import { colors, fontFamily, fontSize } from "@/theme";

type Variant = "big" | "title" | "heading" | "body" | "medium" | "bold" | "label";
type Props = TextProps & { variant?: Variant; color?: string };

export function Text({ variant = "body", color, style, ...rest }: Props) {
  return <RNText {...rest} style={[styles[variant], color ? { color } : null, style]} />;
}

const styles = StyleSheet.create({
  big: { fontFamily: fontFamily.title, fontSize: fontSize.xxl, color: colors.text, letterSpacing: -0.5 },
  title: { fontFamily: fontFamily.title, fontSize: fontSize.xl, color: colors.text, letterSpacing: -0.3 },
  heading: { fontFamily: fontFamily.title, fontSize: fontSize.lg, color: colors.text },
  body: { fontFamily: fontFamily.body, fontSize: fontSize.md, color: colors.text },
  medium: { fontFamily: fontFamily.bodyMedium, fontSize: fontSize.md, color: colors.text },
  bold: { fontFamily: fontFamily.bodyBold, fontSize: fontSize.md, color: colors.text },
  label: { fontFamily: fontFamily.body, fontSize: fontSize.sm, color: colors.textMuted },
});
