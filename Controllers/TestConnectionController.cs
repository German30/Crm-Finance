using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Data;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TestConnectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestConnectionController> _logger;

        public TestConnectionController(ApplicationDbContext context, ILogger<TestConnectionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            _logger.LogInformation("Iniciando prueba de conexion a MySQL");

            try
            {
                bool canConnct = await _context.Database.CanConnectAsync();
                if (!canConnct)
                {
                    return StatusCode(500, new
                    {
                        Success = false,
                        Message = "No se puede establecer comunicacion con el servidor de MySQL"
                    });
                }

                using var command = _context.Database.GetDbConnection().CreateCommand();
                // DB_NAME() es de T-SQL; en MySQL el equivalente es DATABASE().
                command.CommandText = "SELECT VERSION() AS Version, DATABASE() AS DbName;";

                await _context.Database.OpenConnectionAsync();
                using var reader = await command.ExecuteReaderAsync();

                string dbVersion = "Desconocida";
                string databaseName = "Desconocida";

                if (await reader.ReadAsync())
                {
                    dbVersion = reader["Version"].ToString() ?? "Desconocida";
                    databaseName = reader["DbName"].ToString() ?? "Desconocida";
                }
                await _context.Database.CloseConnectionAsync();

                _logger.LogInformation("Conexion a la base de datos exitosa!");

                return Ok(new
                {
                    Success = true,
                    Message = "Conexion establecida con MySQL",
                    TimeSpan = DateTime.Now,
                    Details = new
                    {
                        Database = databaseName,
                        version = dbVersion
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error critico al intentar conectar a la base de datos");

                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error al conectar a la base de datos",
                    ErrorDetails = ex.Message,
                    InnerError = ex.InnerException?.Message
                });
            }
        }
    }
}
