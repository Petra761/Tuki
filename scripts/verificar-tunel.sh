#!/usr/bin/env bash
# Comprueba que el tunel expone el Gateway y que responde de extremo a extremo.
#
# Uso:
#   scripts/verificar-tunel.sh <url-del-tunel>
#   scripts/verificar-tunel.sh https://ffh8k5rn-5000.brs.devtunnels.ms
#
# Diagnostica el caso tipico: TLS correcto pero 0 bytes de vuelta, que significa
# que el reenvio apunta a un puerto que no coincide con el protocolo del Gateway.
set -uo pipefail

U="${1:-}"
if [ -z "$U" ]; then
  echo "Uso: $0 <url-del-tunel>"
  exit 2
fi
U="${U%/}"

echo "Gateway tunel: $U"
echo

echo "== TLS =="
echo | openssl s_client -connect "${U#https://}:443" -servername "${U#https://}" 2>/dev/null |
  grep -E "subject=|Verify return" || echo "(no se pudo leer el certificado)"
echo

echo "== GET /api/plans =="
CODE=$(curl -sS -m 30 -o /dev/null -w "%{http_code}" "$U/api/plans" 2>/dev/null)
if [ "$CODE" = "000" ]; then
  echo "000 (sin respuesta): TLS correcto pero 0 bytes de vuelta."
  echo "Causa habitual: el reenvio habla HTTP hacia un puerto HTTPS."
  echo "Solucion: reenvia el 5005 (HTTP), no el 5000 (HTTPS)."
  exit 1
fi
echo "HTTP $CODE"
[ "$CODE" = "200" ] || { echo "Se esperaba 200."; exit 1; }
echo

echo "== GET /api/accounts con X-Tenant-Id (aislamiento) =="
curl -sS -m 30 -o /dev/null -w "HTTP %{http_code}\n" \
  "$U/api/accounts" -H "X-Tenant-Id: 890d174a-0c1f-4945-b6c5-bbe38d169d1f"
echo

echo "== CORS: preflight desde la landing =="
curl -sS -m 20 -o /dev/null -D - -X OPTIONS "$U/api/accounts" \
  -H "Origin: https://tuki-landing-omega.vercel.app" \
  -H "Access-Control-Request-Method: GET" \
  -H "Access-Control-Request-Headers: x-tenant-id" |
  grep -iE "^HTTP|access-control" || echo "(sin cabeceras CORS)"
echo

echo "El tunel responde. Bateria completa con:"
echo "  GATEWAY=$U ./scripts/verificar-rutas.sh"
