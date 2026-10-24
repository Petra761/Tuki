#!/usr/bin/env bash
# Pruebas del CRUD de account-types, accounts y categories contra el Gateway.
# Uso: TENANT_A=... TENANT_B=... ./scripts/pruebas-cuentas.sh
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

id_respuesta() { sed -n 's/.*"id":"\([^"]*\)".*/\1/p' "$TMP/cuerpo"; }

TIPO_TARJETA="00000000-0000-0000-0000-000000000002"
CUENTA_EFECTIVO="00000000-0000-0000-0000-0000000000a1"
CAT_SUELDO="00000000-0000-0000-0000-000000000108"
HORA="$(date +%H%M%S)"

# ---------------------------------------------------------------- Tipos de cuenta
NOMBRE_TIPO="Prueba tipo $HORA"
peticion POST /api/account-types "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_TIPO\",\"IsPerson\":false}"
if [ "$CODIGO" = "201" ] && grep -qi "^location:" "$TMP/cab"; then
  ok "POST /api/account-types crea con 201 y Location"
else
  fallo "POST /api/account-types esperaba 201 con Location y llegó $CODIGO"
fi
TIPO="$(id_respuesta)"

peticion GET /api/account-types "$TENANT_A"
grep -q "$TIPO" "$TMP/cuerpo" && ok "GET /api/account-types lista el registro" \
  || fallo "GET /api/account-types no incluye $TIPO"

peticion GET "/api/account-types/$TIPO" "$TENANT_A"
grep -q "$NOMBRE_TIPO" "$TMP/cuerpo" && ok "GET /api/account-types/{id} devuelve los datos" \
  || fallo "GET /api/account-types/{id} no devolvió los datos (código $CODIGO)"

peticion PUT "/api/account-types/$TIPO" "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_TIPO editado\",\"IsPerson\":false,\"IsArchived\":false}"
if [ "$CODIGO" = "200" ] && grep -q "editado" "$TMP/cuerpo"; then
  ok "PUT /api/account-types/{id} actualiza con 200"
else
  fallo "PUT /api/account-types/{id} no actualizó (código $CODIGO)"
fi

peticion GET /api/account-types "$TENANT_B"
grep -q "$TIPO" "$TMP/cuerpo" && fallo "el tenant B ve el tipo de cuenta del tenant A" \
  || ok "el tenant B no ve el tipo de cuenta del tenant A"
peticion GET "/api/account-types/$TIPO" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "GET del tenant B responde 404" \
  || fallo "GET del tenant B esperaba 404 y llegó $CODIGO"

peticion POST /api/account-types "$TENANT_A" '{"Name":"","IsPerson":false}'
[ "$CODIGO" = "400" ] && ok "POST con nombre vacío responde 400" \
  || fallo "POST con nombre vacío esperaba 400 y llegó $CODIGO"
LARGO="$(printf 'x%.0s' {1..150})"
peticion POST /api/account-types "$TENANT_A" "{\"Name\":\"$LARGO\",\"IsPerson\":false}"
[ "$CODIGO" = "400" ] && ok "POST con nombre de 150 caracteres responde 400" \
  || fallo "POST con nombre largo esperaba 400 y llegó $CODIGO"

peticion GET "/api/account-types/$(guid)" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "GET de un id inexistente responde 404" \
  || fallo "GET de un id inexistente esperaba 404 y llegó $CODIGO"

peticion POST /api/account-types "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_TIPO\",\"IsPerson\":false}"
[ "$CODIGO" = "409" ] && ok "POST de un nombre repetido responde 409" \
  || fallo "POST de un nombre repetido esperaba 409 y llegó $CODIGO"

peticion DELETE "/api/account-types/$TIPO" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "DELETE /api/account-types/{id} responde 204" \
  || fallo "DELETE del tipo esperaba 204 y llegó $CODIGO"
peticion GET /api/account-types "$TENANT_A"
grep -q "$TIPO" "$TMP/cuerpo" && fallo "el tipo sigue en la lista después del DELETE" \
  || ok "el tipo ya no aparece en la lista"
