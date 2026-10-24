#!/usr/bin/env bash
# Pruebas del CRUD de tags, shortcuts y transactions contra el Gateway.
# Uso: ./scripts/pruebas-movimientos.sh
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

# Primer "id" del cuerpo: en transacciones el cuerpo trae también los ids de
# las líneas y un .* codicioso se quedaría con el último
id_respuesta() {
  grep -o '"id":"[^"]*"' "$TMP/cuerpo" | head -1 | sed 's/.*"\([^"]*\)"$/\1/'
}

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

# ------------------------------------------------------------ Transacciones
# Cuentas propias de esta sección: así las líneas no dependen de las sembradas
FECHA="$(date +%F)"
AYER="$(date -d 'yesterday' +%F 2>/dev/null || date -v-1d +%F)"

peticion POST /api/accounts "$TENANT_A" \
  "{\"Name\":\"Cuenta mov 1 $HORA\",\"AccountTypeId\":\"00000000-0000-0000-0000-000000000001\"}"
[ "$CODIGO" = "201" ] && ok "POST crea la cuenta 1 para las transacciones (201)" \
  || fallo "crear la cuenta 1 esperaba 201 y llegó $CODIGO"
CUENTA_1="$(id_respuesta)"

peticion POST /api/accounts "$TENANT_A" \
  "{\"Name\":\"Cuenta mov 2 $HORA\",\"AccountTypeId\":\"00000000-0000-0000-0000-000000000001\"}"
[ "$CODIGO" = "201" ] && ok "POST crea la cuenta 2 para las transacciones (201)" \
  || fallo "crear la cuenta 2 esperaba 201 y llegó $CODIGO"
CUENTA_2="$(id_respuesta)"

peticion POST /api/tags "$TENANT_A" "{\"Name\":\"Prueba tag trans $HORA\"}"
[ "$CODIGO" = "201" ] && ok "POST crea el marcador de las transacciones (201)" \
  || fallo "crear el marcador de las transacciones esperaba 201 y llegó $CODIGO"
TAG_MOV="$(id_respuesta)"

