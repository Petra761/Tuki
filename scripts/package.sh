#!/usr/bin/env bash
set -euo pipefail

root="$(git rev-parse --show-toplevel)"
output_dir="$root/artifacts"
timestamp="$(date +%Y%m%d-%H%M%S)-$$"
temporary="$(mktemp -d)"
tree="$temporary/Tuki"
archive=''

cleanup() {
  local status=$?
  rm -rf -- "$temporary"
  if [[ "$status" -ne 0 && -n "$archive" ]]; then
    rm -f -- "$archive"
  fi
}
trap cleanup EXIT

mkdir -p "$tree" "$output_dir"
git -C "$root" archive --format=tar HEAD | tar -xf - -C "$tree"
find "$tree" -depth -type d \( -name .git -o -name node_modules -o -name bin -o -name obj \) \
  -exec rm -rf -- {} +
find "$tree" -type f \( -name .env -o \( -name '.env.*' ! -name '.env.example' \) \) -delete
find "$tree" -type l -delete

for required in \
  README.md \
  .env.example \
  mobile/.env.example \
  backend/Tuki.sln \
  backend/src/Tuki.Autenticacion/appsettings.json \
  backend/src/Tuki.Gateway/appsettings.json
do
  if [[ ! -f "$tree/$required" ]]; then
    printf 'Falta un archivo necesario para el paquete: %s\n' "$required" >&2
    exit 1
  fi
done

if ! find "$tree/backend/src" -type f -path '*/Migrations/*.cs' -print -quit | grep -q .; then
  echo 'No se encontraron migraciones de backend para incluir.' >&2
  exit 1
fi

known_secret_pattern='-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|AKIA[0-9A-Z]{16}|gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|AIza[0-9A-Za-z_-]{35}|sk_(live|test)_[A-Za-z0-9]{20,}|eyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}|(postgres|postgresql)://[^[:space:]@]+:[^[:space:]@]+@'
config_secret_pattern='"(password|passwd|secret|apikey|api_key|service[_-]?role[_-]?key|jwt[_-]?key|token|connectionstring|key)"[[:space:]]*:[[:space:]]*"[^"]{8,}"|^[[:space:]]*[A-Za-z0-9_]*(PASSWORD|PASSWD|JWT__KEY|API_KEY|SECRET|TOKEN|SERVICE_ROLE_KEY)[A-Za-z0-9_]*[[:space:]]*=[[:space:]]*[^[:space:]]{8,}'
known_matches="$(grep -RIlE --binary-files=without-match -- "$known_secret_pattern" "$tree" || true)"
config_matches="$(
  grep -RIlEi --binary-files=without-match \
    --include='*.json' --include='*.yaml' --include='*.yml' --include='.env*' \
    -- "$config_secret_pattern" "$tree" || true
)"

if [[ -n "$known_matches$config_matches" ]]; then
  echo 'Se detectaron posibles credenciales; no se creó el paquete. Revisa estos archivos:' >&2
  printf '%s\n%s\n' "$known_matches" "$config_matches" |
    sed "s#^$tree/##" |
    sort -u >&2
  exit 1
fi

if command -v rar >/dev/null 2>&1; then
  archive="$output_dir/Tuki-$timestamp.rar"
  (cd "$temporary" && rar a -idq -r -y "$archive" Tuki)
elif command -v zip >/dev/null 2>&1; then
  archive="$output_dir/Tuki-$timestamp.zip"
  (cd "$temporary" && zip -q -r -X "$archive" Tuki)
elif command -v python3 >/dev/null 2>&1; then
  archive="$output_dir/Tuki-$timestamp.zip"
  python3 - "$temporary" "$archive" <<'PY'
import os
import sys
import zipfile

root, archive = sys.argv[1:]
with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as output:
    for folder, _, names in os.walk(os.path.join(root, "Tuki")):
        for name in names:
            path = os.path.join(folder, name)
            output.write(path, os.path.relpath(path, root))
PY
else
  echo 'Se necesita rar, zip o Python 3 para crear el paquete.' >&2
  exit 1
fi

printf 'Paquete creado: %s\n' "${archive#"$root"/}"
if [[ "$archive" == *.zip ]]; then
  echo 'rar no está instalado; se generó un ZIP para convertirlo a RAR si la entrega lo requiere.'
fi
