# CRMFinacierto — Backend

API REST en **ASP.NET Core 8** (Web API) para un CRM financiero con dos áreas de negocio: **Banca** y **Seguros**. Autenticación JWT, EF Core sobre **MySQL 8** (proveedor Pomelo), Swagger en desarrollo. El frontend previsto es Angular (`http://localhost:4200`, habilitado vía CORS).

## Comandos

Requisitos locales: **.NET 8** (runtime `Microsoft.NETCore.App` **y** `Microsoft.AspNetCore.App`, ambos 8.x) y **MySQL 8** en `localhost:3306`. Ojo: tener solo el runtime 9 no basta; el proyecto es `net8.0` y falla al arrancar.

```powershell
winget install Microsoft.DotNet.Runtime.8        # si dotnet --list-runtimes no muestra 8.x
winget install Microsoft.DotNet.AspNetCore.8

dotnet restore
dotnet build
dotnet run                      # http://localhost:5170 (perfil "http", ASPNETCORE_ENVIRONMENT=Development)
```

### Con Docker

```powershell
cp .env.example .env            # y cambia las claves
docker compose up -d --build    # API en http://localhost:5170, MySQL en localhost:3307
docker compose logs -f api
docker compose down             # -v para borrar tambien el volumen de la BD
```

El stack levanta **su propio MySQL**, así que no toca el que tengas instalado (que ocupa el 3306 y el 33060).

**Dos servicios, cada uno con su puerto, los dos corriendo:**

| Servicio | Puerto | Qué hace |
|---|---|---|
| `crm-api` | `${API_PORT}` → 5170 | La imagen del `Dockerfile`. Escucha en 8080 dentro |
| `crm-mysql` | `${MYSQL_PORT}` → 3307 | MySQL 8.4, `utf8mb4` / `utf8mb4_0900_ai_ci`. Volumen `crm-mysqldata` |

Estado correcto tras `up -d`: **ambos `Up (healthy)`**. Si ves algo distinto, algo falla de verdad.

La base se crea e inicializa sola: los scripts montados en `/docker-entrypoint-initdb.d` los ejecuta la propia imagen de MySQL. Antes había un tercer contenedor `db-init` que hacía ese trabajo y terminaba en `Exited (0)`; funcionaba, pero se veía como un contenedor caído y no tenía puerto propio.

**`initdb.d` solo corre con el directorio de datos vacío.** Si cambias los scripts de esquema, hay que recrear el volumen:

```powershell
docker compose down -v && docker compose up -d
```

La API se conecta como **`crm_app`** con solo `SELECT/INSERT/UPDATE/DELETE` sobre `crm_finance`, no como `root`: no puede tocar el esquema, de eso se encargan los scripts. Los permisos los da [`docker/mysql/02_permisos_app.sh`](docker/mysql/02_permisos_app.sh) — hace falta porque la imagen crea el usuario de `MYSQL_USER` pero **no le concede nada** si no se define también `MYSQL_DATABASE`, y aquí la base la crea `00_schema.sql`.

Los secretos van en `.env`, que está en `.gitignore`.

## Estructura

| Carpeta | Contenido |
|---|---|
| `Program.cs` | Composición completa: CORS, DbContext, JWT, políticas de autorización, Swagger, DI, pipeline |
| `Controllers/` | `Auth`, `User`, `Client`, `Product`, `Contract`, `Oportunity`, `Operation`, `Catalog`, `TestConnection` |
| `Services/` | Interfaz + implementación por dominio (`Auth`, `User`, `Client`, `Product`, `Contract`, `Oportunity`, `Operation`, `Catalog`) + `IPasswordHasher`/`PasswordHasher` |
| `Data/ApplicationDbContext.cs` | `DbSet`s + `OnModelCreating` (relaciones 1:1, precisiones decimal, `DeleteBehavior`) |
| `Models/` | Entidades EF (23), mapeo explícito a tablas/columnas `snake_case` |
| `DTOs/` | `records` por dominio: `User/`, `Client/`, `Product/`, `Contract/`, `Oportunity/`, `Operation/`, `Catalog/` |
| `database/mysql/` | Esquema y catálogos de MySQL. **Fuente de verdad del esquema** |
| `database/sqlserver/` | Los mismos scripts para SQL Server, de cuando esa era la base. Archivo histórico, ya no se usan |
| `Dockerfile` · `docker-compose.yml` · `docker/` · `.env` | Stack contenerizado: API + MySQL. `docker/` lleva el sondeo de salud y los permisos del usuario de la app |
| `Validation/` | `RequiredDateAttribute` (fechas obligatorias reales) y `DecimalPrecision` (topes de las columnas `decimal`) |
| `HealthChecks/` | `DatabaseHealthCheck`: alimenta `/health/ready`, que es lo que sondea Docker |
| `Middleware/` | `GlobalExceptionHandler`: toda excepción no controlada sale como `{ "message": "..." }` |
| `Context/` | Vacía |