# Gasto con dos líneas y un marcador: el tipo se deduce de las líneas
GASTO_DESC="Compra mercado $HORA"
GASTO_BODY="{\"Description\":\"$GASTO_DESC\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":-4050},{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":-150}],\"TagIds\":[\"$TAG_MOV\"]}"
peticion POST /api/transactions "$TENANT_A" "$GASTO_BODY"
if [ "$CODIGO" = "201" ] && grep -qi "^location:" "$TMP/cab" && grep -q '"type":"Expense"' "$TMP/cuerpo"; then
  ok "POST crea un gasto con 2 líneas y marcador (201 y type Expense)"
else
  fallo "POST de gasto esperaba 201 con type Expense y llegó $CODIGO"
fi
GASTO="$(id_respuesta)"

peticion GET /api/transactions "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && ok "GET /api/transactions lista el registro" \
  || fallo "GET /api/transactions no incluye $GASTO"

peticion GET "/api/transactions/$GASTO" "$TENANT_A"
if [ "$CODIGO" = "200" ] && grep -q "$GASTO_DESC" "$TMP/cuerpo" &&
  grep -q "\"amountCents\":-4050" "$TMP/cuerpo" && grep -q "\"amountCents\":-150" "$TMP/cuerpo" &&
  grep -q "$TAG_MOV" "$TMP/cuerpo"; then
  ok "GET /api/transactions/{id} devuelve líneas y marcador"
else
  fallo "GET /api/transactions/{id} no devolvió las líneas y el marcador (código $CODIGO)"
fi

# Filtros del listado
peticion GET "/api/transactions?search=$HORA" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && ok "filtro search encuentra la descripción" \
  || fallo "filtro search no encontró la transacción"
peticion GET "/api/transactions?search=zzzsincoincidencia$HORA" "$TENANT_A"
grep -q '"total":0' "$TMP/cuerpo" && ok "filtro search sin coincidencias da total 0" \
  || fallo "filtro search sin coincidencias no devolvió total 0"
peticion GET "/api/transactions?from=$FECHA" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && ok "filtro from devuelve la transacción de hoy" \
  || fallo "filtro from no incluyó la transacción"
peticion GET "/api/transactions?to=$AYER" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && fallo "filtro to de ayer incluye la transacción de hoy" \
  || ok "filtro to de ayer excluye la transacción de hoy"
peticion GET "/api/transactions?accountId=$CUENTA_1" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && ok "filtro accountId encuentra la transacción" \
  || fallo "filtro accountId no encontró la transacción"
peticion GET "/api/transactions?accountId=$CUENTA_2" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && fallo "filtro accountId de otra cuenta incluye el gasto" \
  || ok "filtro accountId de otra cuenta excluye el gasto"
peticion GET "/api/transactions?categoryId=$CAT_ALIMENTACION" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && ok "filtro categoryId encuentra la transacción" \
  || fallo "filtro categoryId no encontró la transacción"
peticion GET "/api/transactions?tagId=$TAG_MOV" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && ok "filtro tagId encuentra la transacción" \
  || fallo "filtro tagId no encontró la transacción"

# Paginación: pageSize 1 y tope máximo de 200
peticion GET "/api/transactions?pageSize=1&page=1" "$TENANT_A"
grep -q '"page":1,"pageSize":1,' "$TMP/cuerpo" && ok "pageSize=1 respeta el tamaño de página" \
  || fallo "pageSize=1 no devolvió pageSize 1"
peticion GET "/api/transactions?pageSize=999" "$TENANT_A"
grep -q '"pageSize":200,' "$TMP/cuerpo" && ok "pageSize=999 se limita a 200" \
  || fallo "pageSize=999 no se limitó a 200"

# Ingreso: una sola línea positiva con categoría
peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Sueldo $HORA\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_2\",\"CategoryId\":\"00000000-0000-0000-0000-000000000108\",\"AmountCents\":7000}]}"
if [ "$CODIGO" = "201" ] && grep -q '"type":"Income"' "$TMP/cuerpo"; then
  ok "POST crea un ingreso (201 y type Income)"
else
  fallo "POST de ingreso esperaba 201 con type Income y llegó $CODIGO"
fi
INGRESO="$(id_respuesta)"
[ -n "$INGRESO" ] && peticion DELETE "/api/transactions/$INGRESO" "$TENANT_A"

# Traslado válido e inválido
peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Traslado $HORA\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"AmountCents\":-1000},{\"AccountId\":\"$CUENTA_2\",\"AmountCents\":1000}]}"
if [ "$CODIGO" = "201" ] && grep -q '"type":"Transfer"' "$TMP/cuerpo"; then
  ok "POST crea un traslado que suma 0 (201 y type Transfer)"
else
  fallo "POST de traslado esperaba 201 con type Transfer y llegó $CODIGO"
fi
TRASLADO="$(id_respuesta)"

peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Traslado descuadrado\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"AmountCents\":-1000},{\"AccountId\":\"$CUENTA_2\",\"AmountCents\":900}]}"
[ "$CODIGO" = "400" ] && ok "POST de traslado que no suma 0 responde 400" \
  || fallo "traslado descuadrado esperaba 400 y llegó $CODIGO"

# Apertura: una sola línea sin categoría y una por cuenta
peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Apertura $HORA\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_2\",\"AmountCents\":5000}]}"
if [ "$CODIGO" = "201" ] && grep -q '"type":"Opening"' "$TMP/cuerpo"; then
  ok "POST crea una apertura (201 y type Opening)"
else
  fallo "POST de apertura esperaba 201 con type Opening y llegó $CODIGO"
fi
APERTURA="$(id_respuesta)"

peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Segunda apertura\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_2\",\"AmountCents\":100}]}"
[ "$CODIGO" = "409" ] && ok "segunda apertura en la misma cuenta responde 409" \
  || fallo "segunda apertura esperaba 409 y llegó $CODIGO"

# Validaciones
peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Sin líneas\",\"OccurredOn\":\"$FECHA\",\"Lines\":[]}"
[ "$CODIGO" = "400" ] && ok "POST sin líneas responde 400" \
  || fallo "POST sin líneas esperaba 400 y llegó $CODIGO"

peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Importe cero\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":0}]}"
[ "$CODIGO" = "400" ] && ok "POST con importe 0 responde 400" \
  || fallo "POST con importe 0 esperaba 400 y llegó $CODIGO"

peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Marcador inexistente\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":-100}],\"TagIds\":[\"$(guid)\"]}"
[ "$CODIGO" = "400" ] && ok "POST con marcador inexistente responde 400" \
  || fallo "marcador inexistente esperaba 400 y llegó $CODIGO"

peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Marcador repetido\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":-100}],\"TagIds\":[\"$TAG_MOV\",\"$TAG_MOV\"]}"
[ "$CODIGO" = "400" ] && ok "POST con marcador repetido responde 400" \
  || fallo "marcador repetido esperaba 400 y llegó $CODIGO"

# $TAG quedó archivado en la sección de marcadores
peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Marcador archivado\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":-100}],\"TagIds\":[\"$TAG\"]}"
[ "$CODIGO" = "400" ] && ok "POST con marcador archivado responde 400" \
  || fallo "marcador archivado esperaba 400 y llegó $CODIGO"

peticion POST /api/transactions "$TENANT_A" \
  "{\"Description\":\"Líneas mezcladas\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":-100},{\"AccountId\":\"$CUENTA_2\",\"AmountCents\":100}]}"
[ "$CODIGO" = "400" ] && ok "POST con líneas mezcladas responde 400" \
  || fallo "líneas mezcladas esperaba 400 y llegó $CODIGO"

# Aislamiento entre tenants
peticion GET "/api/transactions?search=$HORA" "$TENANT_B"
grep -q "$GASTO" "$TMP/cuerpo" && fallo "el tenant B ve la transacción del tenant A" \
  || ok "el tenant B no ve la transacción del tenant A"
peticion GET "/api/transactions/$GASTO" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "GET del tenant B responde 404" \
  || fallo "GET del tenant B esperaba 404 y llegó $CODIGO"
peticion PUT "/api/transactions/$GASTO" "$TENANT_B" "$GASTO_BODY"
[ "$CODIGO" = "404" ] && ok "PUT del tenant B responde 404" \
  || fallo "PUT del tenant B esperaba 404 y llegó $CODIGO"
peticion DELETE "/api/transactions/$GASTO" "$TENANT_B"
[ "$CODIGO" = "404" ] && ok "DELETE del tenant B responde 404" \
  || fallo "DELETE del tenant B esperaba 404 y llegó $CODIGO"

peticion GET "/api/transactions/$(guid)" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "GET de una transacción inexistente responde 404" \
  || fallo "GET inexistente esperaba 404 y llegó $CODIGO"

# Actualización: cambia la descripción y deja una sola línea
peticion PUT "/api/transactions/$GASTO" "$TENANT_A" \
  "{\"Description\":\"$GASTO_DESC editada\",\"OccurredOn\":\"$FECHA\",\"Lines\":[{\"AccountId\":\"$CUENTA_1\",\"CategoryId\":\"$CAT_ALIMENTACION\",\"AmountCents\":-2000}],\"TagIds\":[]}"
if [ "$CODIGO" = "200" ] && grep -q "editada" "$TMP/cuerpo" &&
  grep -q "\"amountCents\":-2000" "$TMP/cuerpo" &&
  ! grep -q "\"amountCents\":-150" "$TMP/cuerpo"; then
  ok "PUT /api/transactions/{id} reemplaza descripción, líneas y marcadores"
else
  fallo "PUT /api/transactions/{id} no reemplazó bien (código $CODIGO)"
fi

peticion DELETE "/api/transactions/$GASTO" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "DELETE /api/transactions/{id} responde 204" \
  || fallo "DELETE esperaba 204 y llegó $CODIGO"
peticion GET "/api/transactions/$GASTO" "$TENANT_A"
[ "$CODIGO" = "404" ] && ok "la transacción eliminada responde 404" \
  || fallo "la transacción eliminada esperaba 404 y llegó $CODIGO"
peticion GET "/api/transactions?search=$HORA" "$TENANT_A"
grep -q "$GASTO" "$TMP/cuerpo" && fallo "la transacción eliminada sigue en la lista" \
  || ok "la transacción eliminada ya no aparece en la lista"

# ------------------------------------------------------------------ Limpieza
if [ -n "$TAG_2" ]; then
  peticion DELETE "/api/tags/$TAG_2" "$TENANT_A"
  [ "$CODIGO" = "204" ] && ok "limpieza: segundo marcador dado de baja" \
    || fallo "limpieza: DELETE del segundo marcador devolvió $CODIGO"
fi

for TRANSACCION in "$TRASLADO" "$APERTURA"; do
  if [ -n "$TRANSACCION" ]; then
    peticion DELETE "/api/transactions/$TRANSACCION" "$TENANT_A"
    [ "$CODIGO" = "204" ] && ok "limpieza: transacción $TRANSACCION eliminada" \
      || fallo "limpieza: DELETE de $TRANSACCION devolvió $CODIGO"
  fi
done

peticion DELETE "/api/tags/$TAG_MOV" "$TENANT_A"
[ "$CODIGO" = "204" ] && ok "limpieza: marcador de las transacciones dado de baja" \
  || fallo "limpieza: DELETE del marcador devolvió $CODIGO"

for CUENTA in "$CUENTA_1" "$CUENTA_2"; do
  if [ -n "$CUENTA" ]; then
    peticion DELETE "/api/accounts/$CUENTA" "$TENANT_A"
    [ "$CODIGO" = "204" ] && ok "limpieza: cuenta $CUENTA dada de baja" \
      || fallo "limpieza: DELETE de $CUENTA devolvió $CODIGO"
  fi
done

echo "Hora $HORA - tenant A $TENANT_A - resumen: fallos=$fallos"
if [ "$fallos" -gt 0 ]; then
  exit 1
fi
echo "Todas las comprobaciones de tags, atajos y transacciones pasaron."
