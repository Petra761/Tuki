const MONTHS = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];
const WEEKDAYS = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

const pad = (n: number) => String(n).padStart(2, "0");

export function toISODate(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

// Se construye con año, mes y día para evitar que "YYYY-MM-DD" se interprete en UTC y corra un día
export function parseISODate(iso: string): Date {
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y, m - 1, d);
}

export const todayISO = () => toISODate(new Date());

export function addDays(iso: string, days: number): string {
  const d = parseISODate(iso);
  d.setDate(d.getDate() + days);
  return toISODate(d);
}

export function isValidISODate(text: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(text)) return false;
  return toISODate(parseISODate(text)) === text;
}

export type Month = { year: number; month: number };

export function currentMonth(): Month {
  const now = new Date();
  return { year: now.getFullYear(), month: now.getMonth() + 1 };
}

export function shiftMonth({ year, month }: Month, delta: number): Month {
  const d = new Date(year, month - 1 + delta, 1);
  return { year: d.getFullYear(), month: d.getMonth() + 1 };
}

export function monthRange({ year, month }: Month): { from: string; to: string } {
  return { from: toISODate(new Date(year, month - 1, 1)), to: toISODate(new Date(year, month, 0)) };
}

export function monthLabel({ year, month }: Month): string {
  return `${MONTHS[month - 1]} ${year}`;
}

export function monthName(month: number): string {
  return MONTHS[month - 1];
}

export function dayLabel(iso: string): string {
  const today = todayISO();
  if (iso === today) return "Hoy";
  if (iso === addDays(today, -1)) return "Ayer";
  const d = parseISODate(iso);
  return `${WEEKDAYS[d.getDay()]} ${d.getDate()} de ${MONTHS[d.getMonth()]}`;
}

// El backend envía hasta 7 decimales en los segundos y no todos los motores de JS los aceptan
export function parseServerDate(value: string): Date {
  return new Date(value.replace(/(\.\d{3})\d+/, "$1"));
}

export function formatTime(value: string): string {
  const d = parseServerDate(value);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function formatDateTime(value: string): string {
  const d = parseServerDate(value);
  return `${d.getDate()} de ${MONTHS[d.getMonth()]}, ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
