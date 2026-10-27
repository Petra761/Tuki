#!/usr/bin/env bash
# Comprueba que cada recurso publicado en el OpenAPI "publico" de un servicio
# está enrutado por el Gateway. Si aparece un controlador nuevo sin su ruta
# en appsettings.json del Gateway, el script falla y dice cuál falta.
set -u

BASE_URL="${1:-https://localhost:5000}"
SERVICIOS=(administrador autenticacion cuentas movimientos presupuestos)

fallos=0

for servicio in "${SERVICIOS[@]}"; do
  doc="$(mktemp)"
  # -k: los servicios corren con certificado autofirmado en desarrollo.
  if ! curl -ksSf "$BASE_URL/openapi/$servicio.json" -o "$doc"; then
    echo "FALLO [$servicio]: no se pudo descargar $BASE_URL/openapi/$servicio.json"
    fallos=$((fallos + 1))
    rm -f "$doc"
    continue
  fi

  # Solo las rutas /api/... se exponen por el Gateway; /internal/... es privada.
  rutas="$(jq -r '.paths | keys[]' "$doc" | grep '^/api/' || true)"
  rm -f "$doc"

  if [ -z "$rutas" ]; then
    echo "FALLO [$servicio]: el documento público no tiene rutas /api/"
    fallos=$((fallos + 1))
    continue
  fi

  while IFS= read -r ruta; do
    # Se prueba el prefijo fijo de la ruta (hasta el primer segmento dinámico):
    # basta con que el Gateway la reconozca.
    # El Gateway agrega el header X-Tuki-Gateway a toda respuesta enrutada, así
    # que un 404 SIN el header significa que la ruta no está configurada, mientras
    # que un 404 CON el header viene del servicio (por ejemplo, un recurso sin
    # GET en la colección) y la ruta existe. 400 por falta de X-Tenant-Id o
    # 401 sin token también cuentan como enrutadas.
    prefijo="$(echo "$ruta" | sed 's/{.*//' | sed 's:/$::')"
    respuesta="$(curl -ksS -o /dev/null -D - -w '%{http_code}' "$BASE_URL$prefijo")"
    codigo="$(echo "$respuesta" | tail -n 1)"
    enrutada=1
    if [ "$codigo" != "404" ] || echo "$respuesta" | grep -qi "^x-tuki-gateway:"; then
      enrutada=0
    fi
    if [ "$enrutada" -ne 0 ]; then
      echo "FALLO [$servicio]: $ruta no está enrutada por el Gateway (404 en $prefijo)"
      fallos=$((fallos + 1))
    else
      echo "OK    [$servicio] $ruta ($codigo)"
    fi
  done <<< "$rutas"
done

if [ "$fallos" -gt 0 ]; then
  echo
  echo "$fallos recurso(s) sin ruta en el Gateway. Agrega el recurso a la lista de su servicio en backend/src/Tuki.Gateway/appsettings.json."
  exit 1
fi

echo
echo "Todas las rutas del documento público de cada servicio responden por el Gateway."
