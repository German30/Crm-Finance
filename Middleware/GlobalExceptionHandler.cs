using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace CRMFinaciertoBackend.Middleware
{
    /// <summary>
    /// Convierte cualquier excepcion no controlada en la misma forma JSON que usa el resto de
    /// la API (<c>{ "message": "..." }</c>) en vez de dejar escapar la pagina de excepciones de
    /// desarrollo o un 500 vacio en produccion. El detalle real se registra en el log.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Si el cliente corto la conexion no hay a quien responder.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
                return false;

            var (statusCode, message) = exception switch
            {
                // Choque contra una restriccion de la BD (clave unica, clave foranea, longitud).
                // Las reglas de negocio conocidas ya se validan antes y devuelven 400; llegar
                // aqui significa que se escapo una, asi que ademas queda registrado.
                DbUpdateException => (
                    StatusCodes.Status409Conflict,
                    "La operacion choca con una restriccion de la base de datos y no se pudo guardar."),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Ocurrio un error inesperado al procesar la peticion.")
            };

            _logger.LogError(
                exception,
                "Excepcion no controlada en {Method} {Path}: se respondio {StatusCode}.",
                httpContext.Request.Method, httpContext.Request.Path, statusCode);

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(new { Message = message }, cancellationToken);

            return true;
        }
    }
}
