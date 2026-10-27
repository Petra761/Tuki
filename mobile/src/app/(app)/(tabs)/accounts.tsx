import { useMemo, useState } from "react";
import { Alert, StyleSheet, View } from "react-native";
import { IconButton, Button } from "@/components/Button";
import { Chips, Field, SwitchRow } from "@/components/Inputs";
import { Card, Empty, ErrorNote, Loading, Screen, SectionTitle, TabHeader } from "@/components/Layout";
import { Sparkline } from "@/components/Charts";
import { Sheet } from "@/components/Sheet";
import { Text } from "@/components/Text";
import { PressableScale } from "@/components/Button";
import { accountsApi, fetchAllTransactions, transactionsApi } from "@/api/resources";
import type { Account } from "@/api/types";
import { errorMessage } from "@/api/client";
import { useCatalog } from "@/hooks/useCatalog";
import { useResource } from "@/hooks/useResource";
import { balancesFrom, historyFrom } from "@/lib/ledger";
import { formatBs, parseAmountToCents } from "@/lib/money";
import { todayISO } from "@/lib/dates";
import { colors, spacing } from "@/theme";

export default function AccountsScreen() {
  const catalog = useCatalog();
  const ledger = useResource(() => fetchAllTransactions());
  const [editing, setEditing] = useState<Account | "new" | null>(null);

  const accounts = catalog.data?.accounts ?? [];
  const transactions = ledger.data ?? [];
  const balances = useMemo(() => balancesFrom(transactions), [transactions]);

  const isPerson = (account: Account) => catalog.typeById.get(account.accountTypeId)?.isPerson ?? false;
  const own = accounts.filter((a) => !isPerson(a));
  const people = accounts.filter(isPerson);
  const total = own.reduce((sum, a) => sum + (balances.get(a.id) ?? 0), 0);

  function reloadAll() {
    void catalog.reload();
    void ledger.reload();
  }

  const loading = (catalog.loading && !catalog.data) || (ledger.loading && !ledger.data);
  const error = catalog.error ?? ledger.error;

  function renderCard(account: Account) {
    const balance = balances.get(account.id) ?? 0;
    const person = isPerson(account);
    return (
      <PressableScale key={account.id} onPress={() => setEditing(account)} style={styles.accountCard}>
        <View style={styles.flex}>
          <Text variant="label">
            {account.name}
            {account.isDefault ? " · principal" : ""}
          </Text>
          <Text variant="heading" color={balance < 0 ? colors.danger : colors.text}>
            {formatBs(balance)}
          </Text>
          {person ? (
            <Text variant="label" color={colors.textDim}>
              {balance > 0 ? "Te debe" : balance < 0 ? "Le debes" : "Sin deuda"}
            </Text>
          ) : (
            <Text variant="label" color={colors.textDim}>
              {account.accountType}
            </Text>
          )}
        </View>
        <Sparkline values={historyFrom(transactions, account.id)} />
      </PressableScale>
    );
  }

  return (
    <>
      <Screen tab refreshing={catalog.refreshing} onRefresh={() => { catalog.refresh(); void ledger.reload(); }}>
        <TabHeader title="Cuentas" right={<IconButton icon="add" label="Nueva cuenta" onPress={() => setEditing("new")} />} />

        {error ? <ErrorNote message={error} onRetry={reloadAll} /> : null}
        {loading ? <Loading /> : null}

        {!loading && !error ? (
          <>
            <View>
              <Text variant="label">Total en tus cuentas</Text>
              <Text variant="big">{formatBs(total)}</Text>
            </View>

            {accounts.length === 0 ? (
              <Empty
                icon="wallet-outline"
                title="Aún no tienes cuentas"
                text="Una cuenta es un lugar donde guardas dinero: efectivo, un banco o una billetera."
                actionLabel="Crear mi primera cuenta"
                onAction={() => setEditing("new")}
              />
            ) : null}

            {own.map(renderCard)}

            {people.length > 0 ? (
              <>
                <SectionTitle>Personas</SectionTitle>
                {people.map(renderCard)}
              </>
            ) : null}
          </>
        ) : null}
      </Screen>

      <AccountSheet
        target={editing}
        types={catalog.data?.types ?? []}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null);
          reloadAll();
        }}
      />
    </>
  );
}

