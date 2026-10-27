import { useMemo, useState } from "react";
import { StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { errorMessage } from "@/api/client";
import { fetchAllTransactions, tagsApi } from "@/api/resources";
import type { Tag } from "@/api/types";
import { Bar } from "@/components/Charts";
import { Button, PressableScale } from "@/components/Button";
import { Field } from "@/components/Inputs";
import { Empty, ErrorNote, Loading, Screen, SectionTitle } from "@/components/Layout";
import { Sheet } from "@/components/Sheet";
import { Text } from "@/components/Text";
import { TransactionRow } from "@/components/TransactionRow";
import { useCatalog } from "@/hooks/useCatalog";
import { useFormState } from "@/hooks/useFormState";
import { useResource } from "@/hooks/useResource";
import { confirmDestructive } from "@/lib/confirm";
import { currentMonth, monthName, monthRange } from "@/lib/dates";
import { formatBs } from "@/lib/money";
import { colors, radius, spacing } from "@/theme";

export default function TagsScreen() {
  const router = useRouter();
  const catalog = useCatalog();
  const month = currentMonth();
  const range = monthRange(month);
  const ledger = useResource(() => fetchAllTransactions(range), [range.from, range.to]);
  const [target, setTarget] = useState<Tag | "new" | null>(null);
  const [selected, setSelected] = useState<string | null>(null);

  const tags = catalog.data?.tags ?? [];

  // Cuántos movimientos tiene cada marcador este mes y cuánto suman (gastos e ingresos en valor absoluto)
  const usage = useMemo(() => {
    const map = new Map<string, { count: number; total: number }>();
    for (const t of ledger.data ?? []) {
      if (t.type === "Transfer" || t.type === "Opening") continue;
      const amount = Math.abs(t.lines.reduce((sum, l) => sum + l.amountCents, 0));
      for (const tagId of t.tagIds) {
        const current = map.get(tagId) ?? { count: 0, total: 0 };
        map.set(tagId, { count: current.count + 1, total: current.total + amount });
      }
    }
    return map;
  }, [ledger.data]);

  const biggest = Math.max(1, ...[...usage.values()].map((u) => u.total));
  const selectedTransactions = (ledger.data ?? []).filter((t) => selected && t.tagIds.includes(selected));
  const selectedTag = tags.find((t) => t.id === selected);
  const error = catalog.error ?? ledger.error;

  return (
    <>
      <Screen refreshing={catalog.refreshing} onRefresh={() => { catalog.refresh(); void ledger.reload(); }}>
        <Button label="Nuevo marcador" icon="add" onPress={() => setTarget("new")} />
        {error ? <ErrorNote message={error} onRetry={() => { void catalog.reload(); void ledger.reload(); }} /> : null}
        {catalog.loading && !catalog.data ? <Loading /> : null}
        {catalog.data && tags.length === 0 ? (
          <Empty
            icon="pricetag-outline"
            title="Sin marcadores"
            text="Un marcador es una etiqueta como #universidad o #viaje que reúne movimientos de distintas categorías."
          />
        ) : null}

        {tags.map((tag) => {
          const u = usage.get(tag.id);
          const active = selected === tag.id;
          return (
            <View key={tag.id} style={[styles.row, active ? styles.rowActive : null]}>
              <PressableScale onPress={() => setSelected(active ? null : tag.id)}>
                <View style={styles.rowTop}>
                  <Text variant="medium">#{tag.name}</Text>
                  <Text variant="label">{u ? `${u.count} movimientos` : "Sin movimientos en " + monthName(month.month)}</Text>
                </View>
                {u ? (
                  <View style={styles.bar}>
                    <Bar ratio={u.total / biggest} />
                    <Text variant="label" color={colors.text}>
                      {formatBs(u.total)}
                    </Text>
                  </View>
                ) : null}
              </PressableScale>
              {active ? <Button label="Editar marcador" variant="secondary" onPress={() => setTarget(tag)} /> : null}
            </View>
          );
        })}

        {selectedTag && selectedTransactions.length > 0 ? (
          <>
            <SectionTitle>Con #{selectedTag.name} en {monthName(month.month)}</SectionTitle>
            {selectedTransactions.map((t) => (
              <TransactionRow
                key={t.id}
                transaction={t}
                accountById={catalog.accountById}
                categoryById={catalog.categoryById}
                onPress={() => router.push({ pathname: "/transaction-form", params: { id: t.id } })}
              />
            ))}
          </>
        ) : null}
      </Screen>

      <TagSheet
        target={target}
        onClose={() => setTarget(null)}
        onSaved={() => {
          setTarget(null);
          setSelected(null);
          void catalog.reload();
        }}
      />
    </>
  );
}

function TagSheet({ target, onClose, onSaved }: { target: Tag | "new" | null; onClose: () => void; onSaved: () => void }) {
  const tag = target && target !== "new" ? target : null;
  const [name, setName] = useFormState(target, () => tag?.name ?? "");
  const [error, setError] = useFormState<string | null>(target, () => null);
  const [saving, setSaving] = useState(false);

  async function save() {
    // El # se escribe solo al mostrarlo; si la persona lo teclea, se quita
    const clean = name.trim().replace(/^#+/, "");
    if (!clean) return setError("Escribe un nombre.");
    setSaving(true);
    setError(null);
    try {
      if (tag) await tagsApi.update(tag.id, { name: clean, isArchived: false });
      else await tagsApi.create({ name: clean });
      onSaved();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  function archive() {
    if (!tag) return;
    confirmDestructive("Archivar marcador", `«#${tag.name}» dejará de ofrecerse al registrar movimientos.`, "Archivar", async () => {
      try {
        await tagsApi.remove(tag.id);
        onSaved();
      } catch (e) {
        setError(errorMessage(e));
      }
    });
  }

  return (
    <Sheet visible={target !== null} title={tag ? "Editar marcador" : "Nuevo marcador"} onClose={onClose}>
      <Field label="Nombre" value={name} onChangeText={setName} autoCapitalize="none" placeholder="universidad" />
      {error ? <Text variant="medium" color={colors.danger}>{error}</Text> : null}
      <Button label="Guardar" onPress={save} loading={saving} />
      {tag ? <Button label="Archivar marcador" variant="danger" onPress={archive} /> : null}
    </Sheet>
  );
}

const styles = StyleSheet.create({
  row: {
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
  rowActive: { borderColor: colors.primary },
  rowTop: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", marginBottom: spacing.sm },
  bar: { gap: spacing.xs },
});
