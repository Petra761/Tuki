#!/usr/bin/env bash
# Pruebas de la Parte 5: planes con removes_ads y showAds en el estado de la
# suscripción, contra el Gateway. Las suscripciones y el estado leen el
# identificador del tenant en la cabecera X-Tenant-Id (la tabla de usuarios ya
# vive en Autenticación), que aquí se toma del registro.
# Uso: ./scripts/pruebas-administrador.sh
set -u

GATEWAY="${GATEWAY:-https://localhost:5000}"

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

# Tenant que viaja en X-Tenant-Id; vacío = no enviar la cabecera (para probar el 400)
TENANT=""

CODIGO=""
peticion() {
  # método, ruta y JSON opcional; deja el código en CODIGO y el cuerpo en $TMP/cuerpo
  local metodo="$1" ruta="$2" datos="${3:-}"
  local args=(-ksS -X "$metodo" -o "$TMP/cuerpo" -D "$TMP/cab" -w '%{http_code}')
  if [ -n "$datos" ]; then
    args+=(-H "Content-Type: application/json" --data "$datos")
  fi
  if [ -n "$TENANT" ]; then
    args+=(-H "X-Tenant-Id: $TENANT")
  fi
  CODIGO="$(curl "${args[@]}" "$GATEWAY$ruta")"
  if [ -z "$CODIGO" ]; then
    CODIGO=000
  fi
}

# Primer "id" del cuerpo
id_respuesta() {
  grep -o '"id":"[^"]*"' "$TMP/cuerpo" | head -1 | sed 's/.*"\([^"]*\)"$/\1/'
}

HORA="$(date +%H%M%S)"

# -------------------------------------------------------------------- Planes
peticion GET /api/plans
if [ "$CODIGO" = "200" ] && grep -q '"name":"Gratis"' "$TMP/cuerpo"; then
  ok "GET /api/plans lista los planes oficiales"
else
  fallo "GET /api/plans no devolvió los planes (código $CODIGO)"
fi

grep -o '{"id":"[^"]*","name":"Gratis"[^}]*}' "$TMP/cuerpo" | grep -q '"removesAds":false' \
  && ok "el plan Gratis tiene removesAds false" \
  || fallo "el plan Gratis no tiene removesAds false"

grep -o '{"id":"[^"]*","name":"Compra única"[^}]*}' "$TMP/cuerpo" | grep -q '"removesAds":true' \
  && ok "el plan Compra única tiene removesAds true" \
  || fallo "el plan Compra única no tiene removesAds true"

grep -o '{"id":"[^"]*","name":"Premium mensual"[^}]*}' "$TMP/cuerpo" | grep -q '"removesAds":true' \
  && ok "el plan Premium mensual tiene removesAds true" \
  || fallo "el plan Premium mensual no tiene removesAds true"

