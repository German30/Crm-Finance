# =============================================================================
# Meridian — CRM Financiero (frontend)
#
# Build en dos etapas: se compila con Node y solo el resultado estatico pasa a la
# imagen final, que lleva unicamente nginx. La imagen resultante no contiene ni
# Node, ni node_modules, ni el codigo fuente.
#
# nginx hace DOS cosas:
#   1. Sirve la SPA (con el fallback a index.html que necesita el enrutador).
#   2. Reenvia /api al backend.
#
# El punto 2 es el que importa: asi el navegador solo habla con UN origen y NO
# hace falta CORS. src/environments/environment.prod.ts ya apunta a "/api", que
# es exactamente lo que este proxy resuelve.
# =============================================================================

FROM node:22-alpine AS build
WORKDIR /app

# Los manifiestos van primero y solos: asi la capa de `npm ci` se cachea y no se
# repite en cada cambio de codigo.
COPY package.json package-lock.json ./

# `npm ci` y no `npm install`: instala exactamente lo que fija el lockfile, sin
# resolver versiones nuevas. Un build reproducible es innegociable en una imagen.
RUN npm ci

COPY . .

# La configuracion de produccion aplica outputHashing, presupuestos y minificado.
RUN npm run build


# -----------------------------------------------------------------------------

# nginx-unprivileged y no la imagen oficial: esta corre como root y abre el 80.
# Esta corre como el usuario `nginx` y escucha en el 8080, el mismo puerto interno
# que usa la API. Un contenedor de cara a internet no deberia necesitar root.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final

# La plantilla se procesa con envsubst en el arranque (lo hace el entrypoint de la
# imagen), asi que la URL del backend se decide al LEVANTAR el contenedor y no al
# construirlo: la misma imagen sirve para desarrollo, pruebas y produccion.
COPY docker/nginx.conf.template /etc/nginx/templates/default.conf.template

# Las cabeceras de seguridad viven aparte porque hay que incluirlas en varios
# `location` (ver el comentario en la plantilla). No es una plantilla: no lleva
# variables, asi que va directo a conf.d.
COPY docker/seguridad.inc /etc/nginx/conf.d/seguridad.inc

# Solo se sustituyen las variables que empiezan por API_. Sin este filtro,
# envsubst tambien tocaria cosas como $HOSTNAME dentro de la plantilla.
ENV NGINX_ENVSUBST_FILTER=^API_

# Valor por defecto: el servicio `api` del docker-compose. Se puede cambiar al
# levantar el contenedor sin reconstruir la imagen.
ENV API_UPSTREAM=http://api:8080

# Valida y normaliza API_UPSTREAM antes de que se genere la configuracion. El
# prefijo 15- lo coloca antes de 20-envsubst-on-templates.sh, que es quien
# procesa la plantilla; el orden de /docker-entrypoint.d/ es alfabetico.
COPY docker/15-normaliza-api-upstream.sh /docker-entrypoint.d/15-normaliza-api-upstream.sh

COPY --from=build /app/dist/crm-frontend/browser /usr/share/nginx/html

# El sondeo se copia como root y se marca ejecutable ANTES de volver al usuario sin
# privilegios, que no puede escribir en /usr/local/bin.
USER root
COPY docker/healthcheck.sh /usr/local/bin/healthcheck
RUN chmod +x /usr/local/bin/healthcheck /docker-entrypoint.d/15-normaliza-api-upstream.sh
USER nginx

EXPOSE 8080
