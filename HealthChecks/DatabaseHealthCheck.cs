using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CRMFinaciertoBackend.Data;

namespace CRMFinaciertoBackend.HealthChecks
{
    /// <summary>
    /// Comprueba que la API puede hablar con MySQL. Es lo que decide si el contenedor
    /// esta "healthy": arrancar el proceso no significa nada si la base no responde.
    /// <para>
    /// Se hace a mano en vez de con el paquete
    /// Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore para no meter otra
    /// referencia en un csproj donde las versiones son delicadas (ver Trampas conocidas).
    /// </para>
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DatabaseHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("Conexion con MySQL establecida.")
                    : HealthCheckResult.Unhealthy("MySQL no acepta la conexion.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Error al comprobar la conexion con MySQL.", ex);
            }
        }
    }
}
