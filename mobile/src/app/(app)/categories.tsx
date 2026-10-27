import { useState } from "react";
import { StyleSheet } from "react-native";
import { errorMessage } from "@/api/client";
import { categoriesApi } from "@/api/resources";
import type { Category, CategoryKind } from "@/api/types";
import { Button, PressableScale } from "@/components/Button";
import { Chips, Field } from "@/components/Inputs";
import { Empty, ErrorNote, Loading, Screen, SectionTitle } from "@/components/Layout";
import { Sheet } from "@/components/Sheet";
import { Text } from "@/components/Text";
import { useFormState } from "@/hooks/useFormState";
import { useResource } from "@/hooks/useResource";
import { confirmDestructive } from "@/lib/confirm";
import { colors, radius, spacing } from "@/theme";

const KIND_OPTIONS = [
  { value: "Expense", label: "Gasto" },
  { value: "Income", label: "Ingreso" },
];

export default function CategoriesScreen() {
  const list = useResource(async () => (await categoriesApi.list()).filter((c) => !c.isArchived));
  const [target, setTarget] = useState<Category | "new" | null>(null);
  const all = list.data ?? [];

  function renderGroup(kind: CategoryKind, title: string) {
    const roots = all.filter((c) => c.kind === kind && !c.parentId);
    if (roots.length === 0) return null;
    return (
      <>
        <SectionTitle>{title}</SectionTitle>
        {roots.map((root) => (
          <PressableScale key={root.id} onPress={() => setTarget(root)} style={styles.row}>
            <Text variant="medium">{root.name}</Text>
            {all
              .filter((c) => c.parentId === root.id)
              .map((child) => (
                <PressableScale key={child.id} onPress={() => setTarget(child)} style={styles.child}>
                  <Text variant="label">{child.name}</Text>
                </PressableScale>
              ))}
          </PressableScale>
        ))}
      </>
    );
  }

  return (
    <>
      <Screen refreshing={list.refreshing} onRefresh={list.refresh}>
        <Button label="Nueva categoría" icon="add" onPress={() => setTarget("new")} />
        {list.error ? <ErrorNote message={list.error} onRetry={list.reload} /> : null}
        {list.loading && !list.data ? <Loading /> : null}
        {list.data?.length === 0 ? (
          <Empty icon="apps-outline" title="Sin categorías" text="Las categorías te dicen en qué gastas o de dónde viene tu dinero." />
        ) : null}
        {renderGroup("Expense", "Gastos")}
        {renderGroup("Income", "Ingresos")}
      </Screen>

      <CategorySheet
        target={target}
        all={all}
        onClose={() => setTarget(null)}
        onSaved={() => {
          setTarget(null);
          void list.reload();
        }}
      />
    </>
  );
}

type SheetProps = { target: Category | "new" | null; all: Category[]; onClose: () => void; onSaved: () => void };

function CategorySheet({ target, all, onClose, onSaved }: SheetProps) {
  const category = target && target !== "new" ? target : null;
  const [form, setForm] = useFormState(target, () => ({
    name: category?.name ?? "",
    kind: (category?.kind ?? "Expense") as CategoryKind,
    parentId: category?.parentId ?? "",
  }));
  const [error, setError] = useFormState<string | null>(target, () => null);
  const [saving, setSaving] = useState(false);

  // Solo hay un nivel de subcategorías: una categoría con hijas no puede tener padre
  const hasChildren = category ? all.some((c) => c.parentId === category.id) : false;
  const parentOptions = [
    { value: "", label: "Ninguna" },
    ...all
      .filter((c) => !c.parentId && c.kind === form.kind && c.id !== category?.id)
      .map((c) => ({ value: c.id, label: c.name })),
  ];

  async function save() {
    if (!form.name.trim()) return setError("Escribe un nombre.");
    setSaving(true);
    setError(null);
    try {
      const body = { name: form.name.trim(), kind: form.kind, parentId: form.parentId || null };
      if (category) await categoriesApi.update(category.id, { ...body, isArchived: false });
      else await categoriesApi.create(body);
      onSaved();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  function archive() {
    if (!category) return;
    confirmDestructive("Archivar categoría", `«${category.name}» dejará de ofrecerse al registrar movimientos.`, "Archivar", async () => {
      try {
        await categoriesApi.remove(category.id);
        onSaved();
      } catch (e) {
        // El backend responde 409 si la categoría está en uso
        setError(errorMessage(e));
      }
    });
  }

  return (
    <Sheet visible={target !== null} title={category ? "Editar categoría" : "Nueva categoría"} onClose={onClose}>
      <Field label="Nombre" value={form.name} onChangeText={(name) => setForm({ ...form, name })} placeholder="Supermercado" />
      {!category ? (
        <Chips
          label="Tipo"
          options={KIND_OPTIONS}
          selected={[form.kind]}
          onSelect={(kind) => setForm({ ...form, kind: kind as CategoryKind, parentId: "" })}
        />
      ) : null}
      {!hasChildren ? (
        <Chips
          label="Categoría principal"
          options={parentOptions}
          selected={[form.parentId]}
          onSelect={(parentId) => setForm({ ...form, parentId })}
        />
      ) : null}
      {error ? <Text variant="medium" color={colors.danger}>{error}</Text> : null}
      <Button label="Guardar" onPress={save} loading={saving} />
      {category ? <Button label="Archivar categoría" variant="danger" onPress={archive} /> : null}
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
  child: { paddingLeft: spacing.md, paddingVertical: spacing.xs },
});
