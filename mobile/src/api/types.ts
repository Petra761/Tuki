// Tipos de las respuestas y cuerpos del backend. Los importes son enteros en centavos.

export type Account = {
  id: string;
  accountTypeId: string;
  accountType: string;
  name: string;
  currencyCode: string;
  isDefault: boolean;
  isArchived: boolean;
  createdAt: string;
};
export type AccountCreate = { name: string; accountTypeId: string };
export type AccountUpdate = AccountCreate & { isDefault: boolean; isArchived: boolean };

export type AccountType = { id: string; name: string; isPerson: boolean; isArchived: boolean; createdAt: string };
export type AccountTypeCreate = { name: string; isPerson: boolean };
export type AccountTypeUpdate = AccountTypeCreate & { isArchived: boolean };

export type CategoryKind = "Expense" | "Income";
export type Category = {
  id: string;
  parentId: string | null;
  name: string;
  kind: CategoryKind;
  isArchived: boolean;
  createdAt: string;
};
export type CategoryCreate = { name: string; kind: CategoryKind; parentId: string | null };
export type CategoryUpdate = CategoryCreate & { isArchived: boolean };

export type TransactionType = "Expense" | "Income" | "Transfer" | "Opening";
export type TransactionLine = { id: string; accountId: string; categoryId: string | null; amountCents: number };
export type Transaction = {
  id: string;
  type: TransactionType;
  occurredOn: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  lines: TransactionLine[];
  tagIds: string[];
};
export type TransactionInput = {
  description: string;
  occurredOn: string;
  lines: { accountId: string; categoryId: string | null; amountCents: number }[];
  tagIds: string[];
};
export type TransactionList = { items: Transaction[]; page: number; pageSize: number; total: number };

export type Tag = { id: string; name: string; isArchived: boolean; createdAt: string };
export type TagCreate = { name: string };
export type TagUpdate = TagCreate & { isArchived: boolean };

export type Shortcut = {
  id: string;
  name: string;
  amountCents: number;
  categoryId: string;
  accountId: string;
  sortOrder: number;
  isArchived: boolean;
  createdAt: string;
};
export type ShortcutCreate = { name: string; amountCents: number; categoryId: string; accountId: string; sortOrder: number };
export type ShortcutUpdate = ShortcutCreate & { isArchived: boolean };

export type Budget = {
  id: string;
  categoryId: string | null;
  amountCents: number;
  period: "Monthly";
  isArchived: boolean;
  createdAt: string;
};
export type BudgetCreate = { categoryId: string | null; amountCents: number; period: "Monthly" };
export type BudgetUpdate = BudgetCreate & { isArchived: boolean };

export type SubscriptionStatus = {
  userId: string;
  result: "Active" | "Expired" | "NonExistent";
  reason: string;
  plan: string | null;
  tenantId: string;
  startedAt: string | null;
  expiresAt: string | null;
  status: "Enabled" | "Cancelled" | null;
  showAds: boolean;
  checkedAt: string;
};
