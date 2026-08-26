#!/bin/bash
# Sondeo de salud del contenedor de la API.
#
# La imagen mcr.microsoft.com/dotnet/aspnet:8.0 no trae curl ni wget, pero bash si
# soporta /dev/tcp, asi que la peticion HTTP se hace con builtins y no hace falta
# instalar nada (ni engordar la imagen ni ampliar su superficie).
#
# Se consulta /health/ready, que ademas de responder comprueba que SQL Server
# contesta: un proceso vivo que no alcanza la base no esta listo para dar trafico.

set -euo pipefail

PUERTO="${ASPNETCORE_HTTP_PORTS:-8080}"

exec 3<>"/dev/tcp/127.0.0.1/${PUERTO}"

printf 'GET /health/ready HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3

# La primera linea de la respuesta trae el codigo: "HTTP/1.1 200 OK".
head -n 1 <&3 | grep -q ' 200 '
