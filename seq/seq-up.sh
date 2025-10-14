#!/usr/bin/env bash
set -euo pipefail

# ---- Config (override via env if desired) ----
IMAGE="${SEQ_IMAGE:-datalust/seq:latest}"
CONTAINER="${SEQ_CONTAINER:-seq}"
VOLUME="${SEQ_VOLUME:-seq-data}"

UI_PORT="${SEQ_UI_PORT:-8081}"          # Seq web UI (container port 80)
INGEST_PORT="${SEQ_INGEST_PORT:-5341}"  # Seq ingestion (container port 5341)

# ---- Helpers ----
exists_container() { docker ps -a --format '{{.Names}}' | grep -qx "$CONTAINER"; }
running_container() { docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; }
exists_volume() { docker volume ls --format '{{.Name}}' | grep -qx "$VOLUME"; }

# ---- Ensure Docker is available ----
if ! command -v docker >/dev/null 2>&1; then
  echo "ERROR: Docker is not installed or not on PATH. Install Docker Desktop (WSL2 backend) and try again." >&2
  exit 1
fi

# ---- Create volume if needed ----
if ! exists_volume; then
  echo "Creating Docker volume: $VOLUME"
  docker volume create "$VOLUME" >/dev/null
else
  echo "Docker volume already exists: $VOLUME"
fi

# ---- Pull latest image ----
echo "Pulling image: $IMAGE"
docker pull "$IMAGE"

# ---- Stop/remove any existing container with same name ----
if running_container; then
  echo "Stopping running container: $CONTAINER"
  docker stop "$CONTAINER" >/dev/null
fi

if exists_container; then
  echo "Removing existing container: $CONTAINER"
  docker rm "$CONTAINER" >/dev/null
fi

# ---- Run container ----
echo "Starting Seq container..."
docker run -d \
  --name "$CONTAINER" \
  -p "${UI_PORT}:80" \
  -p "${INGEST_PORT}:5341" \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINUSERNAME=admin \
  -e SEQ_FIRSTRUN_ADMINPASSWORD=Abc123456789 \
  -v "${VOLUME}:/data" \
  "$IMAGE" >/dev/null

echo "✓ Seq is starting."

# ---- Status output ----
echo
echo "UI:       http://localhost:${UI_PORT}"
echo "Ingest:   http://localhost:${INGEST_PORT}"
echo
echo "Useful commands:"
echo "  docker logs -f ${CONTAINER}"
echo "  docker stop ${CONTAINER} && docker rm ${CONTAINER}"
echo "  docker exec -it ${CONTAINER} seq help"


#The container’s data lives in the Docker volume seq-data. Remove the volume only if you intend to wipe data
##docker volume rm seq-data