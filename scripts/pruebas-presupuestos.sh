#!/usr/bin/env bash
# Pruebas del CRUD de budgets contra el Gateway.
# Uso: TENANT_A=... TENANT_B=... ./scripts/pruebas-presupuestos.sh
set -u

GATEWAY="${GATEWAY:-https://localhost:5000}"

if [ -z "${TENANT_A:-}" ] || [ -z "${TENANT_B:-}" ]; then
  echo "Faltan TENANT_A y TENANT_B. Ejemplo: TENANT_A=<guid> TENANT_B=<guid> $0"
  exit 2
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

fallos=0
ok() { echo "OK    $1"; }
fallo() {
  echo "FALLO $1"
  fallos=$((fallos + 1))
}

guid() {
  cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "00000000-0000-4000-8000-00000000000$RANDOM"
}

CODIGO=""
peticion() {
  # método, ruta, tenant y JSON opcional; deja el código en CODIGO y el cuerpo en $TMP/cuerpo
  local metodo="$1" ruta="$2" tenant="$3" datos="${4:-}"
  local args=(-ksS -X "$metodo" -o "$TMP/cuerpo" -D "$TMP/cab" -w '%{http_code}'
    -H "X-Tenant-Id: $tenant")
  if [ -n "$datos" ]; then
    args+=(-H "Content-Type: application/json" --data "$datos")
  fi
  CODIGO="$(curl "${args[@]}" "$GATEWAY$ruta")"
  if [ -z "$CODIGO" ]; then
    CODIGO=000
  fi
}

HORA="$(date +%H%M%S)"
CAT_1="$(guid)"
CAT_2="$(guid)"
CANTIDAD=250000

# 1. Crear con el tenant A
peticion POST /api/budgets "$TENANT_A" \
  "{\"CategoryId\":\"$CAT_1\",\"AmountCents\":$CANTIDAD,\"Period\":\"monthly\"}"
if [ "$CODIGO" = "201" ] && grep -qi "^location:" "$TMP/cab"; then
  ok "POST /api/budgets crea con 201 y Location"
else
  fallo "POST /api/budgets esperaba 201 con Location y llegó $CODIGO"
