import type { Session } from "@/auth/session";
import { api, ApiError } from "./client";

export type RegisterRequest = { email: string; displayName: string; password: string };

export type RegisterResponse = {
  userId: string;
  tenantId: string;
  status: string;
  services: { service: string; database: string; result: string; error: string | null }[];
  passwordTemporal?: string | null;
};

export function login(email: string, password: string) {
  return api<Session>("/api/auth/login", {
    method: "POST",
    body: { email: email.trim(), password },
  });
}

export function register(data: RegisterRequest) {
  return api<RegisterResponse>("/api/users/register", {
    method: "POST",
    body: { ...data, email: data.email.trim() },
    // Registrar crea tres bases de datos y tarda más que una petición normal
    timeoutMs: 60000,
  });
}

// El 423 trae la fecha hasta la que la cuenta está bloqueada
export function getLockedUntil(error: ApiError): string | null {
  const data = error.data as { lockedUntil?: string } | null;
  return error.status === 423 && data?.lockedUntil ? data.lockedUntil : null;
}
