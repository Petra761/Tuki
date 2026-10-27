import { useEffect, useMemo, useState } from "react";
import { RefreshControl, SectionList, StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { transactionsApi } from "@/api/resources";
import type { Transaction } from "@/api/types";
import { IconButton } from "@/components/Button";
import { Field } from "@/components/Inputs";
import { Empty, ErrorNote, Loading, MonthSwitcher, TabHeader } from "@/components/Layout";
import { Text } from "@/components/Text";
import { TransactionRow } from "@/components/TransactionRow";
import { useCatalog } from "@/hooks/useCatalog";
import { useResource } from "@/hooks/useResource";
import { currentMonth, dayLabel, monthLabel, monthRange, shiftMonth } from "@/lib/dates";
import { colors, spacing } from "@/theme";

export default function TransactionsScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const catalog = useCatalog();
  const [month, setMonth] = useState(currentMonth());
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");

  // La búsqueda espera a que se deje de escribir para no llamar al servidor en cada letra
  useEffect(() => {
    const timer = setTimeout(() => setQuery(search.trim()), 350);
    return () => clearTimeout(timer);
  }, [search]);

  const range = monthRange(month);
  const list = useResource(
    () => transactionsApi.query({ ...range, search: query || undefined, pageSize: 200 }),
    [range.from, range.to, query],
  );

  const sections = useMemo(() => {
    const groups = new Map<string, Transaction[]>();
    for (const t of list.data?.items ?? []) {
      groups.set(t.occurredOn, [...(groups.get(t.occurredOn) ?? []), t]);
    }
    return [...groups.entries()]
      .sort(([a], [b]) => b.localeCompare(a))
      .map(([date, data]) => ({ title: dayLabel(date), data }));
  }, [list.data]);

  const error = list.error ?? catalog.error;

  return (
    <SectionList
      style={styles.list}
      contentContainerStyle={{ padding: spacing.md, paddingTop: insets.top + spacing.md, gap: spacing.sm }}
      sections={sections}
      keyExtractor={(t) => t.id}
      keyboardShouldPersistTaps="handled"
      stickySectionHeadersEnabled={false}
      refreshControl={
        <RefreshControl
          refreshing={list.refreshing}
          onRefresh={list.refresh}
          tintColor={colors.primary}
          colors={[colors.primary]}
          progressBackgroundColor={colors.surface}
        />
      }
      ListHeaderComponent={
        <View style={styles.header}>
          <TabHeader title="Movimientos" right={<IconButton icon="add" label="Nuevo movimiento" onPress={() => router.push("/transaction-form")} />} />
          <MonthSwitcher label={monthLabel(month)} onPrev={() => setMonth(shiftMonth(month, -1))} onNext={() => setMonth(shiftMonth(month, 1))} />
          <Field label="Buscar" value={search} onChangeText={setSearch} placeholder="Descripción del movimiento" />
          {error ? <ErrorNote message={error} onRetry={list.reload} /> : null}
        </View>
      }
      renderSectionHeader={({ section }) => (
        <Text variant="label" style={styles.day}>
          {section.title}
        </Text>
      )}
      renderItem={({ item }) => (
        <TransactionRow
          transaction={item}
          accountById={catalog.accountById}
          categoryById={catalog.categoryById}
          onPress={() => router.push({ pathname: "/transaction-form", params: { id: item.id } })}
        />
      )}
      ListEmptyComponent={
        list.loading && !list.data ? (
          <Loading />
        ) : error ? undefined : (
          <Empty
            icon="receipt-outline"
            title={query ? "Sin resultados" : "No hay movimientos este mes"}
            text={query ? "Prueba con otra palabra." : "Registra tu primer gasto o ingreso del mes."}
            actionLabel={query ? undefined : "Registrar movimiento"}
            onAction={() => router.push("/transaction-form")}
          />
        )
      }
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1, backgroundColor: colors.background },
  header: { gap: spacing.md, marginBottom: spacing.sm },
  day: { marginTop: spacing.sm },
});