fi
ID="$(sed -n 's/.*"id":"\([^"]*\)".*/\1/p' "$TMP/cuerpo")"
if [ -z "$ID" ]; then
  fallo "la respuesta no trae el id del presupuesto creado"
  echo "Resumen: fallos=$fallos"
  exit 1
fi

# 2. Listar y consultar por id
peticion GET "/api/budgets" "$TENANT_A"
grep -q "$ID" "$TMP/cuerpo" && ok "GET /api/budgets lista el registro" \
  || fallo "GET /api/budgets no incluye $ID"

peticion GET "/api/budgets/$ID" "$TENANT_A"
if [ "$CODIGO" = "200" ] && grep -q "\"amountCents\":$CANTIDAD" "$TMP/cuerpo"; then
  ok "GET /api/budgets/{id} devuelve los datos"
else
  fallo "GET /api/budgets/{id} no devolvió los datos (código $CODIGO)"
fi

# Filtro por período
peticion GET "/api/budgets?period=monthly" "$TENANT_A"
grep -q "$ID" "$TMP/cuerpo" && ok "GET /api/budgets?period=monthly incluye el registro" \
  || fallo "GET /api/budgets?period=monthly no incluye $ID"

# 3. Actualizar
peticion PUT "/api/budgets/$ID" "$TENANT_A" \
  "{\"CategoryId\":\"$CAT_1\",\"AmountCents\":300000,\"Period\":\"monthly\",\"IsArchived\":false}"
if [ "$CODIGO" = "200" ] && grep -q '"amountCents":300000' "$TMP/cuerpo"; then
  ok "PUT /api/budgets/{id} actualiza con 200"
else
  fallo "PUT /api/budgets/{id} no actualizó (código $CODIGO)"
fi

# 4. El tenant B no ve el registro del tenant A
peticion GET "/api/budgets" "$TENANT_B"
grep -q "$ID" "$TMP/cuerpo" && fallo "el tenant B ve el registro del tenant A" \
  || ok "el tenant B no ve el registro del tenant A"

peticion GET "/api/budgets/$ID" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "GET del tenant B responde 404" \
  || fallo "GET del tenant B esperaba 404 y llegó $CODIGO"

# 5. Validaciones, 404 y reglas de negocio
peticion POST /api/budgets "$TENANT_A" \
  "{\"CategoryId\":\"$CAT_1\",\"AmountCents\":0,\"Period\":\"monthly\"}"
[ "$CODIGO" = "400" ] && ok "POST con AmountCents 0 responde 400" \
  || fallo "POST con AmountCents 0 esperaba 400 y llegó $CODIGO"

peticion POST /api/budgets "$TENANT_A" \
  "{\"CategoryId\":\"$CAT_1\",\"AmountCents\":100,\"Period\":7}"
[ "$CODIGO" = "400" ] && ok "POST con período inválido responde 400" \
  || fallo "POST con período inválido esperaba 400 y llegó $CODIGO"

peticion GET "/api/budgets/$(guid)" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "GET de un id inexistente responde 404" \
  || fallo "GET de un id inexistente esperaba 404 y llegó $CODIGO"

peticion POST /api/budgets "$TENANT_A" \
  "{\"CategoryId\":\"$CAT_1\",\"AmountCents\":999,\"Period\":\"monthly\"}"
[ "$CODIGO" = "409" ] && ok "POST repetido (misma categoría y período) responde 409" \
  || fallo "POST repetido esperaba 409 y llegó $CODIGO"

# Segundo presupuesto para probar el 409 del PUT
peticion POST /api/budgets "$TENANT_A" \
  "{\"CategoryId\":\"$CAT_2\",\"AmountCents\":150000,\"Period\":\"monthly\"}"
ID_2="$(sed -n 's/.*"id":"\([^"]*\)".*/\1/p' "$TMP/cuerpo")"
[ "$CODIGO" = "201" ] && ok "POST crea un segundo presupuesto (201)" \
  || fallo "POST del segundo presupuesto esperaba 201 y llegó $CODIGO"

if [ -n "$ID_2" ]; then
  peticion PUT "/api/budgets/$ID_2" "$TENANT_A" \
    "{\"CategoryId\":\"$CAT_1\",\"AmountCents\":150000,\"Period\":\"monthly\",\"IsArchived\":false}"
  [ "$CODIGO" = "409" ] && ok "PUT a una categoría y período ya usados responde 409" \
    || fallo "PUT en conflicto esperaba 409 y llegó $CODIGO"
fi

# 6. Baja por archivado
peticion DELETE "/api/budgets/$ID" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "DELETE /api/budgets/{id} responde 204" \
  || fallo "DELETE esperaba 204 y llegó $CODIGO"

peticion GET "/api/budgets" "$TENANT_A"
grep -q "$ID" "$TMP/cuerpo" && fallo "el registro sigue en la lista después del DELETE" \
  || ok "el registro ya no aparece en la lista"

peticion GET "/api/budgets?includeArchived=true" "$TENANT_A"
grep -q "$ID" "$TMP/cuerpo" && ok "includeArchived=true muestra el registro archivado" \
  || fallo "includeArchived=true no muestra el registro archivado"

# 7. Limpieza: todo lo creado queda dado de baja
if [ -n "$ID_2" ]; then
  peticion DELETE "/api/budgets/$ID_2" "$TENANT_A"
  [ "$CODIGO" = "204" ] && ok "limpieza: segundo presupuesto dado de baja" \
    || fallo "limpieza: DELETE del segundo presupuesto devolvió $CODIGO"
fi

echo "Hora $HORA - tenant A $TENANT_A - resumen: fallos=$fallos"
if [ "$fallos" -gt 0 ]; then
  exit 1
fi
echo "Todas las comprobaciones de budgets pasaron."
