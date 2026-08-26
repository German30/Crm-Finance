using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.HealthChecks;
using CRMFinaciertoBackend.Middleware;
using CRMFinaciertoBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Los origenes salen de configuracion para poder cambiarlos en un contenedor sin
// recompilar (Cors__AllowedOrigins__0=...). Sin nada configurado se mantiene el
// Angular local de siempre.
var origenesPermitidos = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (origenesPermitidos is null || origenesPermitidos.Length == 0)
    origenesPermitidos = new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirAngular", policy =>
    {
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexion no esta configurada");

// La version del servidor se declara en configuracion en vez de detectarla:
// ServerVersion.AutoDetect abre una conexion durante el arranque, asi que la app no
// levantaria si MySQL todavia no esta listo (justo lo que pasa en un contenedor).
// Si se deja vacia se cae en la deteccion automatica.
var versionMySql = builder.Configuration.GetValue<string>("Database:MySqlVersion");
var versionServidor = string.IsNullOrWhiteSpace(versionMySql)
    ? ServerVersion.AutoDetect(cadenaConexion)
    : ServerVersion.Parse(versionMySql);

// EnableRetryOnFailure absorbe los fallos transitorios de conexion, habituales cuando la
// BD vive en otro contenedor y todavia esta arrancando.
//
// OJO: instala una estrategia de ejecucion INCOMPATIBLE con las transacciones abiertas a
// mano. Todo BeginTransactionAsync tiene que ir dentro de
// Database.CreateExecutionStrategy().ExecuteAsync(...) o falla en ejecucion (ver
// OperationService).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseMySql(
       cadenaConexion,
       versionServidor,
       mySql => mySql.EnableRetryOnFailure(
           maxRetryCount: 5,
           maxRetryDelay: TimeSpan.FromSeconds(10),
           errorNumbersToAdd: null)));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey")
    ?? throw new InvalidOperationException("La clave JWT no esta configurada");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = false,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidateAudience = false,
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequiereAdministrador", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("AreaBanca", policy => policy.RequireClaim("Area", "Banca"));
    options.AddPolicy("AreaSeguros", policy => policy.RequireClaim("Area", "Seguros"));

    // Las politicas de area puras dejan fuera al administrador cuando su rol cuelga de otra
    // area. Los endpoints especificos de negocio usan estas dos, que aceptan ambos casos.
    options.AddPolicy("BancaOAdministrador", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Administrador") || context.User.HasClaim("Area", "Banca")));

    options.AddPolicy("SegurosOAdministrador", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Administrador") || context.User.HasClaim("Area", "Seguros")));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Cache en memoria para los catalogos (ver CatalogService).
builder.Services.AddMemoryCache();

// /health/live  -> el proceso responde.
// /health/ready -> ademas MySQL contesta. Es la que sondea Docker: un contenedor
//                  que arranca pero no alcanza la base no esta listo para recibir trafico.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("mysql", tags: new[] { "ready" });

// Cualquier excepcion no controlada sale como { "message": "..." }, igual que el resto de la API.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CRM Financiero", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticacion JWT el esquema Bearer Ejemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer"}
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IOportunityService, OportunityService>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

var app = builder.Build();

app.UseExceptionHandler();

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// En Development el perfil de lanzamiento solo expone HTTP (puerto 5170), asi que redirigir
// a HTTPS solo genera un warning por peticion y rompe la llamada de Angular.
//
// En un contenedor pasa lo mismo aunque el entorno sea Production: la app sirve HTTP plano
// y el TLS lo termina el proxy de delante. Por eso es un flag propio y no depende del
// entorno: Hosting__UseHttpsRedirection=false.
var usarHttpsRedirection = app.Configuration.GetValue<bool?>("Hosting:UseHttpsRedirection")
    ?? !app.Environment.IsDevelopment();

if (usarHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("PermitirAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Anonimos a proposito: los sondea Docker, que no tiene token.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.Run();