## Convenciones

- **Namespace raíz**: `CRMFinaciertoBackend` (el typo "Fenaciert**o**" está en todo el proyecto: csproj, sln, namespaces — no lo "corrijas" parcialmente).
- **Entidades**: C# en `PascalCase`, mapeadas explícitamente con `[Table("snake_case")]` y `[Column("snake_case")]`. Navegaciones `virtual` + `[ForeignKey(nameof(...))]`. Nullable habilitado; se usa `required` en propiedades obligatorias.
- **DTOs**: siempre `record` posicional con DataAnnotations inline. Patrón por dominio: `XCreateDto`, `XUpdateDto`, `XResponseDto` / `XGridDto` (lista) y `XDetailDto` (detalle).
- **Servicios**: reciben `ApplicationDbContext` por constructor, proyectan a DTO con `.Select(...)` dentro del query (no se devuelven entidades). Interfaz + implementación registradas como `Scoped` en `Program.cs`.
- **Controladores**: `[ApiController]`, `[Route("api/[controller]")]`, delegan todo al servicio, devuelven `Ok`/`NotFound`/`BadRequest` con `new { Message = "..." }` en español.
- **Idioma**: mensajes de usuario, comentarios y logs en español.

## Modelo de dominio

```
Area ──< Role ──< User
              └──< FinanceProduct

Client (type_person) ──1:1── PhisicPersonClient   (persona física)
                     └─1:1── MoralPersonClient    (persona moral)

ProductsContract  (contrato base: client + product + user + estado + referencia)
   ├─1:1── BankContract      ──< BankTransaction   (transaction_type)
   └─1:1── InsuranceContract ──< InsuranceClaim    (disaster_state)
                              └── PayForm

ComercialOportunity (client + product + user + Stage + probabilidad)
```

La herencia se modela como **table-per-type manual**: la tabla hija comparte PK con la padre (`HasKey` + `HasForeignKey<T>` en `OnModelCreating`) y se borra en cascada. Todas las FK "horizontales" usan `DeleteBehavior.Restrict`.

Catálogos: `Area`, `Role`, `UserStatus`, `TypePerson`, `Gender`, `CivilState`, `FinanceStatusProduct`, `ContractStatus`, `PayForm`, `Stage`, `TransactionType`, `DisasterState`.

## Autenticación y autorización

- `POST /api/Auth/login` → `{ Token, ExpirationInMinutes }`. Valida email + estado `"Activo"`.
- El token lleva claims: `NameIdentifier`, `Name`, `Email`, `Role` (nombre del rol) y **`Area`** (nombre del área del rol).
- `GET /api/Auth/me` devuelve esos claims sin tocar la BD (es lo que el frontend usa para pintar el menú por rol/área).
- Políticas declaradas en `Program.cs`. Son **políticas**, así que se aplican con `[Authorize(Policy = "...")]`; con `[Authorize(Roles = "...")]` nunca autorizan:
  - `RequiereAdministrador` — rol `Administrador`.
  - `AreaBanca` / `AreaSeguros` — claim `Area` exacto.
  - `BancaOAdministrador` / `SegurosOAdministrador` — `RequireAssertion`: rol `Administrador` **o** el claim de área. Son las que usan los endpoints de negocio, porque el rol `Administrador` cuelga del área `General` y las políticas de área puras lo dejarían fuera.
