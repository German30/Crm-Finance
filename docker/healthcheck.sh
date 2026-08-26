#!/bin/sh
# Sondeo de salud del contenedor del frontend.
#
# Comprueba /healthz, que sirve el propio nginx. A proposito NO comprueba la API:
# el frontend esta sano si sirve la SPA, aunque el backend este caido. Si se
# encadenaran, una caida de la API marcaria tambien el contenedor web como
# unhealthy y un orquestador lo reiniciaria sin motivo.
#
# sh y no bash: la imagen de nginx es Alpine y no trae bash. wget si viene, dentro
# de busybox, asi que no hace falta instalar nada.

set -eu

PUERTO="${NGINX_PORT:-8080}"

wget --quiet --tries=1 --timeout=3 --spider "http://127.0.0.1:${PUERTO}/healthz"
