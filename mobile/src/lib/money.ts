// Los importes viajan como enteros en centavos; aquí se convierten para mostrarlos y para leer lo que escribe la persona.

type FormatOptions = { sign?: boolean; hideCents?: boolean };

export function formatBs(cents: number, { sign, hideCents }: FormatOptions = {}): string {
  const abs = Math.abs(Math.round(cents));
  const whole = Math.floor(abs / 100);
  const rest = abs % 100;
  const grouped = String(whole).replace(/\B(?=(\d{3})+(?!\d))/g, ".");
  const decimals = hideCents && rest === 0 ? "" : `,${String(rest).padStart(2, "0")}`;
  const prefix = cents < 0 ? "-" : sign ? "+" : "";
  return `${prefix}Bs ${grouped}${decimals}`;
}

// Acepta "25", "25,5", "25.50" y "1.250,75"; devuelve null si el texto no es un importe válido
export function parseAmountToCents(text: string): number | null {
  const clean = text.trim().replace(/\s/g, "");
  if (!clean) return null;
  // Si hay coma, el punto solo puede ser separador de miles
  const normalized = clean.includes(",") ? clean.replace(/\./g, "").replace(",", ".") : clean;
  if (!/^\d+(\.\d{1,2})?$/.test(normalized)) return null;
  const [whole, decimals = ""] = normalized.split(".");
  return Number(whole) * 100 + Number(decimals.padEnd(2, "0"));
}

export function centsToInput(cents: number): string {
  const abs = Math.abs(cents);
  const rest = abs % 100;
  return rest === 0 ? String(Math.floor(abs / 100)) : `${Math.floor(abs / 100)},${String(rest).padStart(2, "0")}`;
}
