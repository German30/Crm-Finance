# Meridian · CRM Financiero (frontend)

Panel de administración en Angular 20 (standalone, signals, control flow `@if`/`@for`)
para el CRM financiero. Consume **únicamente** la API .NET del backend: no hay datos
simulados en el código de la aplicación.

## Requisitos

- Node 20 o superior
- El backend .NET corriendo en `http://localhost:5170`

## Arranque

```bash
npm install
npm start          # http://localhost:4321 (o 4200 por defecto)
```

### Sobre CORS y el proxy

El backend no envía cabeceras CORS, así que una llamada directa del navegador a
`http://localhost:5170` queda bloqueada en el preflight. Por eso el frontend habla
siempre con **`/api` en su propio origen** y `ng serve` reenvía esas rutas al backend:

```jsonc
// proxy.conf.json
{ "/api": { "target": "http://localhost:5170", "secure": false, "changeOrigin": true } }
```

Así no hace falta tocar la configuración del backend. Si el backend cambia de puerto,
edita `proxy.conf.json`. En producción, publica el frontend detrás del mismo host que
la API (o configura el reverse proxy para `/api`); `src/environments/environment.prod.ts`
ya apunta a `/api`.

Si al arrancar ves `ECONNREFUSED` en la consola de `ng serve`, el backend no está
levantado o escucha en otro puerto.

## Endpoints que consume

| Método | Ruta | Política del backend |
|---|---|---|
| POST | `/api/Auth/login` | anónima |
| GET | `/api/Auth/me` | autenticado |
| POST | `/api/Auth/change-password` | autenticado |
| GET | `/api/Client`, `/Client/phisic/{id}`, `/Client/moral/{id}` | autenticado |
| DELETE | `/api/Client/{id}` | **RequiereAdministrador** |
| GET | `/api/Product` | autenticado |
| GET | `/api/Contract` | autenticado |
| GET | `/api/Contract/bank/{id}` | **BancaOAdministrador** |
| GET | `/api/Contract/insurance/{id}` | **SegurosOAdministrador** |
| GET | `/api/Operation/transactions/contract/{id}` | **BancaOAdministrador** |
| GET | `/api/Operation/claims/contract/{id}` | **SegurosOAdministrador** |
| GET, PATCH | `/api/Oportunity`, `/Oportunity/{id}/stage` | autenticado |
| GET | `/api/Catalog/*`, `/User/roles`, `/User/areas`, `/User/status` | autenticado |
| GET, POST, PUT, PATCH | `/api/User*` | **RequiereAdministrador** |

## Roles y áreas

El backend firma en el JWT un claim `role` (uno de los 19 del catálogo) y un claim
`Area` (`General`, `Seguros` o `Banca`). El frontend refleja esas políticas en
`core/services/access.service.ts`:

| Capacidad | Quién |
|---|---|
| Clientes, contratos, oportunidades, productos (lectura) | los 19 roles |
| Detalle y movimientos de Banca | área Banca + Administrador |
| Detalle y siniestros de Seguros | área Seguros + Administrador |
| Gestión de usuarios | solo Administrador |
| Alta/edición de productos y borrados | solo Administrador |

Esto **no es seguridad**: el backend sigue siendo la autoridad y responde 403. Sirve
para no ofrecer acciones que el rol tiene prohibidas — un botón que siempre falla es
peor que un botón ausente. El menú lateral, las rutas y los enlaces de detalle se
recortan al alcance del rol, y cuando algo queda fuera se explica en pantalla.

El Panorama se construye sobre endpoints que **todos** los roles pueden llamar
(`/Client`, `/Contract`, `/Oportunity`, `/Product`), nunca sobre `/User`, que exige
Administrador.

## Estructura

```
src/app/
  core/
    guards/          authGuard (con returnUrl), guestGuard y permissionGuard por rol
    interceptors/    JWT + manejo de 401 y mensajes de error legibles
    services/        auth, access (políticas), crm (toda la API), crm-analytics,
                     theme, toast
  modules/
    auth/login        Pantalla de acceso
    admin/shell       Marco de la aplicación (rail filtrado por rol, barra, cuenta)
    admin/overview    Panorama comercial (sirve a los 19 roles)
    admin/clients     Cartera + expediente de persona física / moral
    admin/contracts   Rejilla + detalle bancario y de seguros con sus operaciones
    admin/pipeline    Embudo por etapa, con cambio de etapa en línea
    admin/products    Catálogo de productos financieros
    admin/user-list   Directorio (solo Administrador)
    admin/user-form   Alta y edición (solo Administrador)
    admin/settings    Sesión, capacidades del rol, contraseña, tema y conexión
    not-found         404
  shared/
    models/          Contratos de la API y de las gráficas
    ui/              Íconos, gráficas, diálogo, toasts
    utils/           Formato de fechas/números y escalas de ejes
```

