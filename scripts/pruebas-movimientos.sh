#!/usr/bin/env bash
# Pruebas del CRUD de tags y shortcuts contra el Gateway.
# Uso: ./scripts/pruebas-movimientos.sh   (la Parte 4 amplía este script)
set -u

GATEWAY="${GATEWAY:-https://localhost:5000}"

# Usuarios de prueba de la tarea 5 (registrados en el Administrador); se pueden
# sobreescribir por entorno para correr las pruebas contra otros tenants
TENANT_A="${TENANT_A:-890d174a-0c1f-4945-b6c5-bbe38d169d1f}"
TENANT_B="${TENANT_B:-3ad791be-7905-4252-81b6-7ff62cbb890e}"

if [ -z "$TENANT_A" ] || [ -z "$TENANT_B" ]; then
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

HORA="$(date +%H%M%S)"

# ------------------------------------------------------------------- Marcadores
NOMBRE_TAG="Prueba tag $HORA"
peticion POST /api/tags "$TENANT_A" "{\"Name\":\"$NOMBRE_TAG\"}"
if [ "$CODIGO" = "201" ] && grep -qi "^location:" "$TMP/cab"; then
  ok "POST /api/tags crea con 201 y Location"
else
  fallo "POST /api/tags esperaba 201 con Location y llegó $CODIGO"
fi
TAG="$(id_respuesta)"

peticion GET /api/tags "$TENANT_A"
grep -q "$TAG" "$TMP/cuerpo" && ok "GET /api/tags lista el registro" \
  || fallo "GET /api/tags no incluye $TAG"

peticion GET "/api/tags/$TAG" "$TENANT_A"
grep -q "$NOMBRE_TAG" "$TMP/cuerpo" && ok "GET /api/tags/{id} devuelve los datos" \
  || fallo "GET /api/tags/{id} no devolvió los datos (código $CODIGO)"

peticion PUT "/api/tags/$TAG" "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_TAG editado\",\"IsArchived\":false}"
if [ "$CODIGO" = "200" ] && grep -q "editado" "$TMP/cuerpo"; then
  ok "PUT /api/tags/{id} actualiza con 200"
else
  fallo "PUT /api/tags/{id} no actualizó (código $CODIGO)"
fi

peticion GET /api/tags "$TENANT_B"
grep -q "$TAG" "$TMP/cuerpo" && fallo "el tenant B ve el marcador del tenant A" \
  || ok "el tenant B no ve el marcador del tenant A"
peticion GET "/api/tags/$TAG" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "GET del tenant B responde 404" \
  || fallo "GET del tenant B esperaba 404 y llegó $CODIGO"

peticion POST /api/tags "$TENANT_A" '{"Name":""}'
[ "$CODIGO" = "400" ] && ok "POST con nombre vacío responde 400" \
  || fallo "POST con nombre vacío esperaba 400 y llegó $CODIGO"
LARGO="$(printf 'x%.0s' {1..60})"
peticion POST /api/tags "$TENANT_A" "{\"Name\":\"$LARGO\"}"
[ "$CODIGO" = "400" ] && ok "POST con nombre de 60 caracteres responde 400" \
  || fallo "POST con nombre largo esperaba 400 y llegó $CODIGO"

peticion GET "/api/tags/$(guid)" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "GET de un id inexistente responde 404" \
  || fallo "GET de un id inexistente esperaba 404 y llegó $CODIGO"

# Nombre repetido sin distinguir mayúsculas
peticion POST /api/tags "$TENANT_A" "{\"Name\":\"$(echo "$NOMBRE_TAG editado" | tr '[:lower:]' '[:upper:]')\"}"
[ "$CODIGO" = "409" ] && ok "POST repetido sin distinguir mayúsculas responde 409" \
  || fallo "POST repetido esperaba 409 y llegó $CODIGO"

# Segundo marcador para probar el 409 del PUT
peticion POST /api/tags "$TENANT_A" "{\"Name\":\"Prueba tag 2 $HORA\"}"
TAG_2="$(id_respuesta)"
[ "$CODIGO" = "201" ] && ok "POST crea un segundo marcador (201)" \
  || fallo "POST del segundo marcador esperaba 201 y llegó $CODIGO"
if [ -n "$TAG_2" ]; then
  peticion PUT "/api/tags/$TAG_2" "$TENANT_A" \
    "{\"Name\":\"$NOMBRE_TAG editado\",\"IsArchived\":false}"
  [ "$CODIGO" = "409" ] && ok "PUT a un nombre ya usado responde 409" \
    || fallo "PUT en conflicto esperaba 409 y llegó $CODIGO"
fi

peticion DELETE "/api/tags/$TAG" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "DELETE /api/tags/{id} responde 204" \
  || fallo "DELETE del marcador esperaba 204 y llegó $CODIGO"
peticion GET /api/tags "$TENANT_A"
grep -q "$TAG" "$TMP/cuerpo" && fallo "el marcador sigue en la lista después del DELETE" \
  || ok "el marcador ya no aparece en la lista"
peticion GET "/api/tags?includeArchived=true" "$TENANT_A"
grep -q "$TAG" "$TMP/cuerpo" && ok "includeArchived=true muestra el marcador archivado" \
  || fallo "includeArchived=true no muestra el marcador archivado"

