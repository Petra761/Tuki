import { api } from "./client";
import type {
  Account, AccountCreate, AccountType, AccountTypeCreate, AccountTypeUpdate, AccountUpdate,
  Budget, BudgetCreate, BudgetUpdate, Category, CategoryCreate, CategoryUpdate,
  Shortcut, ShortcutCreate, ShortcutUpdate, SubscriptionStatus, Tag, TagCreate, TagUpdate,
  Transaction, TransactionInput, TransactionList,
} from "./types";

// Todos los recursos comparten el mismo patrón CRUD
function crud<T, C, U>(base: string) {
  return {
    list: (query = "") => api<T[]>(`${base}${query}`),
    get: (id: string) => api<T>(`${base}/${id}`),
    create: (body: C) => api<T>(base, { method: "POST", body }),
    update: (id: string, body: U) => api<T>(`${base}/${id}`, { method: "PUT", body }),
    // El backend responde 204: dar de baja es archivar
    remove: (id: string) => api<null>(`${base}/${id}`, { method: "DELETE" }),
  };
}

export const accountsApi = crud<Account, AccountCreate, AccountUpdate>("/api/accounts");
export const accountTypesApi = crud<AccountType, AccountTypeCreate, AccountTypeUpdate>("/api/account-types");
export const categoriesApi = crud<Category, CategoryCreate, CategoryUpdate>("/api/categories");
export const tagsApi = crud<Tag, TagCreate, TagUpdate>("/api/tags");
export const shortcutsApi = crud<Shortcut, ShortcutCreate, ShortcutUpdate>("/api/shortcuts");
export const budgetsApi = crud<Budget, BudgetCreate, BudgetCreate & { isArchived: boolean }>("/api/budgets");

const transactionsCrud = crud<Transaction, TransactionInput, TransactionInput>("/api/transactions");

type TransactionQuery = { from?: string; to?: string; search?: string; tagId?: string; page?: number; pageSize?: number };

function toQueryString(params: Record<string, string | number | undefined>): string {
  // URLSearchParams no está completo en React Native, por eso se arma a mano
  const parts = Object.entries(params)
    .filter(([, value]) => value !== undefined && value !== "")
    .map(([key, value]) => `${key}=${encodeURIComponent(String(value))}`);
  return parts.length ? `?${parts.join("&")}` : "";
}

export const transactionsApi = {
  get: transactionsCrud.get,
  create: transactionsCrud.create,
  update: transactionsCrud.update,
  remove: transactionsCrud.remove,
  query: (params: TransactionQuery = {}) => api<TransactionList>(`/api/transactions${toQueryString(params)}`),
};

// Los saldos se calculan sumando líneas, así que hace falta traer todos los movimientos
export async function fetchAllTransactions(filters: { from?: string; to?: string } = {}): Promise<Transaction[]> {
  const pageSize = 200;
  const all: Transaction[] = [];
  for (let page = 1; page <= 50; page++) {
    const result = await transactionsApi.query({ ...filters, page, pageSize });
    all.push(...result.items);
    if (all.length >= result.total || result.items.length === 0) break;
  }
  return all;
}

export const getSubscriptionStatus = (userId: string) =>
  api<SubscriptionStatus>(`/api/users/${userId}/subscription-status`);

// Re-exportado para que las pantallas importen todo desde un solo lugar
export type { Account, AccountType, Budget, Category, Shortcut, Tag, Transaction };
