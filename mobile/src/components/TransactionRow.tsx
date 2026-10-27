import { StyleSheet, View } from "react-native";
import type { Account, Category, Transaction } from "@/api/types";
import { categoryLabel, netAmount } from "@/lib/ledger";
import { formatBs } from "@/lib/money";
import { colors, radius, spacing } from "@/theme";
import { PressableScale } from "./Button";
import { Text } from "./Text";

type Props = {
  transaction: Transaction;
  accountById: Map<string, Account>;
  categoryById: Map<string, Category>;
  onPress?: () => void;
};

const TYPE_LABEL = { Expense: "Gasto", Income: "Ingreso", Transfer: "Traslado", Opening: "Saldo inicial" } as const;

export function TransactionRow({ transaction, accountById, categoryById, onPress }: Props) {
  const first = transaction.lines[0];
  const category = first?.categoryId ? categoryById.get(first.categoryId) : undefined;

  let subtitle: string;
  if (transaction.type === "Transfer") {
    const from = transaction.lines.find((l) => l.amountCents < 0);
    const to = transaction.lines.find((l) => l.amountCents > 0);
    subtitle = `${accountById.get(from?.accountId ?? "")?.name ?? "?"} → ${accountById.get(to?.accountId ?? "")?.name ?? "?"}`;
  } else {
    const account = accountById.get(first?.accountId ?? "")?.name ?? "";
    subtitle = category ? `${categoryLabel(category, categoryById)}, ${account}` : account;
  }

  const title = transaction.description || category?.name || TYPE_LABEL[transaction.type];
  const amount = netAmount(transaction);
  const income = transaction.type === "Income";
  const expense = transaction.type === "Expense";

  return (
    <PressableScale onPress={onPress} disabled={!onPress} style={styles.row}>
      <View style={[styles.icon, income ? styles.iconIncome : null]}>
        <Text variant="bold" color={income ? colors.onPrimary : colors.text}>
          {title.charAt(0).toUpperCase()}
        </Text>
      </View>
      <View style={styles.text}>
        <Text variant="medium" numberOfLines={1}>
          {title}
        </Text>
        <Text variant="label" numberOfLines={1}>
          {subtitle}
        </Text>
      </View>
      <Text variant="bold" color={income ? colors.primary : expense ? colors.danger : colors.textMuted}>
        {formatBs(amount, { sign: income })}
      </Text>
    </PressableScale>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.sm + 4,
  },
  icon: {
    width: 38,
    height: 38,
    borderRadius: radius.full,
    backgroundColor: colors.surfaceAlt,
    alignItems: "center",
    justifyContent: "center",
  },
  iconIncome: { backgroundColor: colors.primary },
  text: { flex: 1 },
});
