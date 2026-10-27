import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";
import { parseServerDate } from "@/lib/dates";

const KEY = "tuki.session";

function getWebStorage(): Storage {
  if (typeof window === "undefined" || !window.localStorage) {
    throw new Error("El almacenamiento local no está disponible en este navegador.");
  }
  return window.localStorage;
}

export type SessionUser = {
  id: string;
  tenantId: string;
  email: string;
  displayName: string;
  emailVerified: boolean;
};

export type Session = {
  accessToken: string;
  expiresAt: string;
  user: SessionUser;
};

export async function saveSession(session: Session) {
  const value = JSON.stringify(session);
  if (Platform.OS === "web") {
    getWebStorage().setItem(KEY, value);
    return;
  }
  await SecureStore.setItemAsync(KEY, value);
}

export async function clearSession() {
  if (Platform.OS === "web") {
    getWebStorage().removeItem(KEY);
    return;
  }
  await SecureStore.deleteItemAsync(KEY);
}

export async function loadSession(): Promise<Session | null> {
  const raw = Platform.OS === "web"
    ? getWebStorage().getItem(KEY)
    : await SecureStore.getItemAsync(KEY);
  if (!raw) return null;

  let session: Session;
  try {
    session = JSON.parse(raw) as Session;
  } catch {
    await clearSession();
    return null;
  }

  try {
    // Un token vencido no sirve: se descarta para pedir el login directamente
    if (parseServerDate(session.expiresAt).getTime() <= Date.now()) {
      await clearSession();
      return null;
    }
    return session;
  } catch {
    await clearSession();
    return null;
  }
}