PLAN_UNICA="$(grep -o '{"id":"[^"]*","name":"Compra única"' "$TMP/cuerpo" | sed 's/{"id":"//;s/".*//')"
PLAN_GRATIS="$(grep -o '{"id":"[^"]*","name":"Gratis"' "$TMP/cuerpo" | sed 's/{"id":"//;s/".*//')"

# ------------------------------------------------------------- Usuario nuevo
CORREO="prueba.anuncios.$HORA@ejemplo.com"
peticion POST /api/users/register \
  "{\"Email\":\"$CORREO\",\"DisplayName\":\"Prueba anuncios $HORA\",\"Password\":\"ClaveDePrueba123\"}"
[ "$CODIGO" = "201" ] && ok "POST /api/users/register crea el usuario con 201" \
  || fallo "POST /api/users/register esperaba 201 y llegó $CODIGO"
USUARIO="$(grep -o '"userId":"[^"]*"' "$TMP/cuerpo" | head -1 | sed 's/.*:"\([^"]*\)"/\1/')"
# El tenant aprobvisionado en el registro es el que viaja en X-Tenant-Id
TENANT="$(grep -o '"tenantId":"[^"]*"' "$TMP/cuerpo" | head -1 | sed 's/.*:"\([^"]*\)"/\1/')"

# --------------------------------------------------------- Estado sin plan
peticion GET "/api/users/$USUARIO/subscription-status"
if [ "$CODIGO" = "200" ] && grep -q '"showAds":true' "$TMP/cuerpo"; then
  ok "sin suscripción el estado devuelve showAds true"
else
  fallo "sin suscripción el estado esperaba showAds true (código $CODIGO)"
fi

# Sin cabecera el estado no sabe de qué tenant hablar
SIN_CABECERA="$TENANT"
TENANT=""
peticion GET "/api/users/$USUARIO/subscription-status"
[ "$CODIGO" = "400" ] && ok "el estado sin X-Tenant-Id responde 400" \
  || fallo "estado sin X-Tenant-Id esperaba 400 y llegó $CODIGO"
TENANT="$SIN_CABECERA"

# Un tenant que no existe en el Administrador responde 404
TENANT="$(guid)"
peticion GET "/api/users/$USUARIO/subscription-status"
[ "$CODIGO" = "404" ] && ok "el estado de un tenant inexistente responde 404" \
  || fallo "estado de tenant inexistente esperaba 404 y llegó $CODIGO"
# Restaurar el tenant real para el resto de las pruebas
TENANT="$SIN_CABECERA"

# ------------------------------------------------------- Suscripción y anuncios
peticion POST /api/subscriptions "{\"UserId\":\"$USUARIO\",\"PlanId\":\"$PLAN_UNICA\"}"
[ "$CODIGO" = "201" ] && ok "POST /api/subscriptions crea la suscripción con 201" \
  || fallo "POST /api/subscriptions esperaba 201 y llegó $CODIGO"
SUSCRIPCION="$(id_respuesta)"

peticion GET "/api/users/$USUARIO/subscription-status"
if [ "$CODIGO" = "200" ] && grep -q '"showAds":false' "$TMP/cuerpo"; then
  ok "con Compra única activa el estado devuelve showAds false"
else
  fallo "con Compra única activa el estado esperaba showAds false (código $CODIGO)"
fi

# Regla de negocio: una sola suscripción activa por usuario
peticion POST /api/subscriptions "{\"UserId\":\"$USUARIO\",\"PlanId\":\"$PLAN_GRATIS\"}"
[ "$CODIGO" = "409" ] && ok "una segunda suscripción activa responde 409" \
  || fallo "segunda suscripción esperaba 409 y llegó $CODIGO"

# ----------------------------------------------------------------- Cancelación
peticion POST "/api/subscriptions/$SUSCRIPCION/cancel"
[ "$CODIGO" = "200" ] && ok "POST /api/subscriptions/{id}/cancel responde 200" \
  || fallo "cancelación esperaba 200 y llegó $CODIGO"

peticion GET "/api/users/$USUARIO/subscription-status"
if [ "$CODIGO" = "200" ] && grep -q '"showAds":true' "$TMP/cuerpo"; then
  ok "tras cancelar el estado vuelve a showAds true"
else
  fallo "tras cancelar el estado esperaba showAds true (código $CODIGO)"
fi

peticion GET "/api/subscriptions/$SUSCRIPCION"
grep -q '"status":"Cancelled"' "$TMP/cuerpo" \
  && ok "la suscripción cancelada informa status Cancelled" \
  || fallo "la suscripción cancelada no informa Cancelled (código $CODIGO)"

# ------------------------------------------------------------------ Limpieza
# La suscripción queda cancelada arriba. No existe DELETE de usuarios, así que
# el usuario de prueba permanece con su correo de esta hora
echo "Hora $HORA - usuario $USUARIO - resumen: fallos=$fallos"
if [ "$fallos" -gt 0 ]; then
  exit 1
fi
echo "Todas las comprobaciones de administrador pasaron."
