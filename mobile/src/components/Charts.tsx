import { useEffect, useRef } from "react";
import { Animated, StyleSheet, View } from "react-native";
import Svg, { Circle, Polyline } from "react-native-svg";
import { chartColors, colors, radius } from "@/theme";

type Slice = { key: string; value: number };

// El radio 15.9155 hace que la circunferencia mida 100, así los porcentajes sirven directo en strokeDasharray
const R = 15.9155;

export function Donut({ slices, size = 120 }: { slices: Slice[]; size?: number }) {
  const total = slices.reduce((sum, s) => sum + s.value, 0);
  let accumulated = 0;
  return (
    <Svg viewBox="0 0 36 36" width={size} height={size}>
      <Circle cx={18} cy={18} r={R} fill="none" stroke="rgba(241, 244, 238, 0.07)" strokeWidth={4.2} />
      {total > 0
        ? slices.map((slice, index) => {
            const pct = (slice.value / total) * 100;
            const circle = (
              <Circle
                key={slice.key}
                cx={18}
                cy={18}
                r={R}
                fill="none"
                stroke={chartColors[index % chartColors.length]}
                strokeWidth={4.2}
                strokeDasharray={`${Math.max(pct - 1.2, 0.1)} ${100 - pct + 1.2}`}
                strokeDashoffset={25 - accumulated}
                strokeLinecap="round"
              />
            );
            accumulated += pct;
            return circle;
          })
        : null}
    </Svg>
  );
}

export function Gauge({ ratio, size = 96, over }: { ratio: number; size?: number; over?: boolean }) {
  const pct = Math.min(Math.max(ratio, 0), 1) * 100;
  return (
    <Svg viewBox="0 0 36 36" width={size} height={size}>
      <Circle cx={18} cy={18} r={R} fill="none" stroke="rgba(241, 244, 238, 0.08)" strokeWidth={4} />
      <Circle
        cx={18}
        cy={18}
        r={R}
        fill="none"
        stroke={over ? colors.danger : colors.primary}
        strokeWidth={4}
        strokeLinecap="round"
        strokeDasharray={`${pct} ${100 - pct}`}
        strokeDashoffset={25}
      />
    </Svg>
  );
}

export function Sparkline({ values, width = 90, height = 30 }: { values: number[]; width?: number; height?: number }) {
  if (values.length < 2) return null;
  const min = Math.min(...values);
  const range = Math.max(...values) - min || 1;
  const points = values
    .map((v, i) => `${((i / (values.length - 1)) * 100).toFixed(1)},${(26 - ((v - min) / range) * 22).toFixed(1)}`)
    .join(" ");
  return (
    <Svg viewBox="0 0 100 30" width={width} height={height} preserveAspectRatio="none">
      <Polyline points={points} fill="none" stroke={colors.primary} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
    </Svg>
  );
}

export function Bar({ ratio, over }: { ratio: number; over?: boolean }) {
  const progress = useRef(new Animated.Value(0)).current;
  const target = Math.min(Math.max(ratio, 0), 1);
  useEffect(() => {
    // El ancho no admite el driver nativo
    Animated.timing(progress, { toValue: target, duration: 450, useNativeDriver: false }).start();
  }, [progress, target]);
  return (
    <View style={styles.track}>
      <Animated.View
        style={[
          styles.fill,
          { backgroundColor: over ? colors.danger : colors.primary, width: progress.interpolate({ inputRange: [0, 1], outputRange: ["0%", "100%"] }) },
        ]}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  track: { height: 8, borderRadius: radius.full, backgroundColor: "rgba(241, 244, 238, 0.08)", overflow: "hidden" },
  fill: { height: 8, borderRadius: radius.full },
});