- **Toda la API exige token.** Solo `AuthController` (login) y `TestConnectionController` son `[AllowAnonymous]`.
- **Contraseñas hasheadas** con PBKDF2-HMAC-SHA256 (`PasswordHasher`, 100 000 iteraciones, sal de 16 bytes). Formato almacenado: `PBKDF2$iteraciones$saltB64$hashB64`. `Verify` acepta además hashes heredados en texto plano y devuelve `SuccessRehashNeeded`; el login los regraba hasheados de forma transparente en el primer acceso correcto.
- Cambio de contraseña: `POST /api/Auth/change-password` (el propio usuario, pide la actual) y `PATCH /api/User/{id}/reset-password` (administrador, no la pide).
- La caducidad sale de `JwtSettings:ExpirationInMinutes` (400 por defecto) y se valida (`ValidateLifetime = true`). `ValidateIssuer` y `ValidateAudience` siguen en `false`.

## Base de datos

- **MySQL 8** (proveedor `Pomelo.EntityFrameworkCore.MySql`). Local: `localhost:3306`, BD `crm_finance`, usuario `root`.
- **No hay migraciones de EF Core.** El código-primero es solo mapeo; el esquema se versiona a mano en `database/mysql/`, que es la fuente de verdad. Si añades o renombras una entidad, escribe también el script.
- La versión del servidor se declara en `Database:MySqlVersion` en vez de detectarse: `ServerVersion.AutoDetect` abre una conexión durante el arranque y la app no levantaría si MySQL aún no responde (justo lo que pasa en un contenedor). Si se deja vacía, se cae en la detección automática.

### Recrear la base desde cero

```powershell
mysql -u root -p --default-character-set=utf8mb4 < database/mysql/00_schema.sql
mysql -u root -p --default-character-set=utf8mb4 < database/mysql/00_seed.sql
```

| Script | Qué hace |
|---|---|
| `00_schema.sql` | 23 tablas con sus claves, índices y restricciones **en línea**. Crea la BD si no existe |
| `00_seed.sql` | Los 13 catálogos, los 24 productos y el usuario administrador de arranque |
| `10_datos_demo.sql` | **Opcional.** 100 usuarios + 500 clientes con contrato vigente. No se carga solo |

Ambos son idempotentes: el esquema usa `CREATE TABLE IF NOT EXISTS` y el seed `INSERT IGNORE`, así que se pueden reejecutar sin duplicar nada. Ambos empiezan con `SET NAMES utf8mb4`, que blinda la carga sin depender de cómo se invoque el cliente. Como MySQL no tiene `ADD CONSTRAINT IF NOT EXISTS` ni `CREATE INDEX IF NOT EXISTS`, **todo va dentro del `CREATE TABLE`** y las tablas están en orden topológico: cada una después de las que referencia.

`00_seed.sql` siembra al administrador con la contraseña **en texto plano**, a propósito: `AuthService` acepta un hash heredado en texto plano una vez y lo regraba como PBKDF2 en el primer login correcto. Hay que cambiarla al entrar.

### Datos de demostración

`10_datos_demo.sql` puebla la base para poder ver el CRM con volumen real. **No** está montado en `initdb.d` a propósito: son datos de prueba, se cargan a mano.

```powershell
# local
mysql -u root -p --default-character-set=utf8mb4 < database/mysql/10_datos_demo.sql
# contenedor
docker exec -i crm-mysql mysql -u root -p<clave> --default-character-set=utf8mb4 crm_finance < database/mysql/10_datos_demo.sql
```

| | |
|---|---|
| 100 usuarios | 55 de Banca y 45 de Seguros, repartidos entre los 18 roles operativos |
| 500 clientes | 329 personas físicas y 171 morales, cada una con su ejecutivo asignado |
| 500 contratos | Todos **Vigentes**: 250 de Banca y 250 de Seguros, uno por cliente |

Los ids van desde 1001 para no chocar nunca con filas creadas desde la API, y todo es `INSERT IGNORE`, así que se puede reejecutar. El generador usa semilla fija: el fichero es reproducible.

Los 100 usuarios comparten la contraseña de demo **`Demo1234*`** en texto plano, que `AuthService` convierte a PBKDF2 en el primer login de cada uno.

Coherencias que respeta, y que conviene no romper al regenerarlo: cada cliente está asignado a un ejecutivo **de su misma área**, los productos de crédito (`4,5,6,7`) llevan `loan_amount_granted > 0` y su `balance_actual` es saldo deudor, y las cuentas a la vista llevan `loan_amount_granted = 0`.

