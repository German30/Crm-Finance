using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CRMFinaciertoBackend.Data;

namespace CRMFinaciertoBackend.Services
{
    public class AuthService : IAuthService
    {
        public const int DefaultExpirationInMinutes = 400;

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IPasswordHasher passwordHasher,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<LoginResult?> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r!.Area)
                .Include(u => u.Status)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.Status?.StatusName != "Activo")
                return null;

            var verification = _passwordHasher.Verify(user.PasswordHash, password);

            if (verification == PasswordVerificationResult.Failed)
                return null;

            // Un usuario sin rol o sin area asociada es un dato inconsistente: se rechaza
            // el login en vez de reventar con NullReferenceException (500). Se comprueba antes
            // de regrabar el hash para no escribir en la BD en un login que va a fallar.
            if (user.Role?.Area == null)
            {
                _logger.LogError(
                    "El usuario {UserId} tiene un rol o area mal configurados y no puede iniciar sesion.",
                    user.UserId);
                return null;
            }

            // Migracion transparente: las contrasenas heredadas (texto plano) se regraban
            // hasheadas la primera vez que el usuario inicia sesion correctamente.
            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.Hash(password);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Se actualizo el hash de la contrasena del usuario {UserId} al formato vigente.",
                    user.UserId);
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");

            var secretKey = jwtSettings.GetValue<string>("SecretKey")
                ?? throw new InvalidOperationException("La clave JWT no esta configurada");

            var expirationInMinutes = jwtSettings.GetValue<int?>("ExpirationInMinutes")
                ?? DefaultExpirationInMinutes;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim("Area", user.Role.Area.AreaName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = jwtSettings.GetValue<string>("Issuer"),
                Audience = jwtSettings.GetValue<string>("Audience"),
                Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new LoginResult(tokenHandler.WriteToken(token), expirationInMinutes);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (_passwordHasher.Verify(user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("La contrasena actual no es correcta.");

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("El usuario {UserId} cambio su contrasena.", userId);
            return true;
        }
    }
}
