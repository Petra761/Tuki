import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { login } from "@/api/auth";
import { setAccessToken, setUnauthorizedHandler } from "@/api/client";
import { clearSession, loadSession, saveSession, type Session } from "./session";

type AuthValue = {
  session: Session | null;
  loading: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);

  const signOut = useCallback(async () => {
    setAccessToken(null);
    setSession(null);
    await clearSession();
  }, []);

  useEffect(() => {
    // Si el servidor responde 401 con un token enviado, la sesión venció: se vuelve al login
    setUnauthorizedHandler(() => {
      void signOut();
    });

    let active = true;
    loadSession().then((stored) => {
      if (!active) return;
      if (stored) {
        setAccessToken(stored.accessToken);
        setSession(stored);
      }
      setLoading(false);
    });

    return () => {
      active = false;
      setUnauthorizedHandler(null);
    };
  }, [signOut]);

  const signIn = useCallback(async (email: string, password: string) => {
    const next = await login(email, password);
    setAccessToken(next.accessToken);
    await saveSession(next);
    setSession(next);
  }, []);

  const value = useMemo(() => ({ session, loading, signIn, signOut }), [session, loading, signIn, signOut]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth debe usarse dentro de AuthProvider");
  return ctx;
}
