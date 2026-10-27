import { useMemo, useState } from "react";
import { Alert, ScrollView, StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { fetchAllTransactions, shortcutsApi, transactionsApi } from "@/api/resources";
import { errorMessage } from "@/api/client";
import { useAuth } from "@/auth/AuthContext";
import { Donut } from "@/components/Charts";
import { PressableScale } from "@/components/Button";
import { Card, Empty, ErrorNote, FadeIn, Loading, Screen, SectionTitle, TabHeader } from "@/components/Layout";
import { Text } from "@/components/Text";
import { TransactionRow } from "@/components/TransactionRow";
import { useCatalog } from "@/hooks/useCatalog";
import { useResource } from "@/hooks/useResource";
import { currentMonth, monthName, monthRange, todayISO } from "@/lib/dates";
import { balancesFrom, expenseByCategory, monthTotals } from "@/lib/ledger";
import { formatBs } from "@/lib/money";
import { chartColors, colors, radius, spacing } from "@/theme";

export default function HomeScreen() {
  const router = useRouter();
  const { session } = useAuth();
  const catalog = useCatalog();
  const month = currentMonth();
  const range = monthRange(month);
  const ledger = useResource(() => fetchAllTransactions());
  const shortcuts = useResource(async () => (await shortcutsApi.list()).filter((s) => !s.isArchived).sort((a, b) => a.sortOrder - b.sortOrder));
  const [notice, setNotice] = useState<{ id: string; text: string } | null>(null);

  const all = ledger.data ?? [];
  const monthTx = useMemo(() => all.filter((t) => t.occurredOn >= range.from && t.occurredOn <= range.to), [all, range.from, range.to]);
  const balances = useMemo(() => balancesFrom(all), [all]);
  const totals = useMemo(() => monthTotals(monthTx), [monthTx]);
  const byCategory = useMemo(() => expenseByCategory(monthTx, catalog.categoryById), [monthTx, catalog.categoryById]);
  const recent = useMemo(
    () => [...all].sort((a, b) => (a.occurredOn === b.occurredOn ? b.createdAt.localeCompare(a.createdAt) : b.occurredOn.localeCompare(a.occurredOn))).slice(0, 3),
    [all],
  );

  const accounts = catalog.data?.accounts ?? [];
  const isPerson = (accountTypeId: string) => catalog.typeById.get(accountTypeId)?.isPerson ?? false;
  let total = 0;
  let owedToMe = 0;
  let iOwe = 0;
  for (const account of accounts) {
    const balance = balances.get(account.id) ?? 0;
    if (!isPerson(account.accountTypeId)) total += balance;
    else if (balance > 0) owedToMe += balance;
    else iOwe += -balance;
  }

  const loading = (ledger.loading && !ledger.data) || (catalog.loading && !catalog.data);
  const error = ledger.error ?? catalog.error ?? shortcuts.error;

  async function registerShortcut(id: string) {
    const shortcut = shortcuts.data?.find((s) => s.id === id);
    if (!shortcut) return;
    try {
      const created = await transactionsApi.create({
        description: shortcut.name,
        occurredOn: todayISO(),
        lines: [{ accountId: shortcut.accountId, categoryId: shortcut.categoryId, amountCents: -shortcut.amountCents }],
        tagIds: [],
      });
      setNotice({ id: created.id, text: `${shortcut.name} registrado: ${formatBs(shortcut.amountCents)}` });
      void ledger.reload();
    } catch (e) {
      Alert.alert("No se pudo registrar", errorMessage(e));
    }
  }

  function reloadAll() {
    void catalog.reload();
    void ledger.reload();
    void shortcuts.reload();
  }

  return (
    <Screen tab refreshing={ledger.refreshing} onRefresh={() => { ledger.refresh(); void catalog.reload(); void shortcuts.reload(); }}>
      <TabHeader title={`Hola, ${session?.user.displayName.split(" ")[0] ?? ""}`} />

      {error ? <ErrorNote message={error} onRetry={reloadAll} /> : null}
      {loading ? <Loading /> : null}

      {!loading && !error ? (
        <>
          <FadeIn>
            <View>
              <Text variant="label">Saldo total</Text>
              <Text variant="big">{formatBs(total)}</Text>
              {owedToMe > 0 || iOwe > 0 ? (
                <Text variant="label">
                  {owedToMe > 0 ? `Te deben ${formatBs(owedToMe)}` : ""}
                  {owedToMe > 0 && iOwe > 0 ? " · " : ""}
                  {iOwe > 0 ? `Debes ${formatBs(iOwe)}` : ""}
                </Text>
              ) : null}
            </View>
          </FadeIn>

          <FadeIn delay={70}>
            <View style={styles.duo}>
              <Card style={styles.flex}>
                <Text variant="label">Ingresos de {monthName(month.month)}</Text>
                <Text variant="bold" color={colors.primary}>
                  {formatBs(totals.income, { sign: true })}
                </Text>
              </Card>
              <Card style={styles.flex}>
                <Text variant="label">Gastos de {monthName(month.month)}</Text>
                <Text variant="bold" color={colors.danger}>
                  {formatBs(-totals.expense)}
                </Text>
              </Card>
            </View>
          </FadeIn>

          {shortcuts.data && shortcuts.data.length > 0 ? (
            <FadeIn delay={140}>
              <SectionTitle>Atajos</SectionTitle>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.shortcuts}>
                {shortcuts.data.map((s) => (
                  <PressableScale key={s.id} onPress={() => void registerShortcut(s.id)} style={styles.shortcut}>
                    <Text variant="medium">{s.name}</Text>
                    <Text variant="label">{formatBs(s.amountCents, { hideCents: true })}</Text>
                  </PressableScale>
                ))}
              </ScrollView>
              {notice ? (
                <View style={styles.notice}>
                  <Text variant="label" color={colors.primary} style={styles.flex}>
                    {notice.text}
                  </Text>
                  <PressableScale onPress={() => router.push({ pathname: "/transaction-form", params: { id: notice.id } })}>
                    <Text variant="medium" color={colors.primary}>
                      Editar
                    </Text>
                  </PressableScale>
                </View>
              ) : null}
            </FadeIn>
          ) : null}

          <FadeIn delay={210}>
            <Card style={styles.donutCard}>
              {byCategory.length > 0 ? (
                <>
                  <Donut slices={byCategory.slice(0, 6).map((c) => ({ key: c.categoryId, value: c.amount }))} size={110} />
                  <View style={styles.legend}>
                    {byCategory.slice(0, 6).map((c, index) => (
                      <View key={c.categoryId} style={styles.legendRow}>
                        <View style={[styles.dot, { backgroundColor: chartColors[index % chartColors.length] }]} />
                        <Text variant="label" numberOfLines={1} style={styles.flex}>
                          {c.name}
                        </Text>
                        <Text variant="label" color={colors.text}>
                          {formatBs(c.amount, { hideCents: true })}
                        </Text>
                      </View>
                    ))}
                  </View>
                </>
              ) : (
                <Text variant="label">Cuando registres gastos este mes verás aquí en qué categorías se van.</Text>
              )}
            </Card>
          </FadeIn>

          <SectionTitle>Últimos movimientos</SectionTitle>
          {recent.length === 0 ? (
            <Empty
              icon="receipt-outline"
              title="Todavía no hay movimientos"
              text="Registra un gasto o un ingreso para empezar a ver tu resumen."
              actionLabel="Registrar movimiento"
              onAction={() => router.push("/transaction-form")}
            />
          ) : (
            recent.map((t) => (
              <TransactionRow
                key={t.id}
                transaction={t}
                accountById={catalog.accountById}
                categoryById={catalog.categoryById}
                onPress={() => router.push({ pathname: "/transaction-form", params: { id: t.id } })}
              />
            ))
          )}
        </>
      ) : null}
    </Screen>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  duo: { flexDirection: "row", gap: spacing.sm },
  shortcuts: { gap: spacing.sm, paddingVertical: spacing.sm },
  shortcut: {
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm + 2,
  },
  notice: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  donutCard: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  legend: { flex: 1, gap: spacing.xs + 2 },
  legendRow: { flexDirection: "row", alignItems: "center", gap: spacing.sm },
  dot: { width: 8, height: 8, borderRadius: 4 },
});
