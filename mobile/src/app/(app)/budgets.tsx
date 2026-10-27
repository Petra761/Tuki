import { useMemo, useState } from "react";
import { StyleSheet, View } from "react-native";
import { errorMessage } from "@/api/client";
import { budgetsApi, fetchAllTransactions } from "@/api/resources";
import type { Budget } from "@/api/types";
import { Bar, Gauge } from "@/components/Charts";
import { Button, PressableScale } from "@/components/Button";
import { Chips, Field } from "@/components/Inputs";
import { Card, Empty, ErrorNote, Loading, Screen } from "@/components/Layout";
import { Sheet } from "@/components/Sheet";
import { Text } from "@/components/Text";
import { useCatalog } from "@/hooks/useCatalog";
import { useFormState } from "@/hooks/useFormState";
import { useResource } from "@/hooks/useResource";
import { confirmDestructive } from "@/lib/confirm";
import { currentMonth, monthName, monthRange } from "@/lib/dates";
import { monthTotals, spentInCategory } from "@/lib/ledger";
import { centsToInput, formatBs, parseAmountToCents } from "@/lib/money";
import { colors, radius, spacing } from "@/theme";

const TOTAL = "total";

export default function BudgetsScreen() {
  const catalog = useCatalog();
  const month = currentMonth();
  const range = monthRange(month);
  const budgets = useResource(async () => (await budgetsApi.list()).filter((b) => !b.isArchived));
  const ledger = useResource(() => fetchAllTransactions(range), [range.from, range.to]);
  const [target, setTarget] = useState<Budget | "new" | null>(null);

  const rows = useMemo(() => {
    const transactions = ledger.data ?? [];
    return (budgets.data ?? []).map((budget) => {
      const spent = budget.categoryId ? spentInCategory(transactions, budget.categoryId, catalog.categoryById) : monthTotals(transactions).expense;
      return { budget, spent, name: budget.categoryId ? (catalog.categoryById.get(budget.categoryId)?.name ?? "Categoría") : "Gasto total" };
    });
  }, [budgets.data, ledger.data, catalog.categoryById]);

  // Si hay un presupuesto del gasto total, ese es el límite general; si no, se suman los de categorías
  const totalRow = rows.find((r) => !r.budget.categoryId);
  const limit = totalRow ? totalRow.budget.amountCents : rows.reduce((sum, r) => sum + r.budget.amountCents, 0);
  const spent = totalRow ? totalRow.spent : rows.reduce((sum, r) => sum + r.spent, 0);
  const error = budgets.error ?? ledger.error ?? catalog.error;
  const loading = (budgets.loading && !budgets.data) || (ledger.loading && !ledger.data);

  function reloadAll() {
    void budgets.reload();
    void ledger.reload();
    void catalog.reload();
  }

  return (
    <>
      <Screen refreshing={budgets.refreshing} onRefresh={() => { budgets.refresh(); void ledger.reload(); void catalog.reload(); }}>
        <Button label="Nuevo presupuesto" icon="add" onPress={() => setTarget("new")} />
        {error ? <ErrorNote message={error} onRetry={reloadAll} /> : null}
        {loading ? <Loading /> : null}

        {!loading && rows.length === 0 && !error ? (
          <Empty
            icon="pie-chart-outline"
            title="Sin presupuestos"
            text="Pon un límite mensual a una categoría o a tu gasto total y Tuki te muestra cuánto llevas."
          />
        ) : null}

        {rows.length > 0 ? (
          <>
            <Card style={styles.gaugeCard}>
              <View>
                <Gauge ratio={limit ? spent / limit : 0} over={spent > limit} />
                <View style={styles.gaugeLabel}>
                  <Text variant="heading">{limit ? Math.round((spent / limit) * 100) : 0}%</Text>
                </View>
              </View>
              <View style={styles.flex}>
                <Text variant="label">Presupuesto de {monthName(month.month)}</Text>
                <Text variant="bold">
                  {formatBs(spent, { hideCents: true })} de {formatBs(limit, { hideCents: true })}
                </Text>
                <Text variant="label" color={spent > limit ? colors.danger : colors.primary}>
                  {spent > limit ? `Superado por ${formatBs(spent - limit)}` : `Te queda ${formatBs(limit - spent)}`}
                </Text>
              </View>
            </Card>

            {rows.map(({ budget, spent: rowSpent, name }) => {
              const over = rowSpent > budget.amountCents;
              return (
                <PressableScale key={budget.id} onPress={() => setTarget(budget)} style={styles.row}>
                  <View style={styles.rowTop}>
                    <Text variant="medium">{name}</Text>
                    <Text variant="label" color={over ? colors.danger : colors.textMuted}>
                      {over
                        ? `Superado por ${formatBs(rowSpent - budget.amountCents)}`
                        : `${formatBs(rowSpent, { hideCents: true })} de ${formatBs(budget.amountCents, { hideCents: true })}`}
                    </Text>
                  </View>
                  <Bar ratio={budget.amountCents ? rowSpent / budget.amountCents : 0} over={over} />
                </PressableScale>
              );
            })}
          </>
        ) : null}
      </Screen>

      <BudgetSheet
        target={target}
        categories={(catalog.data?.categories ?? []).filter((c) => c.kind === "Expense")}
        onClose={() => setTarget(null)}
        onSaved={() => {
          setTarget(null);
          reloadAll();
        }}
      />
    </>
  );
}

