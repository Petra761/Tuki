import { useMemo } from "react";
import { accountsApi, accountTypesApi, categoriesApi, tagsApi } from "@/api/resources";
import type { Account, AccountType, Category, Tag } from "@/api/types";
import { useResource } from "./useResource";

type Catalog = { accounts: Account[]; types: AccountType[]; categories: Category[]; tags: Tag[] };

// Cuentas, tipos, categorías y marcadores: lo que casi todas las pantallas necesitan para mostrar nombres
export function useCatalog() {
  const resource = useResource<Catalog>(async () => {
    const [accounts, types, categories, tags] = await Promise.all([
      accountsApi.list(),
      accountTypesApi.list(),
      categoriesApi.list(),
      tagsApi.list(),
    ]);
    return {
      accounts: accounts.filter((a) => !a.isArchived),
      types: types.filter((t) => !t.isArchived),
      categories: categories.filter((c) => !c.isArchived),
      tags: tags.filter((t) => !t.isArchived),
    };
  });

  const maps = useMemo(() => {
    const catalog = resource.data;
    return {
      accountById: new Map((catalog?.accounts ?? []).map((a) => [a.id, a])),
      categoryById: new Map((catalog?.categories ?? []).map((c) => [c.id, c])),
      typeById: new Map((catalog?.types ?? []).map((t) => [t.id, t])),
      tagById: new Map((catalog?.tags ?? []).map((t) => [t.id, t])),
    };
  }, [resource.data]);

  return { ...resource, ...maps };
}
