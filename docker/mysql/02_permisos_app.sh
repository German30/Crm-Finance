#!/bin/bash
# =============================================================================
# Permisos del usuario de la aplicacion (solo para el contenedor)
# -----------------------------------------------------------------------------
# La imagen de MySQL crea el usuario de MYSQL_USER, pero NO le concede nada: solo
# otorga permisos cuando ademas se define MYSQL_DATABASE, y aqui la base la crea
# 00_schema.sql para tener una sola fuente de verdad del esquema.
#
# Asi que los permisos se dan aqui, y solo sobre datos: nada de DDL, que le
# corresponde a los scripts de database/mysql/.
#
# Es un .sh y no un .sql para poder usar el nombre real de MYSQL_USER en vez de
# darlo por hecho. Lo ejecuta la imagen desde /docker-entrypoint-initdb.d, una sola
# vez, cuando MySQL inicializa su directorio de datos.
#
# No vive en database/mysql/ porque es especifico del contenedor: en una instalacion
# normal de MySQL el usuario lo creas tu con los permisos que quieras.
# =============================================================================

usuario="${MYSQL_USER:-crm_app}"
echo "[permisos] concediendo permisos de datos sobre crm_finance a '${usuario}'"

mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}" --default-character-set=utf8mb4 <<EOSQL
SET NAMES utf8mb4;
GRANT SELECT, INSERT, UPDATE, DELETE ON \`crm_finance\`.* TO '${usuario}'@'%';
FLUSH PRIVILEGES;
EOSQL

echo "[permisos] concedidos:"
mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}" -N -B \
    -e "SHOW GRANTS FOR '${usuario}'@'%';"