peticion GET "/api/account-types?includeArchived=true" "$TENANT_A"
grep -q "$TIPO" "$TMP/cuerpo" && ok "includeArchived=true muestra el tipo archivado" \
  || fallo "includeArchived=true no muestra el tipo archivado"

# --------------------------------------------------------------------- Cuentas
NOMBRE_CUENTA="Prueba cuenta $HORA"
peticion POST /api/accounts "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_CUENTA\",\"AccountTypeId\":\"$TIPO_TARJETA\"}"
[ "$CODIGO" = "201" ] && ok "POST /api/accounts crea con 201" \
  || fallo "POST /api/accounts esperaba 201 y llegó $CODIGO"
CUENTA="$(id_respuesta)"

peticion GET /api/accounts "$TENANT_A"
grep -q "$CUENTA" "$TMP/cuerpo" && ok "GET /api/accounts lista el registro" \
  || fallo "GET /api/accounts no incluye $CUENTA"
peticion GET "/api/accounts/$CUENTA" "$TENANT_A"
grep -q "$NOMBRE_CUENTA" "$TMP/cuerpo" && ok "GET /api/accounts/{id} devuelve los datos" \
  || fallo "GET /api/accounts/{id} no devolvió los datos (código $CODIGO)"

peticion PUT "/api/accounts/$CUENTA" "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_CUENTA editada\",\"AccountTypeId\":\"$TIPO_TARJETA\",\"IsDefault\":false,\"IsArchived\":false}"
[ "$CODIGO" = "200" ] && ok "PUT /api/accounts/{id} actualiza con 200" \
  || fallo "PUT /api/accounts/{id} no actualizó (código $CODIGO)"

# Única cuenta predeterminada: al marcar otra, la anterior pierde la marca
peticion POST /api/accounts "$TENANT_A" \
  "{\"Name\":\"Prueba cuenta $HORA 2\",\"AccountTypeId\":\"$TIPO_TARJETA\"}"
CUENTA_2="$(id_respuesta)"
peticion PUT "/api/accounts/$CUENTA_2" "$TENANT_A" \
  "{\"Name\":\"Prueba cuenta $HORA 2\",\"AccountTypeId\":\"$TIPO_TARJETA\",\"IsDefault\":true,\"IsArchived\":false}"
[ "$CODIGO" = "200" ] && ok "PUT marca una segunda cuenta como predeterminada" \
  || fallo "PUT de la segunda cuenta esperaba 200 y llegó $CODIGO"
peticion GET /api/accounts "$TENANT_A"
PRED="$(grep -o '"isDefault":true' "$TMP/cuerpo" | wc -l)"
if [ "$PRED" = "1" ]; then
  ok "solo hay una cuenta predeterminada después del cambio"
else
  fallo "se esperaba 1 cuenta predeterminada y hay $PRED"
fi

peticion GET /api/accounts "$TENANT_B"
grep -q "$CUENTA" "$TMP/cuerpo" && fallo "el tenant B ve la cuenta del tenant A" \
  || ok "el tenant B no ve la cuenta del tenant A"
peticion GET "/api/accounts/$CUENTA" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "GET de cuenta del tenant B responde 404" \
  || fallo "GET de cuenta del tenant B esperaba 404 y llegó $CODIGO"

peticion POST /api/accounts "$TENANT_A" '{"Name":"","AccountTypeId":"'"$TIPO_TARJETA"'"}'
[ "$CODIGO" = "400" ] && ok "POST de cuenta sin nombre responde 400" \
  || fallo "POST de cuenta sin nombre esperaba 400 y llegó $CODIGO"
peticion POST /api/accounts "$TENANT_A" \
  "{\"Name\":\"Prueba sin tipo\",\"AccountTypeId\":\"$(guid)\"}"
[ "$CODIGO" = "400" ] && ok "POST con tipo de cuenta inexistente responde 400" \
  || fallo "POST con tipo inexistente esperaba 400 y llegó $CODIGO"
