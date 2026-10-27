import { useState } from "react";
import { StyleSheet, View } from "react-native";
import { errorMessage } from "@/api/client";
import { shortcutsApi } from "@/api/resources";
import type { Shortcut } from "@/api/types";
import { Button, PressableScale } from "@/components/Button";
import { Chips, Field } from "@/components/Inputs";
import { Empty, ErrorNote, Loading, Screen } from "@/components/Layout";
import { Sheet } from "@/components/Sheet";
import { Text } from "@/components/Text";
import { useCatalog } from "@/hooks/useCatalog";
import { useFormState } from "@/hooks/useFormState";
import { useResource } from "@/hooks/useResource";
import { confirmDestructive } from "@/lib/confirm";
import { categoryLabel } from "@/lib/ledger";
import { centsToInput, formatBs, parseAmountToCents } from "@/lib/money";
import { colors, radius, spacing } from "@/theme";

export default function ShortcutsScreen() {
  const catalog = useCatalog();
  const list = useResource(async () => (await shortcutsApi.list()).filter((s) => !s.isArchived).sort((a, b) => a.sortOrder - b.sortOrder));
  const [target, setTarget] = useState<Shortcut | "new" | null>(null);
  const error = list.error ?? catalog.error;

  return (
    <>
      <Screen refreshing={list.refreshing} onRefresh={() => { list.refresh(); void catalog.reload(); }}>
        <Button label="Nuevo atajo" icon="add" onPress={() => setTarget("new")} />
        {error ? <ErrorNote message={error} onRetry={() => { void list.reload(); void catalog.reload(); }} /> : null}
        {list.loading && !list.data ? <Loading /> : null}
        {list.data?.length === 0 ? (
          <Empty
            icon="flash-outline"
            title="Sin atajos"
            text="Un atajo registra un gasto frecuente con un toque desde Inicio, como el almuerzo o el pasaje."
          />
        ) : null}
        {list.data?.map((shortcut) => (
          <PressableScale key={shortcut.id} onPress={() => setTarget(shortcut)} style={styles.row}>
            <View style={styles.flex}>
              <Text variant="medium">{shortcut.name}</Text>
              <Text variant="label">
                {categoryLabel(catalog.categoryById.get(shortcut.categoryId), catalog.categoryById)}, {catalog.accountById.get(shortcut.accountId)?.name ?? "?"}
              </Text>
            </View>
            <Text variant="bold" color={colors.danger}>
              {formatBs(-shortcut.amountCents)}
            </Text>
          </PressableScale>
        ))}
      </Screen>

      <ShortcutSheet
        target={target}
        nextOrder={(list.data?.reduce((max, s) => Math.max(max, s.sortOrder), 0) ?? 0) + 1}
        accounts={catalog.data?.accounts ?? []}
        categories={catalog.data?.categories ?? []}
        categoryById={catalog.categoryById}
        onClose={() => setTarget(null)}
        onSaved={() => {
          setTarget(null);
          void list.reload();
        }}
      />
    </>
  );
}

type SheetProps = {
  target: Shortcut | "new" | null;
  nextOrder: number;
  accounts: { id: string; name: string }[];
  categories: { id: string; parentId: string | null; kind: string; name: string }[];
  categoryById: Parameters<typeof categoryLabel>[1];
  onClose: () => void;
  onSaved: () => void;
};

function ShortcutSheet({ target, nextOrder, accounts, categories, categoryById, onClose, onSaved }: SheetProps) {
  const shortcut = target && target !== "new" ? target : null;
  const [form, setForm] = useFormState(target, () => ({
    name: shortcut?.name ?? "",
    amount: shortcut ? centsToInput(shortcut.amountCents) : "",
    categoryId: shortcut?.categoryId ?? "",
    accountId: shortcut?.accountId ?? "",
  }));
  const [error, setError] = useFormState<string | null>(target, () => null);
  const [saving, setSaving] = useState(false);

  // Se registra en la categoría más específica, así que las que tienen subcategorías no se ofrecen
  const parents = new Set(categories.map((c) => c.parentId).filter(Boolean));
  const categoryOptions = categories
    .filter((c) => c.kind === "Expense" && !parents.has(c.id))
    .map((c) => ({ value: c.id, label: categoryLabel(categoryById.get(c.id), categoryById) }));

  async function save() {
    const cents = parseAmountToCents(form.amount);
    if (!form.name.trim()) return setError("Escribe un nombre.");
    if (!cents) return setError("Escribe un monto mayor a cero.");
    if (!form.categoryId) return setError("Elige una categoría.");
    if (!form.accountId) return setError("Elige una cuenta.");

    setSaving(true);
    setError(null);
    try {
      const body = {
        name: form.name.trim(),
        amountCents: cents,
        categoryId: form.categoryId,
        accountId: form.accountId,
        sortOrder: shortcut?.sortOrder ?? nextOrder,
      };
      if (shortcut) await shortcutsApi.update(shortcut.id, { ...body, isArchived: false });
      else await shortcutsApi.create(body);
      onSaved();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  function archive() {
    if (!shortcut) return;
    confirmDestructive("Archivar atajo", `«${shortcut.name}» dejará de aparecer en Inicio.`, "Archivar", async () => {
      try {
        await shortcutsApi.remove(shortcut.id);
        onSaved();
      } catch (e) {
        setError(errorMessage(e));
      }
    });
  }

  return (
    <Sheet visible={target !== null} title={shortcut ? "Editar atajo" : "Nuevo atajo"} onClose={onClose}>
      <Field label="Nombre" value={form.name} onChangeText={(name) => setForm({ ...form, name })} placeholder="Almuerzo" />
      <Field label="Monto (Bs)" value={form.amount} onChangeText={(amount) => setForm({ ...form, amount })} keyboardType="decimal-pad" placeholder="25" />
      <Chips
        label="Categoría"
        options={categoryOptions}
        selected={[form.categoryId]}
        onSelect={(categoryId) => setForm({ ...form, categoryId })}
        emptyText="Crea una categoría de gasto en Más › Categorías."
      />
      <Chips
        label="Cuenta de donde sale"
        options={accounts.map((a) => ({ value: a.id, label: a.name }))}
        selected={[form.accountId]}
        onSelect={(accountId) => setForm({ ...form, accountId })}
      />
      {error ? <Text variant="medium" color={colors.danger}>{error}</Text> : null}
      <Button label="Guardar" onPress={save} loading={saving} />
      {shortcut ? <Button label="Archivar atajo" variant="danger" onPress={archive} /> : null}
    </Sheet>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: radius.md,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
});