## Diseño

Sistema de tokens en `src/styles.css`: tema oscuro por defecto y tema claro con pasos
propios (no una inversión), ambos seleccionables desde Ajustes o desde el ícono de la
barra. La preferencia se guarda en `localStorage` y se aplica antes del primer pintado
(script en `index.html`) para que no haya destello.

- Tipografía: IBM Plex Sans para la interfaz, IBM Plex Mono para cifras y fechas.
- Acento ámbar reservado para acción primaria, selección y estado.
- Paleta categórica de 4 series validada para daltonismo y contraste en ambos temas.

## Pruebas

```bash
npm test -- --watch=false --browsers=ChromeHeadless
```

Cubren el decodificado y la caducidad del JWT (incluidos los 6 roles con acentos), el
mapeo de políticas para **los 19 roles del catálogo**, los guards de permiso, el
interceptor, las escalas de los ejes y las agregaciones del panorama.

## Docker

La imagen compila la SPA con Node y la sirve con nginx; en la imagen final no queda
ni Node, ni `node_modules`, ni codigo fuente (~49 MB). nginx corre como usuario sin
privilegios y hace dos cosas: servir la aplicacion y **reenviar `/api` al backend**.

Ese reenvio es lo importante: el navegador habla con un solo origen, asi que **CORS
no interviene**. `environment.prod.ts` ya apunta a `/api`.

### Solo el frontend (el backend ya corre por su cuenta)

```bash
cp .env.example .env          # y cambia las claves
docker compose up -d --build  # -> http://localhost:4200
```

Por defecto reenvia a `http://host.docker.internal:5170`, es decir al backend que
escucha en tu maquina — lo mismo da que lo hayas levantado con Docker o desde
Visual Studio. Para apuntar a otro sitio, cambia `API_UPSTREAM` en el `.env`.

### Pila completa (MySQL + API + frontend)

```bash
cp .env.example .env
docker compose -f docker-compose.full.yml up -d --build
```

| Servicio | URL |
|---|---|
| Aplicacion | http://localhost:4200 |
| API (Swagger en `/swagger`) | http://localhost:5170 |
| MySQL | `localhost:3307` |

La API se construye desde `BACKEND_PATH` (por defecto `../CRMFinaciertoBackend`).
Esta pila **sustituye** al `docker-compose` del backend: si ya lo tienes arriba,
bajalo antes o chocaran por los puertos.

### Detalles que conviene conocer

- **`API_UPSTREAM` va sin barra ni ruta final.** `http://api:8080`, no
  `http://api:8080/`. Con una ruta al final, nginx la antepondria a todas las
  llamadas y la API responderia 404. El script `docker/15-normaliza-api-upstream.sh`
  recorta la barra y aborta el arranque si el valor no tiene sentido, en vez de
  dejar un fallo silencioso.
- **nginx resuelve el nombre del backend al arrancar y lo cachea.** Si el
  contenedor de la API cambia de IP, `docker compose restart web`. Se eligio esto
  frente al patron `resolver` + variable porque ese patron ignora `/etc/hosts` y
  rompe tanto `host.docker.internal` como la red bridge por defecto.
- **`index.html` no se cachea nunca**; los ficheros con hash en el nombre se
  cachean un anio como inmutables. Asi un despliegue nuevo se ve de inmediato sin
  renunciar al cache de los assets.
- **El sondeo del contenedor solo mira al frontend** (`/healthz`), no a la API. Si
  se encadenaran, una caida del backend marcaria el web como enfermo y un
  orquestador lo reiniciaria sin motivo.
- La CSP esta ajustada a lo que la aplicacion carga de verdad (Google Fonts para
  IBM Plex, y nada mas de fuera). Vive en `docker/seguridad.inc`.

## Build

```bash
npm run build      # dist/crm-frontend — ~327 kB inicial (~92 kB transferidos)
```