# --------------------------------------------------------------------- Atajos
# Los ids pertenecen a Cuentas (categoría Alimentación y cuenta Efectivo);
# este servicio los guarda como identificador sin validarlos
CAT_ALIMENTACION="00000000-0000-0000-0000-000000000101"
CUENTA_EFECTIVO="00000000-0000-0000-0000-0000000000a1"

NOMBRE_ATAJO="Prueba atajo $HORA"
peticion POST /api/shortcuts "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_ATAJO\",\"AmountCents\":4050,\"CategoryId\":\"$CAT_ALIMENTACION\",\"AccountId\":\"$CUENTA_EFECTIVO\",\"SortOrder\":1}"
if [ "$CODIGO" = "201" ] && grep -qi "^location:" "$TMP/cab"; then
  ok "POST /api/shortcuts crea con 201 y Location"
else
  fallo "POST /api/shortcuts esperaba 201 con Location y llegó $CODIGO"
fi
ATAJO="$(id_respuesta)"

peticion GET /api/shortcuts "$TENANT_A"
grep -q "$ATAJO" "$TMP/cuerpo" && ok "GET /api/shortcuts lista el registro" \
  || fallo "GET /api/shortcuts no incluye $ATAJO"

peticion GET "/api/shortcuts/$ATAJO" "$TENANT_A"
if [ "$CODIGO" = "200" ] && grep -q '"amountCents":4050' "$TMP/cuerpo"; then
  ok "GET /api/shortcuts/{id} devuelve los datos"
else
  fallo "GET /api/shortcuts/{id} no devolvió los datos (código $CODIGO)"
fi

peticion PUT "/api/shortcuts/$ATAJO" "$TENANT_A" \
  "{\"Name\":\"$NOMBRE_ATAJO editado\",\"AmountCents\":5100,\"CategoryId\":\"$CAT_ALIMENTACION\",\"AccountId\":\"$CUENTA_EFECTIVO\",\"SortOrder\":2,\"IsArchived\":false}"
if [ "$CODIGO" = "200" ] && grep -q '"amountCents":5100' "$TMP/cuerpo"; then
  ok "PUT /api/shortcuts/{id} actualiza con 200"
else
  fallo "PUT /api/shortcuts/{id} no actualizó (código $CODIGO)"
fi

peticion GET /api/shortcuts "$TENANT_B"
grep -q "$ATAJO" "$TMP/cuerpo" && fallo "el tenant B ve el atajo del tenant A" \
  || ok "el tenant B no ve el atajo del tenant A"
peticion GET "/api/shortcuts/$ATAJO" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "GET del tenant B responde 404" \
  || fallo "GET del tenant B esperaba 404 y llegó $CODIGO"

peticion POST /api/shortcuts "$TENANT_A" \
  "{\"Name\":\"Monto cero\",\"AmountCents\":0,\"CategoryId\":\"$CAT_ALIMENTACION\",\"AccountId\":\"$CUENTA_EFECTIVO\"}"
[ "$CODIGO" = "400" ] && ok "POST con AmountCents 0 responde 400" \
  || fallo "POST con AmountCents 0 esperaba 400 y llegó $CODIGO"

peticion POST /api/shortcuts "$TENANT_A" \
  "{\"Name\":\"\",\"AmountCents\":100,\"CategoryId\":\"$CAT_ALIMENTACION\",\"AccountId\":\"$CUENTA_EFECTIVO\"}"
[ "$CODIGO" = "400" ] && ok "POST sin nombre responde 400" \
  || fallo "POST sin nombre esperaba 400 y llegó $CODIGO"

peticion POST /api/shortcuts "$TENANT_A" \
  "{\"Name\":\"Sin cuenta\",\"AmountCents\":100,\"CategoryId\":\"$CAT_ALIMENTACION\",\"AccountId\":\"00000000-0000-0000-0000-000000000000\"}"
[ "$CODIGO" = "400" ] && ok "POST sin cuenta responde 400" \
  || fallo "POST sin cuenta esperaba 400 y llegó $CODIGO"

peticion GET "/api/shortcuts/$(guid)" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "GET de un atajo inexistente responde 404" \
  || fallo "GET de un atajo inexistente esperaba 404 y llegó $CODIGO"

peticion DELETE "/api/shortcuts/$ATAJO" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "DELETE /api/shortcuts/{id} responde 204" \
  || fallo "DELETE del atajo esperaba 204 y llegó $CODIGO"
peticion GET /api/shortcuts "$TENANT_A"
grep -q "$ATAJO" "$TMP/cuerpo" && fallo "el atajo sigue en la lista después del DELETE" \
  || ok "el atajo ya no aparece en la lista"
peticion GET "/api/shortcuts?includeArchived=true" "$TENANT_A"
grep -q "$ATAJO" "$TMP/cuerpo" && ok "includeArchived=true muestra el atajo archivado" \
  || fallo "includeArchived=true no muestra el atajo archivado"

# ------------------------------------------------------------------ Limpieza
if [ -n "$TAG_2" ]; then
  peticion DELETE "/api/tags/$TAG_2" "$TENANT_A"
  [ "$CODIGO" = "204" ] && ok "limpieza: segundo marcador dado de baja" \
    || fallo "limpieza: DELETE del segundo marcador devolvió $CODIGO"
fi

echo "Hora $HORA - tenant A $TENANT_A - resumen: fallos=$fallos"
if [ "$fallos" -gt 0 ]; then
  exit 1
fi
echo "Todas las comprobaciones de tags y shortcuts pasaron."
