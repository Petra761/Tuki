#!/usr/bin/env bash
# Migra las credenciales existentes de tuki_admin.users a
# tuki_autenticacion.users conservando Id y TenantId, que son los que
# dependen los tokens emitidos y las bases ya aprovisionadas.
#
# Uso: ./scripts/migrar-usuarios.sh
#
# Reglas: no borra ni modifica nada en el origen; solo lectura de tuki_admin
# y escritura de filas nuevas en tuki_autenticacion. Las credenciales de
# Postgres salen del .env de la raíz en el momento de ejecutar: nunca quedan
# escritas en este archivo.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARCHIVO_ENV="$RAIZ/.env"
CARPETA_BACKUPS="$RAIZ/backups"

fallo() {
  echo "FALLO: $1" >&2
  exit 1
}

for cmd in psql pg_dump; do
  command -v "$cmd" >/dev/null 2>&1 || fallo "no está $cmd en el PATH (herramientas cliente de PostgreSQL)."
done

[ -f "$ARCHIVO_ENV" ] || fallo "no existe $ARCHIVO_ENV."

# ---- Credenciales y nombres de base desde el .env (solo se leen) ----
leer() { grep -E "^$1=" "$ARCHIVO_ENV" | head -n 1 | cut -d= -f2-; }

for clave in Tuki__Postgres__Host Tuki__Postgres__Port Tuki__Postgres__Usuario Tuki__Postgres__Password; do
  grep -q "^$clave=" "$ARCHIVO_ENV" || fallo "falta $clave en .env."
done

PGHOST="$(leer Tuki__Postgres__Host)"
PGPORT="$(leer Tuki__Postgres__Port)"
PGUSER="$(leer Tuki__Postgres__Usuario)"
# PGPASSWORD viaja por variable de entorno, nunca por la línea de comandos:
# así no aparece ni en el historial ni en la lista de procesos
export PGPASSWORD="$(leer Tuki__Postgres__Password)"

BASE_ORIGEN="$(leer Tuki__BaseAdministrador)"
BASE_ORIGEN="${BASE_ORIGEN:-tuki_admin}"
BASE_DESTINO="$(leer Tuki__BaseAutenticacion)"
BASE_DESTINO="${BASE_DESTINO:-tuki_autenticacion}"

# Los archivos intermedios con los hashes viven en un temporal y se borran al
# salir; en backups/ solo queda el respaldo comprimido de tuki_admin
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

# ---- 1. Abortar si el destino ya tiene usuarios ----
# Un destino con filas significaría duplicar credenciales; el script jamás
# sobrescribe ni borra, así que en ese caso no hace nada.
if ! destino="$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$BASE_DESTINO" \
  -v ON_ERROR_STOP=1 -tAc "SELECT count(*) FROM users" 2>"$TMP/destino.err")"; then
  fallo "no se pudo leer $BASE_DESTINO.users ($(cat "$TMP/destino.err" | head -n 1)).
        Arranca el servicio de Autenticación una vez para que cree su base y vuelve a intentar."
fi

if [ "$destino" != "0" ]; then
  fallo "la tabla $BASE_DESTINO.users ya tiene $destino fila(s); no se migra nada."
fi

# ---- 2. Respaldo completo de tuki_admin ----
mkdir -p "$CARPETA_BACKUPS"
FECHA="$(date +%Y%m%d-%H%M%S)"
RESPALDO="$CARPETA_BACKUPS/tuki_admin-$FECHA.dump"

echo "Respaldando $BASE_ORIGEN completo -> $RESPALDO"
pg_dump -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$BASE_ORIGEN" -Fc -f "$RESPALDO"
[ -s "$RESPALDO" ] || fallo "el respaldo quedó vacío: $RESPALDO"

# ---- 3. Vuelco de la tabla de usuarios y restauración en el destino ----
echo "Volcando solo la tabla users de $BASE_ORIGEN"
# --column-inserts escribe INSERTs con nombres de columna explícitos, así el
# vuelco se aplica aunque el destino no tenga la clave foránea del origen
pg_dump -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$BASE_ORIGEN" \
  -a -t users --column-inserts -f "$TMP/users.sql"

echo "Restaurando en $BASE_DESTINO"
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$BASE_DESTINO" \
  -v ON_ERROR_STOP=1 -f "$TMP/users.sql" >"$TMP/restauracion.log"

# ---- 4. Comparación final de conteos y correos ----
origen="$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$BASE_ORIGEN" \
  -v ON_ERROR_STOP=1 -tAc "SELECT count(*) FROM users")"
destino="$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$BASE_DESTINO" \
  -v ON_ERROR_STOP=1 -tAc "SELECT count(*) FROM users")"

echo
echo "Filas en $BASE_ORIGEN.users:  $origen"
echo "Filas en $BASE_DESTINO.users: $destino"

if [ "$origen" != "$destino" ]; then
  # Se conserva el temporal para poder revisarlo: contiene los INSERTs que no
  # se aplicaron por completo
  trap - EXIT
  fallo "los conteos no coinciden. Revisa $TMP/users.sql (no se borró nada en el origen)."
fi

echo
echo "Correos migrados (solo correos: los hashes nunca se muestran):"
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$BASE_DESTINO" \
  -v ON_ERROR_STOP=1 -tAc "SELECT '  - ' || email FROM users ORDER BY email"

echo
echo "Listo. Respaldo de $BASE_ORIGEN en: $RESPALDO"
echo "No se eliminó ni modificó ninguna fila en el origen."