type SheetProps = {
  target: Budget | "new" | null;
  categories: { id: string; name: string }[];
  onClose: () => void;
  onSaved: () => void;
};

function BudgetSheet({ target, categories, onClose, onSaved }: SheetProps) {
  const budget = target && target !== "new" ? target : null;
  const [form, setForm] = useFormState(target, () => ({
    category: budget ? (budget.categoryId ?? TOTAL) : "",
    amount: budget ? centsToInput(budget.amountCents) : "",
  }));
  const [error, setError] = useFormState<string | null>(target, () => null);
  const [saving, setSaving] = useState(false);

  async function save() {
    const cents = parseAmountToCents(form.amount);
    if (!form.category) return setError("Elige una categoría o el gasto total.");
    if (!cents) return setError("Escribe un monto mayor a cero.");
    setSaving(true);
    setError(null);
    try {
      const body = { categoryId: form.category === TOTAL ? null : form.category, amountCents: cents, period: "Monthly" as const };
      if (budget) await budgetsApi.update(budget.id, { ...body, isArchived: false });
      else await budgetsApi.create(body);
      onSaved();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  function archive() {
    if (!budget) return;
    confirmDestructive("Archivar presupuesto", "Dejará de contarse en tu resumen del mes.", "Archivar", async () => {
      try {
        await budgetsApi.remove(budget.id);
        onSaved();
      } catch (e) {
        setError(errorMessage(e));
      }
    });
  }

  return (
    <Sheet visible={target !== null} title={budget ? "Editar presupuesto" : "Nuevo presupuesto"} onClose={onClose}>
      <Chips
        label="¿Qué quieres limitar?"
        options={[{ value: TOTAL, label: "Gasto total" }, ...categories.map((c) => ({ value: c.id, label: c.name }))]}
        selected={[form.category]}
        onSelect={(category) => setForm({ ...form, category })}
      />
      <Field label="Límite mensual (Bs)" value={form.amount} onChangeText={(amount) => setForm({ ...form, amount })} keyboardType="decimal-pad" placeholder="500" />
      {error ? <Text variant="medium" color={colors.danger}>{error}</Text> : null}
      <Button label="Guardar" onPress={save} loading={saving} />
      {budget ? <Button label="Archivar presupuesto" variant="danger" onPress={archive} /> : null}
    </Sheet>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  gaugeCard: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  gaugeLabel: { ...StyleSheet.absoluteFill, alignItems: "center", justifyContent: "center" },
  row: {
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
  rowTop: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", gap: spacing.sm },
});
