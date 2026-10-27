import { StyleSheet, Switch, TextInput, View, type TextInputProps } from "react-native";
import { colors, fontFamily, fontSize, radius, spacing } from "@/theme";
import { PressableScale } from "./Button";
import { Text } from "./Text";

type FieldProps = TextInputProps & { label: string; error?: string | null; hint?: string };

export function Field({ label, error, hint, style, ...input }: FieldProps) {
  return (
    <View style={styles.field}>
      <Text variant="label">{label}</Text>
      <TextInput
        placeholderTextColor={colors.textDim}
        selectionColor={colors.primary}
        {...input}
        style={[styles.input, error ? { borderColor: colors.danger } : null, style]}
      />
      {error ? (
        <Text variant="label" color={colors.danger}>
          {error}
        </Text>
      ) : hint ? (
        <Text variant="label" color={colors.textDim}>
          {hint}
        </Text>
      ) : null}
    </View>
  );
}

export type ChipOption = { value: string; label: string };

type ChipsProps = {
  label?: string;
  options: ChipOption[];
  selected: string[];
  onSelect: (value: string) => void;
  emptyText?: string;
};

// Sirve para elegir uno o varios: quien la usa decide qué hacer en onSelect
export function Chips({ label, options, selected, onSelect, emptyText }: ChipsProps) {
  return (
    <View style={styles.field}>
      {label ? <Text variant="label">{label}</Text> : null}
      {options.length === 0 && emptyText ? (
        <Text variant="label" color={colors.textDim}>
          {emptyText}
        </Text>
      ) : null}
      <View style={styles.chips}>
        {options.map((option) => {
          const active = selected.includes(option.value);
          return (
            <PressableScale
              key={option.value}
              onPress={() => onSelect(option.value)}
              style={[styles.chip, active ? styles.chipActive : null]}
            >
              <Text variant="medium" style={styles.chipText} color={active ? colors.onPrimary : colors.text}>
                {option.label}
              </Text>
            </PressableScale>
          );
        })}
      </View>
    </View>
  );
}

export function Segmented({ options, value, onChange }: { options: ChipOption[]; value: string; onChange: (value: string) => void }) {
  return (
    <View style={styles.segmented}>
      {options.map((option) => {
        const active = option.value === value;
        return (
          <PressableScale key={option.value} onPress={() => onChange(option.value)} containerStyle={styles.flex} style={[styles.segment, active ? styles.segmentActive : null]}>
            <Text variant="bold" style={styles.chipText} color={active ? colors.onPrimary : colors.textMuted}>
              {option.label}
            </Text>
          </PressableScale>
        );
      })}
    </View>
  );
}

export function SwitchRow({ label, value, onChange }: { label: string; value: boolean; onChange: (value: boolean) => void }) {
  return (
    <View style={styles.switchRow}>
      <Text variant="medium">{label}</Text>
      <Switch
        value={value}
        onValueChange={onChange}
        trackColor={{ false: colors.surfaceAlt, true: colors.primary }}
        thumbColor={colors.text}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  field: { gap: spacing.xs + 2 },
  input: {
    minHeight: 52,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: spacing.md,
    color: colors.text,
    fontFamily: fontFamily.body,
    fontSize: fontSize.md,
  },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
  chip: {
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm + 2,
    borderRadius: radius.full,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
  },
  chipActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  chipText: { fontSize: fontSize.sm + 1 },
  segmented: {
    flexDirection: "row",
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    padding: 4,
    gap: 4,
    borderWidth: 1,
    borderColor: colors.border,
  },
  segment: { paddingVertical: spacing.sm + 2, borderRadius: radius.sm + 2, alignItems: "center" },
  segmentActive: { backgroundColor: colors.primary },
  switchRow: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", minHeight: 44 },
});
