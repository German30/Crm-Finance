#!/bin/sh
# Normaliza API_UPSTREAM antes de que se genere la configuracion de nginx.
#
# Se ejecuta desde /docker-entrypoint.d/ y el nombre importa: los scripts corren
# en orden alfabetico y el que aplica envsubst sobre las plantillas es
# 20-envsubst-on-templates.sh. Este tiene que ir ANTES.
#
# Por que hace falta: en la plantilla, `proxy_pass ${API_UPSTREAM};` va sin ruta a
# proposito, para que nginx conserve el URI original. Si el valor trae una barra
# final ("http://api:8080/"), nginx la interpreta como ruta y sustituye el /api/
# de la peticion por ella: todas las llamadas irian a la raiz del backend y
# responderian 404. Es un error facil de cometer y dificil de diagnosticar.

set -eu

if [ -z "${API_UPSTREAM:-}" ]; then
    echo "15-normaliza-api-upstream.sh: API_UPSTREAM esta vacia." >&2
    echo "  Define a donde reenviar /api, por ejemplo http://api:8080" >&2
    exit 1
fi

# Recorta cuantas barras finales haya.
normalizada=$(printf '%s' "$API_UPSTREAM" | sed 's:/*$::')

case "$normalizada" in
    http://*|https://*) ;;
    *)
        echo "15-normaliza-api-upstream.sh: API_UPSTREAM debe empezar por http:// o https://" >&2
        echo "  Valor recibido: $API_UPSTREAM" >&2
        exit 1
        ;;
esac

# Avisa si queda una ruta despues del host: nginx la antepondria a todo.
sin_esquema=${normalizada#*://}
case "$sin_esquema" in
    */*)
        echo "15-normaliza-api-upstream.sh: API_UPSTREAM no debe incluir una ruta." >&2
        echo "  Usa solo esquema, host y puerto. Valor recibido: $API_UPSTREAM" >&2
        exit 1
        ;;
esac

export API_UPSTREAM="$normalizada"
echo "15-normaliza-api-upstream.sh: /api se reenviara a $API_UPSTREAM"
