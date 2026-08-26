# =============================================================================
# CRMFinacierto — API
#
# Build en dos etapas: se compila con el SDK y solo el resultado publicado pasa a
# la imagen final, que lleva unicamente el runtime de ASP.NET.
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# El csproj va primero y solo: asi la capa de restore se cachea y no se repite en
# cada cambio de codigo.
COPY ["CRMFinaciertoBackend.csproj", "./"]
RUN dotnet restore "CRMFinaciertoBackend.csproj"

COPY . .
RUN dotnet publish "CRMFinaciertoBackend.csproj" \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Sondeo de salud. Se copia y se marca ejecutable como root, ANTES de cambiar de
# usuario: el usuario de la app no puede escribir en /usr/local/bin.
COPY docker/healthcheck.sh /usr/local/bin/healthcheck
RUN chmod +x /usr/local/bin/healthcheck

# 8080 y no 5170: es el puerto por defecto de las imagenes .NET 8, el unico que un
# usuario sin privilegios puede abrir. El mapeo a 5170 se hace en docker-compose.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# La app NO sirve HTTPS dentro del contenedor: el TLS lo termina el proxy de delante.
# Sin esto, en Production cada peticion se redirigiria a un https que nadie escucha.
ENV Hosting__UseHttpsRedirection=false

# Usuario sin privilegios que ya viene en la imagen base de .NET 8.
USER $APP_UID

ENTRYPOINT ["dotnet", "CRMFinaciertoBackend.dll"]
