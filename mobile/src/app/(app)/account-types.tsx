import { useState } from "react";
import { StyleSheet, View } from "react-native";
import { errorMessage } from "@/api/client";
import { accountTypesApi } from "@/api/resources";
import type { AccountType } from "@/api/types";
import { Button, PressableScale } from "@/components/Button";
import { Field, SwitchRow } from "@/components/Inputs";
import { Empty, ErrorNote, Loading, Screen } from "@/components/Layout";
import { Sheet } from "@/components/Sheet";
import { Text } from "@/components/Text";
import { useFormState } from "@/hooks/useFormState";
import { useResource } from "@/hooks/useResource";
import { confirmDestructive } from "@/lib/confirm";
import { colors, radius, spacing } from "@/theme";

export default function AccountTypesScreen() {
  const list = useResource(async () => (await accountTypesApi.list()).filter((t) => !t.isArchived));
  const [target, setTarget] = useState<AccountType | "new" | null>(null);

  return (
    <>
      <Screen refreshing={list.refreshing} onRefresh={list.refresh}>
        <Button label="Nuevo tipo de cuenta" icon="add" onPress={() => setTarget("new")} />
        {list.error ? <ErrorNote message={list.error} onRetry={list.reload} /> : null}
        {list.loading && !list.data ? <Loading /> : null}
        {list.data?.length === 0 ? (
          <Empty icon="albums-outline" title="Sin tipos de cuenta" text="Los tipos agrupan tus cuentas: efectivo, banco, billetera o personas." />
        ) : null}
        {list.data?.map((type) => (
          <PressableScale key={type.id} onPress={() => setTarget(type)} style={styles.row}>
            <Text variant="medium" style={styles.flex}>
              {type.name}
            </Text>
            {type.isPerson ? <Text variant="label">Persona</Text> : null}
          </PressableScale>
        ))}
      </Screen>

      <TypeSheet
        target={target}
        onClose={() => setTarget(null)}
        onSaved={() => {
          setTarget(null);
          void list.reload();
        }}
      />
    </>
  );
}

function TypeSheet({ target, onClose, onSaved }: { target: AccountType | "new" | null; onClose: () => void; onSaved: () => void }) {
  const type = target && target !== "new" ? target : null;
  const [form, setForm] = useFormState(target, () => ({ name: type?.name ?? "", isPerson: type?.isPerson ?? false }));
  const [error, setError] = useFormState<string | null>(target, () => null);
  const [saving, setSaving] = useState(false);

  async function save() {
    if (!form.name.trim()) return setError("Escribe un nombre.");
    setSaving(true);
    setError(null);
    try {
      const body = { name: form.name.trim(), isPerson: form.isPerson };
      if (type) await accountTypesApi.update(type.id, { ...body, isArchived: false });
      else await accountTypesApi.create(body);
      onSaved();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  function archive() {
    if (!type) return;
    confirmDestructive("Archivar tipo", `«${type.name}» dejará de ofrecerse al crear cuentas.`, "Archivar", async () => {
      try {
        await accountTypesApi.remove(type.id);
        onSaved();
      } catch (e) {
        // El backend responde 409 si todavía hay cuentas de este tipo
        setError(errorMessage(e));
      }
    });
  }

  return (
    <Sheet visible={target !== null} title={type ? "Editar tipo" : "Nuevo tipo de cuenta"} onClose={onClose}>
      <Field label="Nombre" value={form.name} onChangeText={(name) => setForm({ ...form, name })} placeholder="Efectivo, Banco, Amigos…" />
      <SwitchRow label="Es una persona (deudas y préstamos)" value={form.isPerson} onChange={(isPerson) => setForm({ ...form, isPerson })} />
      {error ? <Text variant="medium" color={colors.danger}>{error}</Text> : null}
      <Button label="Guardar" onPress={save} loading={saving} />
      {type ? <Button label="Archivar tipo" variant="danger" onPress={archive} /> : null}
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
