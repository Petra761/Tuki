import { API_URL } from "./config";

export class ApiError extends Error {
  status: number;
  data: unknown;

  constructor(status: number, message: string, data: unknown) {
    super(message);
    this.status = status;
    this.data = data;
  }
}

type Options = {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  timeoutMs?: number;
};

let accessToken: string | null = null;
let onUnauthorized: (() => void) | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

// El contexto de sesión registra aquí qué hacer cuando el servidor responde 401
export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler;
}

// Algunos errores del backend llegan como texto plano y no como JSON
function parseBody(text: string): unknown {
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function extractMessage(data: unknown, status: number): string {
  if (typeof data === "string" && data) return data;
  if (data && typeof data === "object") {
    const body = data as { error?: unknown; errors?: unknown; title?: unknown };
    if (typeof body.error === "string") return body.error;
    // Validación del modelo (400): { errors: { Campo: ["mensaje"] } }
    if (body.errors && typeof body.errors === "object") {
      const first = Object.values(body.errors as Record<string, unknown>)[0];
      if (Array.isArray(first) && typeof first[0] === "string") return first[0];
    }
    if (typeof body.title === "string") return body.title;
  }
  return `Error ${status}`;
}

export async function api<T = unknown>(
  path: string,
  { method = "GET", body, timeoutMs = 15000 }: Options = {},
): Promise<T> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  let res: Response;
  try {
    res = await fetch(API_URL + path, {
      method,
      headers: {
        ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
        // No se envía X-Tenant-Id: el servidor lo deriva del token
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: controller.signal,
    });
  } catch {
    throw new ApiError(
      0,
      controller.signal.aborted
        ? "El servidor tardó demasiado en responder. Intenta de nuevo."
        : "No se pudo conectar con el servidor. Revisa tu conexión e intenta de nuevo.",
      null,
    );
  } finally {
    clearTimeout(timer);
  }

  const data = parseBody(await res.text());

  if (!res.ok) {
    // Solo es sesión vencida si se envió un token; en el login un 401 significa credenciales incorrectas
    if (res.status === 401 && accessToken) onUnauthorized?.();
    throw new ApiError(res.status, extractMessage(data, res.status), data);
  }

  return data as T;
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Ocurrió un error inesperado.";
}