### Precisiones y tipos que impone el esquema

| Columna | Tipo | Tope |
|---|---|---|
| importes (`balance_actual`, `amount`, `insurance_sume_total`, `estimated_mont`, …) | `DECIMAL(15,2)` | `DecimalPrecision.Money` |
| `agreed_interest_rate`, `tasa_interes_o_prima_base` | `DECIMAL(5,2)` | `DecimalPrecision.Rate` (999.99) |
| `porcent_deductible` | `DECIMAL(4,2)` | `DecimalPrecision.Percent` (99.99) |

Las columnas de negocio (`date_opening_issue`, `birth_date`, `date_occurrence`, …) son `DATE`; las de auditoría, `DATETIME` con `DEFAULT CURRENT_TIMESTAMP`.

Índices UNIQUE: `users.email`, `role.role_name`, `area.area_name`, `type_person.type_name`, `user_status.status_name`, `products_contract.reference_number`, `insurance_claims.report_number`, `clients.fiscal_id` y `bank_contract.interbank_code`.

### Diferencias frente al SQL Server original

| | SQL Server | MySQL |
|---|---|---|
| NULL en índice UNIQUE | todos iguales: solo cabía uno (hizo falta índice filtrado) | cada uno distinto: caben varios sin más |
| Intercalación | `Modern_Spanish_CI_AS` (sensible a acentos) | `utf8mb4_0900_ai_ci` (**insensible** a acentos) |
| `VARCHAR(MAX)` | sí | `TEXT` |
| Índices | 24 declarados | 31: MySQL crea uno por cada FK automáticamente |

La insensibilidad a acentos hace las búsquedas más permisivas (`credito` encuentra `Crédito`), que para un CRM en español es preferible. El efecto secundario a tener presente: dos productos que solo se diferencien en un acento colisionarían en la unicidad por área.

## Endpoints actuales

Todo exige `Authorization: Bearer <token>` salvo lo marcado como anónimo.

```
# Auth
POST   /api/Auth/login                        (anonimo)
GET    /api/Auth/me
POST   /api/Auth/change-password

# Usuarios  (administrador, salvo los tres catalogos)
GET    /api/User                              GET    /api/User/{id}
POST   /api/User                              PUT    /api/User/{id}
PATCH  /api/User/{id}/toggle-status           PATCH  /api/User/{id}/reset-password
GET    /api/User/roles    GET /api/User/areas    GET /api/User/status   (cualquier autenticado)

# Clientes
GET    /api/Client?search=&typePersonId=&assignedUserId=
GET    /api/Client/{id}                       (despacha a fisica o moral)
GET    /api/Client/phisic/{id}                GET    /api/Client/moral/{id}
POST   /api/Client/phisic                     POST   /api/Client/moral
PUT    /api/Client/phisic/{id}                PUT    /api/Client/moral/{id}
DELETE /api/Client/{id}                       (administrador)

# Productos
GET    /api/Product?areaId=&statusId=         GET    /api/Product/{id}
POST   /api/Product                           PUT    /api/Product/{id}       (administrador)
DELETE /api/Product/{id}                      (administrador)

# Contratos
GET    /api/Contract?areaId=&clientId=&userId=&contractStatusId=&search=
GET    /api/Contract/{id}                     (despacha a bancario o seguro)
GET    /api/Contract/bank/{id}                POST /api/Contract/bank       PUT /api/Contract/bank/{id}
GET    /api/Contract/insurance/{id}           POST /api/Contract/insurance  PUT /api/Contract/insurance/{id}
PATCH  /api/Contract/{id}/status
DELETE /api/Contract/{id}                     (administrador)

# Oportunidades
GET    /api/Oportunity?clientId=&userId=&stageId=&areaId=
GET    /api/Oportunity/{id}                   POST   /api/Oportunity
PUT    /api/Oportunity/{id}                   PATCH  /api/Oportunity/{id}/stage
DELETE /api/Oportunity/{id}

# Operaciones
GET    /api/Operation/transactions/contract/{contractId}    (BancaOAdministrador)
GET    /api/Operation/transactions/{transactionId}
POST   /api/Operation/transactions
DELETE /api/Operation/transactions/{transactionId}          (administrador)
GET    /api/Operation/claims/contract/{contractId}          (SegurosOAdministrador)
GET    /api/Operation/claims/{insuranceId}
POST   /api/Operation/claims                  PUT    /api/Operation/claims/{insuranceId}
DELETE /api/Operation/claims/{insuranceId}                  (administrador)

# Catalogos (solo lectura)
GET    /api/Catalog/areas            /type-persons     /genders        /civil-states
GET    /api/Catalog/product-status   /contract-status  /pay-forms      /stages
GET    /api/Catalog/transaction-types                  /disaster-states

GET    /api/TestConnection/ping               (anonimo)

# Salud (anonimos: los sondea Docker, que no tiene token)
GET    /health/live                           proceso vivo
GET    /health/ready                          ademas MySQL responde
```