if [ -n "$TIPO" ]; then
  peticion POST /api/accounts "$TENANT_A" \
    "{\"Name\":\"Prueba tipo archivado\",\"AccountTypeId\":\"$TIPO\"}"
  [ "$CODIGO" = "400" ] && ok "POST con tipo de cuenta archivado responde 400" \
    || fallo "POST con tipo archivado esperaba 400 y llegó $CODIGO"
else
  fallo "sin tipo archivado para probar el alta con tipo archivado"
fi

peticion GET "/api/accounts/$(guid)" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "GET de cuenta inexistente responde 404" \
  || fallo "GET de cuenta inexistente esperaba 404 y llegó $CODIGO"

peticion DELETE "/api/accounts/$CUENTA" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "DELETE /api/accounts/{id} responde 204" \
  || fallo "DELETE de la cuenta esperaba 204 y llegó $CODIGO"
peticion GET /api/accounts "$TENANT_A"
grep -q "$CUENTA" "$TMP/cuerpo" && fallo "la cuenta sigue en la lista después del DELETE" \
  || ok "la cuenta ya no aparece en la lista"
peticion GET "/api/accounts?includeArchived=true" "$TENANT_A"
grep -q "$CUENTA" "$TMP/cuerpo" && ok "includeArchived=true muestra la cuenta archivada" \
  || fallo "includeArchived=true no muestra la cuenta archivada"

# ---------------------------------------------------------------- Categorías
NOMBRE_CAT="Prueba cat $HORA"
peticion POST /api/categories "$TENANT_A" "{\"Name\":\"$NOMBRE_CAT\",\"Kind\":\"Expense\"}"
[ "$CODIGO" = "201" ] && ok "POST /api/categories crea una categoría raíz con 201" \
  || fallo "POST /api/categories esperaba 201 y llegó $CODIGO"
CAT="$(id_respuesta)"

peticion POST /api/categories "$TENANT_A" \
  "{\"Name\":\"Prueba sub $HORA\",\"Kind\":\"Expense\",\"ParentId\":\"$CAT\"}"
[ "$CODIGO" = "201" ] && ok "POST crea una subcategoría con 201" \
  || fallo "POST de subcategoría esperaba 201 y llegó $CODIGO"
SUB="$(id_respuesta)"

peticion GET /api/categories "$TENANT_A"
if grep -q "$CAT" "$TMP/cuerpo" && grep -q "$SUB" "$TMP/cuerpo"; then
  ok "GET /api/categories lista las dos categorías"
else
  fallo "GET /api/categories no lista las categorías creadas"
fi
peticion GET "/api/categories/$CAT" "$TENANT_A"
grep -q "$NOMBRE_CAT" "$TMP/cuerpo" && ok "GET /api/categories/{id} devuelve los datos" \
  || fallo "GET /api/categories/{id} no devolvió los datos (código $CODIGO)"

peticion PUT "/api/categories/$SUB" "$TENANT_A" \
  "{\"Name\":\"Prueba sub $HORA editada\",\"Kind\":\"Expense\",\"ParentId\":\"$CAT\",\"IsArchived\":false}"
[ "$CODIGO" = "200" ] && ok "PUT /api/categories/{id} actualiza con 200" \
  || fallo "PUT /api/categories/{id} no actualizó (código $CODIGO)"

peticion GET /api/categories "$TENANT_B"
grep -q "$CAT" "$TMP/cuerpo" && fallo "el tenant B ve la categoría del tenant A" \
  || ok "el tenant B no ve la categoría del tenant A"
peticion GET "/api/categories/$CAT" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "GET de categoría del tenant B responde 404" \
  || fallo "GET de categoría del tenant B esperaba 404 y llegó $CODIGO"

peticion POST /api/categories "$TENANT_A" '{"Name":"Tipo inválido","Kind":9}'
[ "$CODIGO" = "400" ] && ok "POST con Kind inválido responde 400" \
  || fallo "POST con Kind inválido esperaba 400 y llegó $CODIGO"
