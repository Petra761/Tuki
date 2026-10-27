import type { Category, Transaction } from "@/api/types";

// Cálculos sobre los movimientos: el backend no entrega saldos, así que se obtienen sumando líneas

function byDate(a: Transaction, b: Transaction) {
  return a.occurredOn === b.occurredOn ? a.createdAt.localeCompare(b.createdAt) : a.occurredOn.localeCompare(b.occurredOn);
}

export function balancesFrom(transactions: Transaction[]): Map<string, number> {
  const balances = new Map<string, number>();
  for (const t of transactions) {
    for (const line of t.lines) {
      balances.set(line.accountId, (balances.get(line.accountId) ?? 0) + line.amountCents);
    }
  }
  return balances;
}

// Saldo acumulado después de cada línea; sirve para dibujar la tendencia de una cuenta
export function historyFrom(transactions: Transaction[], accountId: string, points = 8): number[] {
  let running = 0;
  const series: number[] = [];
  for (const t of [...transactions].sort(byDate)) {
    for (const line of t.lines) {
      if (line.accountId === accountId) {
        running += line.amountCents;
        series.push(running);
      }
    }
  }
  return series.slice(-points);
}

export function monthTotals(transactions: Transaction[]): { income: number; expense: number } {
  let income = 0;
  let expense = 0;
  for (const t of transactions) {
    const total = t.lines.reduce((sum, l) => sum + l.amountCents, 0);
    if (t.type === "Income") income += total;
    if (t.type === "Expense") expense += -total;
  }
  return { income, expense };
}

// Suma lo gastado por categoría; las subcategorías se agrupan bajo su categoría principal
export function expenseByCategory(transactions: Transaction[], categoryById: Map<string, Category>) {
  const totals = new Map<string, number>();
  for (const t of transactions) {
    if (t.type !== "Expense") continue;
    for (const line of t.lines) {
      if (!line.categoryId) continue;
      const category = categoryById.get(line.categoryId);
      const rootId = category?.parentId ?? line.categoryId;
      totals.set(rootId, (totals.get(rootId) ?? 0) + -line.amountCents);
    }
  }
  return [...totals.entries()]
    .map(([categoryId, amount]) => ({ categoryId, amount, name: categoryById.get(categoryId)?.name ?? "Sin categoría" }))
    .sort((a, b) => b.amount - a.amount);
}

// Lo gastado en una categoría, contando también sus subcategorías
export function spentInCategory(transactions: Transaction[], categoryId: string, categoryById: Map<string, Category>): number {
  let spent = 0;
  for (const t of transactions) {
    if (t.type !== "Expense") continue;
    for (const line of t.lines) {
      if (!line.categoryId) continue;
      const category = categoryById.get(line.categoryId);
      if (line.categoryId === categoryId || category?.parentId === categoryId) spent += -line.amountCents;
    }
  }
  return spent;
}

export function categoryLabel(category: Category | undefined, categoryById: Map<string, Category>): string {
  if (!category) return "Sin categoría";
  const parent = category.parentId ? categoryById.get(category.parentId) : undefined;
  return parent ? `${parent.name} › ${category.name}` : category.name;
}

// Neto de un movimiento para mostrarlo en la lista: un traslado entre cuentas propias no cambia el total
export function netAmount(transaction: Transaction): number {
  if (transaction.type === "Transfer") {
    return Math.max(...transaction.lines.map((l) => Math.abs(l.amountCents)), 0);
  }
  return transaction.lines.reduce((sum, l) => sum + l.amountCents, 0);
}
