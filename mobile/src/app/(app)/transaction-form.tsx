import { useEffect, useMemo, useState } from "react";
import { Alert, StyleSheet, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { errorMessage } from "@/api/client";
import { transactionsApi } from "@/api/resources";
import type { Category, Transaction } from "@/api/types";
import { Button, PressableScale } from "@/components/Button";
import { Chips, Field, Segmented } from "@/components/Inputs";
import { ErrorNote, Loading, Screen } from "@/components/Layout";
import { Text } from "@/components/Text";
import { useCatalog } from "@/hooks/useCatalog";
import { addDays, isValidISODate, todayISO } from "@/lib/dates";
import { categoryLabel } from "@/lib/ledger";
import { centsToInput, parseAmountToCents } from "@/lib/money";
import { colors, radius, spacing } from "@/theme";

type Kind = "Expense" | "Income" | "Transfer";

const KINDS = [
  { value: "Expense", label: "Gasto" },
  { value: "Income", label: "Ingreso" },
  { value: "Transfer", label: "Traslado" },
];

const GUIDE: Record<Kind, string> = {
  Expense: "¿Consumiste algo? Indica de qué cuenta salió el dinero.",
  Income: "Entró dinero nuevo. Indica a qué cuenta llegó.",
  Transfer: "El dinero solo cambia de lugar entre tus cuentas; no consumiste nada.",
};

export default function TransactionFormScreen() {
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id?: string }>();
  const catalog = useCatalog();

  const [kind, setKind] = useState<Kind>("Expense");
  const [amount, setAmount] = useState("");
  const [description, setDescription] = useState("");
  const [date, setDate] = useState(todayISO());
  const [accountId, setAccountId] = useState("");
  const [toAccountId, setToAccountId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [tagIds, setTagIds] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [loadingEdit, setLoadingEdit] = useState(!!id);
  const [unsupported, setUnsupported] = useState<string | null>(null);

  // Al editar se carga el movimiento y se rellena el formulario con sus datos
  useEffect(() => {
    if (!id) return;
    let active = true;
    transactionsApi
      .get(id)
      .then((t: Transaction) => {
        if (!active) return;
        setDescription(t.description);
        setDate(t.occurredOn);
        setTagIds(t.tagIds);
        if (t.type === "Opening") {
          setUnsupported("Este movimiento es el saldo inicial de una cuenta y no se edita desde aquí.");
        } else if (t.type === "Transfer" && t.lines.length === 2) {
          const from = t.lines.find((l) => l.amountCents < 0);
          const to = t.lines.find((l) => l.amountCents > 0);
          setKind("Transfer");
          setAmount(centsToInput(to?.amountCents ?? 0));
          setAccountId(from?.accountId ?? "");
          setToAccountId(to?.accountId ?? "");
        } else if ((t.type === "Expense" || t.type === "Income") && t.lines.length === 1) {
          setKind(t.type);
          setAmount(centsToInput(t.lines[0].amountCents));
          setAccountId(t.lines[0].accountId);
          setCategoryId(t.lines[0].categoryId ?? "");
        } else {
          setUnsupported("Este movimiento tiene varias líneas y no se puede editar desde la app.");
        }
      })
      .catch((e) => active && setError(errorMessage(e)))
      .finally(() => active && setLoadingEdit(false));
    return () => {
      active = false;
    };
  }, [id]);

  const accounts = catalog.data?.accounts ?? [];

  // Una categoría con subcategorías no se elige: se registra siempre en la más específica
  const categoryOptions = useMemo(() => {
    const all = catalog.data?.categories ?? [];
    const parents = new Set(all.map((c) => c.parentId).filter(Boolean));
    const wanted: Category["kind"] = kind === "Income" ? "Income" : "Expense";
    return all
      .filter((c) => c.kind === wanted && !parents.has(c.id))
      .map((c) => ({ value: c.id, label: categoryLabel(c, catalog.categoryById) }));
  }, [catalog.data, catalog.categoryById, kind]);

  // Si cambia el tipo y la categoría elegida ya no corresponde, se limpia
  useEffect(() => {
    if (categoryId && !categoryOptions.some((o) => o.value === categoryId)) setCategoryId("");
  }, [categoryOptions, categoryId]);

  const toggleTag = (tagId: string) =>
    setTagIds((current) => (current.includes(tagId) ? current.filter((t) => t !== tagId) : [...current, tagId]));

  async function save() {
    const cents = parseAmountToCents(amount);
    if (!cents) return setError("Escribe un monto mayor a cero.");
    if (!accountId) return setError(kind === "Transfer" ? "Elige la cuenta de origen." : "Elige una cuenta.");
    if (kind === "Transfer") {
      if (!toAccountId) return setError("Elige la cuenta de destino.");
      if (toAccountId === accountId) return setError("El origen y el destino deben ser cuentas distintas.");
    } else if (!categoryId) {
      return setError("Elige una categoría.");
    }
    if (!isValidISODate(date)) return setError("La fecha debe tener el formato AAAA-MM-DD.");

    const lines =
      kind === "Transfer"
        ? [
            { accountId, categoryId: null, amountCents: -cents },
            { accountId: toAccountId, categoryId: null, amountCents: cents },
          ]
        : [{ accountId, categoryId, amountCents: kind === "Expense" ? -cents : cents }];

    const fallback = kind === "Transfer" ? "Traslado" : (catalog.categoryById.get(categoryId)?.name ?? "Movimiento");
    const body = { description: description.trim() || fallback, occurredOn: date, lines, tagIds };

    setSaving(true);
    setError(null);
    try {
      if (id) await transactionsApi.update(id, body);
      else await transactionsApi.create(body);
      router.back();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  function remove() {
    if (!id) return;
    Alert.alert("Eliminar movimiento", "Esta acción no se puede deshacer.", [
      { text: "Cancelar", style: "cancel" },
      {
        text: "Eliminar",
        style: "destructive",
        onPress: async () => {
          try {
            await transactionsApi.remove(id);
            router.back();
          } catch (e) {
            setError(errorMessage(e));
          }
        },
      },
    ]);
  }

  if (catalog.loading && !catalog.data) return <Loading />;
  if (catalog.error) return <Screen><ErrorNote message={catalog.error} onRetry={catalog.reload} /></Screen>;
  if (loadingEdit) return <Loading />;

  if (unsupported) {
    return (
      <Screen>
        <Text variant="medium">{unsupported}</Text>
        <Button label="Eliminar movimiento" variant="danger" onPress={remove} />
      </Screen>
    );
  }

  const accountOptions = accounts.map((a) => ({ value: a.id, label: a.name }));

  return (
    <Screen>
      <Segmented options={KINDS} value={kind} onChange={(value) => setKind(value as Kind)} />
      <Text variant="label">{GUIDE[kind]}</Text>

      <Field
        label="Monto (Bs)"
        value={amount}
        onChangeText={setAmount}
        keyboardType="decimal-pad"
        placeholder="0,00"
        style={styles.amount}
        autoFocus={!id}
      />

      <Chips
        label={kind === "Transfer" ? "Desde la cuenta" : kind === "Income" ? "Hacia la cuenta" : "Desde la cuenta"}
        options={accountOptions}
        selected={[accountId]}
        onSelect={setAccountId}
        emptyText="Aún no tienes cuentas. Crea una en la pestaña Cuentas."
      />

      {kind === "Transfer" ? (
        <Chips label="Hacia la cuenta" options={accountOptions} selected={[toAccountId]} onSelect={setToAccountId} />
      ) : (
        <Chips
          label="Categoría"
          options={categoryOptions}
          selected={[categoryId]}
          onSelect={setCategoryId}
          emptyText="No hay categorías de este tipo. Crea una en Más › Categorías."
        />
      )}

      <Field label="Descripción (opcional)" value={description} onChangeText={setDescription} placeholder="Compra semanal" />

      <View style={styles.dateRow}>
        <View style={styles.flex}>
          <Field label="Fecha" value={date} onChangeText={setDate} placeholder="AAAA-MM-DD" keyboardType="numbers-and-punctuation" />
        </View>
        <PressableScale onPress={() => setDate(todayISO())} style={styles.quickDate}>
          <Text variant="medium">Hoy</Text>
        </PressableScale>
        <PressableScale onPress={() => setDate(addDays(todayISO(), -1))} style={styles.quickDate}>
          <Text variant="medium">Ayer</Text>
        </PressableScale>
      </View>

      {(catalog.data?.tags.length ?? 0) > 0 ? (
        <Chips
          label="Marcadores"
          options={(catalog.data?.tags ?? []).map((t) => ({ value: t.id, label: `#${t.name}` }))}
          selected={tagIds}
          onSelect={toggleTag}
        />
      ) : null}

      {error ? (
        <Text variant="medium" color={colors.danger}>
          {error}
        </Text>
      ) : null}

      <Button label={id ? "Guardar cambios" : "Guardar movimiento"} onPress={save} loading={saving} />
      {id ? <Button label="Eliminar movimiento" variant="danger" onPress={remove} /> : null}
    </Screen>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  amount: { fontSize: 28, minHeight: 64 },
  dateRow: { flexDirection: "row", alignItems: "flex-end", gap: spacing.sm },
  quickDate: {
    minHeight: 52,
    paddingHorizontal: spacing.md,
    borderRadius: radius.md,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
    justifyContent: "center",
  },
});