peticion POST /api/categories "$TENANT_A" '{"Name":"","Kind":"Expense"}'
[ "$CODIGO" = "400" ] && ok "POST con nombre vacío responde 400" \
  || fallo "POST con nombre vacío esperaba 400 y llegó $CODIGO"
peticion POST /api/categories "$TENANT_A" \
  "{\"Name\":\"Padre inexistente\",\"Kind\":\"Expense\",\"ParentId\":\"$(guid)\"}"
[ "$CODIGO" = "400" ] && ok "POST con padre inexistente responde 400" \
  || fallo "POST con padre inexistente esperaba 400 y llegó $CODIGO"
peticion POST /api/categories "$TENANT_A" \
  "{\"Name\":\"Padre de otro tipo\",\"Kind\":\"Expense\",\"ParentId\":\"$CAT_SUELDO\"}"
[ "$CODIGO" = "400" ] && ok "POST con padre de otro tipo responde 400" \
  || fallo "POST con padre de otro tipo esperaba 400 y llegó $CODIGO"
peticion POST /api/categories "$TENANT_A" \
  "{\"Name\":\"Segundo nivel\",\"Kind\":\"Expense\",\"ParentId\":\"$SUB\"}"
[ "$CODIGO" = "400" ] && ok "POST con padre de segundo nivel responde 400" \
  || fallo "POST con padre de segundo nivel esperaba 400 y llegó $CODIGO"

peticion GET "/api/categories/$(guid)" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "GET de categoría inexistente responde 404" \
  || fallo "GET de categoría inexistente esperaba 404 y llegó $CODIGO"

peticion DELETE "/api/categories/$CAT" "$TENANT_A"
[ "$CODIGO" = "409" ] && ok "DELETE de categoría con subcategoría activa responde 409" \
  || fallo "DELETE con subcategoría activa esperaba 409 y llegó $CODIGO"

# Limpieza: se archiva primero la subcategoría y luego la categoría raíz
peticion DELETE "/api/categories/$SUB" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "limpieza: subcategoría dada de baja" \
  || fallo "limpieza: DELETE de la subcategoría devolvió $CODIGO"
peticion DELETE "/api/categories/$CAT" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "limpieza: categoría raíz dada de baja" \
  || fallo "limpieza: DELETE de la categoría raíz devolvió $CODIGO"
peticion GET /api/categories "$TENANT_A"
if grep -q "$CAT" "$TMP/cuerpo" || grep -q "$SUB" "$TMP/cuerpo"; then
  fallo "las categorías creadas siguen en la lista"
else
  ok "las categorías creadas ya no aparecen en la lista"
fi

# ------------------------------------------------- Limpieza de cuentas y tipo
if [ -n "$CUENTA_2" ]; then
  peticion PUT "/api/accounts/$CUENTA_2" "$TENANT_A" \
    "{\"Name\":\"Prueba cuenta $HORA 2\",\"AccountTypeId\":\"$TIPO_TARJETA\",\"IsDefault\":false,\"IsArchived\":true}"
  [ "$CODIGO" = "200" ] && ok "limpieza: segunda cuenta dada de baja y sin ser predeterminada" \
    || fallo "limpieza: PUT de la segunda cuenta devolvió $CODIGO"
fi
# Se devuelve la marca de predeterminada a la cuenta Efectivo sembrada
peticion PUT "/api/accounts/$CUENTA_EFECTIVO" "$TENANT_A" \
  "{\"Name\":\"Efectivo\",\"AccountTypeId\":\"00000000-0000-0000-0000-000000000001\",\"IsDefault\":true,\"IsArchived\":false}"
[ "$CODIGO" = "200" ] && ok "limpieza: Efectivo vuelve a ser la cuenta predeterminada" \
  || fallo "limpieza: PUT de Efectivo devolvió $CODIGO"

echo "Hora $HORA - tenant A $TENANT_A - resumen: fallos=$fallos"
if [ "$fallos" -gt 0 ]; then
  exit 1
fi
echo "Todas las comprobaciones de Cuentas pasaron."
