#!/usr/bin/env bash
# Túnel de desarrollo (devtunnels) para compartir el Gateway con los probadores.
#
# Uso:
#   scripts/tunel.sh preparar   # una vez por equipo: valida el CLI, crea el
#                                # túnel tuki-gateway, su puerto y el acceso
#   scripts/tunel.sh            # levanta el túnel; deja esta terminal abierta
#
# Alternativa sin CLI: en VS Code, panel "Puertos" → "Reenviar puerto" → 5000.
set -euo pipefail

TUNEL="${TUNEL:-tuki-gateway}"
# Protocolo del Gateway en local para el túnel: http -> 5005, https -> 5000.
# El tramo público del túnel es HTTPS siempre (certificado de Microsoft); esto
# solo define el enlace interno hacia el Gateway en esta máquina. El 5000 usa el
# certificado de desarrollo de Kestrel, que el agente puede no validar, por eso
# http en 5005 es la opción que sabemos que responde de extremo a extremo.
PROTO="${PROTO:-http}"
if [ "$PROTO" = "https" ]; then
  PUERTO="${PUERTO:-5000}"
else
  PUERTO="${PUERTO:-5005}"
fi
ACCION="${1:-host}"

if ! command -v devtunnel >/dev/null 2>&1; then
  echo "No se encontró devtunnel. Instálalo con:"
  echo "  curl -sL https://aka.ms/DevTunnelCliInstall | bash"
  exit 1
fi

case "$ACCION" in
  preparar)
    # Sesión con GitHub; solo pregunta si aún no hay token guardado
    if ! devtunnel user show >/dev/null 2>&1; then
      echo "Iniciando sesión con tu cuenta GitHub (se abre el navegador)..."
      devtunnel user login -g
    fi

    # Túnel con nombre fijo: la URL no cambia al reiniciar
    if devtunnel show "$TUNEL" >/dev/null 2>&1; then
      echo "El túnel $TUNEL ya existe."
    else
      devtunnel create "$TUNEL"
      echo "Túnel $TUNEL creado."
    fi

    # El reenvío llega por HTTP; el Gateway expone el 5005 para eso
    if devtunnel port show "$TUNEL" -p "$PUERTO" >/dev/null 2>&1; then
      echo "El puerto $PUERTO ya está en el túnel."
    else
      devtunnel port create "$TUNEL" -p "$PUERTO" --protocol "$PROTO"
      echo "Puerto $PUERTO agregado al túnel."
    fi

    # Anónimo para que los probadores no necesiten cuenta de Microsoft/GitHub
    devtunnel access create "$TUNEL" --anonymous --expiration 8h || true

    echo "Preparado. Ahora levántalo con: scripts/tunel.sh"
    ;;
  host)
    echo "Levantando el túnel $TUNEL (puerto $PUERTO). Ctrl+C lo detiene."
    exec devtunnel host "$TUNEL"
    ;;
  *)
    echo "Uso: $0 [preparar|host]"
    exit 2
    ;;
esac
