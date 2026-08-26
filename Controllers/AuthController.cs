using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRMFinaciertoBackend.DTOs.User;
using CRMFinaciertoBackend.Services;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (result == null)
                return Unauthorized(new { Message = "Credenciales incorrectas o usuario inactivo" });

            return Ok(new { Token = result.Token, ExpirationInMinutes = result.ExpirationInMinutes });
        }

        /// <summary>
        /// Devuelve la identidad tal como viaja en el token, sin pegarle a la BD: es lo que
        /// el frontend necesita para pintar el menu por rol y por area.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public ActionResult<CurrentUserDto> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Message = "El token no contiene un usuario valido" });

            return Ok(new CurrentUserDto(
                userId.Value,
                User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                User.FindFirstValue("Area") ?? string.Empty
            ));
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { Message = "El token no contiene un usuario valido" });

            try
            {
                var changed = await _authService.ChangePasswordAsync(userId.Value, dto.CurrentPassword, dto.NewPassword);
                if (!changed) return NotFound(new { Message = "Usuario no encontrado" });
                return Ok(new { Message = "Contrasena actualizada correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var userId) ? userId : null;
        }
    }

    public record LoginRequest(string Email, string Password);
}