Los endpoints `bank/*` usan la política `BancaOAdministrador` y los `insurance/*` la `SegurosOAdministrador`.

## Trampas conocidas

- **Versiones de IdentityModel**: `Microsoft.IdentityModel.Tokens` debe permanecer en **7.0.3**, la misma línea que arrastra `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0` (`System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.JsonWebTokens`). Con una referencia flotante `8.*` el token se emite con los tipos 7.x y se valida con los 8.x, y **toda petición autenticada devuelve 401 `invalid_token`**. JwtBearer 9.x no es opción mientras el target sea `net8.0`. No uses versiones flotantes en este csproj.
- **Los nombres de tabla con typo son intencionales** y coinciden con la BD real: `insurance_contranct`, `finace_status_product`, y la columna `transaction_type.data_created`. No los "corrijas" sin cambiar también el esquema.
- **La imagen `mcr.microsoft.com/dotnet/aspnet:8.0` no trae `curl`, `wget` ni `nc`.** El healthcheck del contenedor usa `/dev/tcp` de bash (`docker/healthcheck.sh`) para no instalar nada. Si escribes el sondeo en línea dentro del YAML, los `

` que necesita la petición HTTP se convierten en saltos de línea reales y rompen el comando; por eso vive en un script.
- **`EnableRetryOnFailure` es incompatible con `BeginTransactionAsync`.** Todo bloque transaccional tiene que ir dentro de `Database.CreateExecutionStrategy().ExecuteAsync(...)`, y la entidad debe construirse *dentro* del bloque: tras un reintento, la instancia anterior conservaría su clave asignada y no se podría reinsertar. Afecta a los dos métodos de `OperationService` que mueven el balance.
- **Carga los scripts SQL con `--default-character-set=utf8mb4`.** Sin esa opcion el cliente de MySQL negocia latin1, los acentos de los ficheros (que son UTF-8) se guardan **doblemente codificados** y `Depósito` acaba almacenado como `DepÃ³sito`. Además de verse mal, **rompe el cálculo del balance**: `OperationService` deduce el signo de cada movimiento del nombre de su `transaction_type`, y con el texto corrupto deja de reconocerlos y devuelve 0. Es un fallo silencioso: los movimientos se registran pero el saldo no se mueve.
- **`[Required]` no detecta un `DateTime` ausente.** El binder deja `default(DateTime)` y `RequiredAttribute` solo rechaza `null`. Por eso las fechas obligatorias de los DTOs llevan además `[RequiredDate]` (`Validation/`), que exige año ≥ 1753. En MySQL una `DATE` no admite el año 1, así que sin esa validación el INSERT reventaría con un 500 en vez de un 400 limpio.
- **Valida los `decimal` contra la precisión real de la columna** con las constantes de `DecimalPrecision`. EF no comprueba precisión: en modo estricto MySQL rechaza el valor al ejecutar el INSERT y el cliente recibe un 500.
- **No hay migraciones**: si añades o renombras una entidad, valida el mapeo contra la BD real y actualiza `database/mysql/00_schema.sql`. Un cambio de esquema que solo exista en tu MySQL local se pierde en cuanto alguien recree la BD.
- **`[Required]` no detecta un `DateTime` ausente.** El binder deja `default(DateTime)` y `RequiredAttribute` lo da por válido (solo rechaza `null`). Por eso las fechas obligatorias de los DTOs llevan además `[RequiredDate]` (`Validation/`), que exige año ≥ 1753. Sin él, un POST sin `dateOpeningIssue` devolvía 201 y guardaba `0001-01-01`.
- **Valida los `decimal` contra la precisión real de la columna** con las constantes de `DecimalPrecision`. EF no comprueba precisión: un valor fuera de rango no falla en la validación del modelo sino al ejecutar el INSERT, y el cliente recibe un 500.
- Usa `DateTime.UtcNow` en todo el proyecto (los defaults de las entidades ya lo hacen).
- **Los `[Authorize]` de controlador y de acción se ACUMULAN, no se sustituyen.** Un `[Authorize]` suelto en una acción no relaja la política del controlador (solo `[AllowAnonymous]` la sobreescribe). Por eso `UserController` lleva `[Authorize]` a secas en el controlador y `[Authorize(Policy = "RequiereAdministrador")]` acción por acción: si la política estuviera en el controlador, `GET /api/User/roles` devolvería 403 a los no administradores.
- **`bank_contract.balance_actual` significa dos cosas distintas según el producto**, y de ahí sale el signo de cada movimiento (`OperationService`):
  - Cuenta de débito / ahorro → **saldo total** (dinero del cliente).
  - Cuenta de crédito → **saldo deudor** (lo que el cliente debe).

  Ni `transaction_type` ni `finance_products` guardan esa clasificación, así que se deduce del nombre: un producto es de crédito si contiene `credito` o `tarjeta` (cubre Tarjeta de Crédito, Crédito Personal / de Nómina, Hipotecario y Automotriz); el resto se trata como saldo total. Efecto de los cinco tipos sembrados:

  | Movimiento | Ahorro (saldo total) | Crédito (saldo deudor) |
  |---|---|---|
  | Depósito | suma | resta (abono a la deuda) |
  | Retiro | resta | suma (disposición) |
  | Pago de Crédito | resta | resta |
  | Cobro de Comisión | resta | suma |
  | Interés Generado | suma (ganado) | suma (cobrado) |

  **Las dos tablas no son inversas**: `Interés Generado` suma en ambas. Un tipo que no encaje en ninguna lista deja el balance intacto y registra un `LogWarning` — si añades tipos o productos al catálogo, actualiza las listas de `OperationService`.
