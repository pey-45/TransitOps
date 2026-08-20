#!/usr/bin/env bash
set -Eeuo pipefail

readonly script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly project_dir="$(cd -- "${script_dir}/.." && pwd)"
readonly compose_file="${DEPLOY_COMPOSE_FILE:-${project_dir}/docker-compose.deploy.yml}"
readonly env_file="${DEPLOY_ENV_FILE:-${project_dir}/.env}"
readonly health_attempts="${DEPLOY_HEALTH_ATTEMPTS:-30}"
compose=(docker compose --env-file "${env_file}" --file "${compose_file}")

if ! command -v docker >/dev/null 2>&1; then
  echo "Error: Docker no está instalado o no está en PATH." >&2
  exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
  echo "Error: el plugin Docker Compose v2 no está disponible." >&2
  exit 1
fi

if [[ ! -f "${compose_file}" ]]; then
  echo "Error: no existe ${compose_file}." >&2
  exit 1
fi

if [[ ! -f "${env_file}" ]]; then
  echo "Error: no existe ${env_file}; créalo desde .env.deploy.example." >&2
  exit 1
fi

cd "${project_dir}"
"${compose[@]}" config --quiet

echo "Descargando imágenes publicadas..."
"${compose[@]}" pull

echo "Aplicando el despliegue..."
"${compose[@]}" up --detach --remove-orphans --wait

echo "Comprobando la API a través del proxy web interno..."
healthy=false
for ((attempt = 1; attempt <= health_attempts; attempt++)); do
  if "${compose[@]}" exec -T web \
    wget --quiet --output-document=- http://127.0.0.1/api/v1/health 2>/dev/null \
    | grep --quiet '"status":"healthy"'; then
    healthy=true
    break
  fi
  sleep 2
done

if [[ "${healthy}" != true ]]; then
  echo "Error: la aplicación no superó el health check interno." >&2
  "${compose[@]}" ps >&2
  "${compose[@]}" logs --no-color --tail=100 api web >&2
  exit 1
fi

echo "Despliegue saludable. Estado de los contenedores:"
"${compose[@]}" ps

echo "Imágenes desplegadas:"
for service in api web; do
  container_id="$("${compose[@]}" ps --quiet "${service}")"
  image_id="$(docker inspect --format '{{.Image}}' "${container_id}")"
  digest="$(docker image inspect --format '{{if .RepoDigests}}{{index .RepoDigests 0}}{{else}}sin-digest{{end}}' "${image_id}")"
  revision="$(docker image inspect --format '{{index .Config.Labels "org.opencontainers.image.revision"}}' "${image_id}")"
  printf '  %s: revision=%s digest=%s\n' "${service}" "${revision:-desconocida}" "${digest}"
done

tunnel_url="$("${compose[@]}" logs --no-color --tail=200 cloudflared 2>/dev/null \
  | sed -n 's#.*\(https://[-a-z0-9]*\.trycloudflare\.com\).*#\1#p' \
  | tail -n 1)"
if [[ -n "${tunnel_url}" ]]; then
  echo "URL pública temporal: ${tunnel_url}"
else
  echo "El túnel está arrancando; consulta su URL con:"
  echo "  docker compose --env-file ${env_file} --file ${compose_file} logs cloudflared"
fi