type SheetProps = {
  target: Account | "new" | null;
  types: { id: string; name: string; isPerson: boolean }[];
  onClose: () => void;
  onSaved: () => void;
};

function AccountSheet({ target, types, onClose, onSaved }: SheetProps) {
  const account = target && target !== "new" ? target : null;
  const [name, setName] = useState("");
  const [typeId, setTypeId] = useState("");
  const [initial, setInitial] = useState("");
  const [isDefault, setIsDefault] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [loadedFor, setLoadedFor] = useState<unknown>(null);

  // Al abrir la hoja se cargan los datos de la cuenta elegida (o se limpian para una nueva)
  if (target !== loadedFor) {
    setLoadedFor(target);
    setName(account?.name ?? "");
    setTypeId(account?.accountTypeId ?? "");
    setInitial("");
    setIsDefault(account?.isDefault ?? false);
    setError(null);
  }

  async function save() {
    if (!name.trim()) return setError("Escribe un nombre para la cuenta.");
    if (!typeId) return setError("Elige un tipo de cuenta.");
    const initialCents = initial.trim() ? parseAmountToCents(initial) : 0;
    if (initialCents === null) return setError("El saldo inicial no es un importe válido.");

    setSaving(true);
    setError(null);
    try {
      if (account) {
        await accountsApi.update(account.id, { name: name.trim(), accountTypeId: typeId, isDefault, isArchived: false });
      } else {
        const created = await accountsApi.create({ name: name.trim(), accountTypeId: typeId });
        if (isDefault) {
          await accountsApi.update(created.id, { name: created.name, accountTypeId: typeId, isDefault: true, isArchived: false });
        }
        if (initialCents && initialCents !== 0) {
          // El saldo inicial es un movimiento sin categoría con una sola línea positiva
          await transactionsApi.create({
            description: "Saldo inicial",
            occurredOn: todayISO(),
            lines: [{ accountId: created.id, categoryId: null, amountCents: initialCents }],
            tagIds: [],
          });
        }
      }
      onSaved();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  function archive() {
    if (!account) return;
    Alert.alert("Archivar cuenta", `«${account.name}» dejará de aparecer en la lista. Sus movimientos se conservan.`, [
      { text: "Cancelar", style: "cancel" },
      {
        text: "Archivar",
        style: "destructive",
        onPress: async () => {
          try {
            await accountsApi.remove(account.id);
            onSaved();
          } catch (e) {
            setError(errorMessage(e));
          }
        },
      },
    ]);
  }

  return (
    <Sheet visible={target !== null} title={account ? "Editar cuenta" : "Nueva cuenta"} onClose={onClose}>
      <Field label="Nombre" value={name} onChangeText={setName} placeholder="Efectivo, Banco Unión…" />
      <Chips
        label="Tipo de cuenta"
        options={types.map((t) => ({ value: t.id, label: t.name }))}
        selected={[typeId]}
        onSelect={setTypeId}
        emptyText="Primero crea un tipo de cuenta en Más › Tipos de cuenta."
      />
      {!account ? (
        <Field label="Saldo inicial (opcional)" value={initial} onChangeText={setInitial} keyboardType="decimal-pad" placeholder="0,00" />
      ) : null}
      <SwitchRow label="Cuenta principal" value={isDefault} onChange={setIsDefault} />
      {error ? <Text variant="medium" color={colors.danger}>{error}</Text> : null}
      <Button label="Guardar" onPress={save} loading={saving} />
      {account ? <Button label="Archivar cuenta" variant="danger" onPress={archive} /> : null}
    </Sheet>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  accountCard: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
});