- **El balance se actualiza con un UPDATE aritmético en el servidor** (`ExecuteUpdateAsync`) dentro de una transacción explícita, no leyendo-sumando-guardando: así dos movimientos simultáneos sobre el mismo contrato no se pisan.

## Estado y huecos conocidos

Los módulos de Client, Product, Contract, Oportunity, Operation y Catalog ya están implementados (servicio + controlador sobre los DTOs existentes), la API entera exige token y las contraseñas se guardan hasheadas. Queda pendiente:

- **No hay proyecto de tests.** La verificación se hizo con scripts end-to-end y de concurrencia contra la BD real; no queda nada automatizado en el repo.
- **Sin paginación**: los `GET` de lista devuelven todo, con filtros por query string.
- Los campos de texto libre (`AddressFiscal`, `Description`, `BeneficiaryName`, `ReportDetails`) no llevan `StringLength` porque sus columnas son `varchar(MAX)`; el único tope es el límite de cuerpo de Kestrel.
- Los catálogos se sirven desde `IMemoryCache` con 5 minutos de caducidad (`CatalogService.CacheDuration`). Como se mantienen por SQL directo, un alta tarda hasta ese tiempo en verse.
- La `SecretKey` JWT y la cadena de conexión están en `appsettings.json` versionado.
- `ClientService` fija `TypePersonFisica = 1` / `TypePersonMoral = 2` como constantes (coinciden con `type_person` en la BD actual) y valida que existan antes de usarlas; los DTOs de alta no traen `TypePersonId`, la ruta lo determina (`/phisic` o `/moral`).
- `MoralClientCreateDto` no tiene `Phone` (el `UpdateDto` sí): un cliente moral no puede llevar teléfono en el alta.
- Las bajas son físicas (`DELETE` real), no lógicas. Se bloquean con `400` cuando hay hijos con FK `Restrict` (contratos, oportunidades, transacciones, siniestros).
