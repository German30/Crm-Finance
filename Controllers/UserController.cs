using CRMFinaciertoBackend.DTOs.User;
using CRMFinaciertoBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // "RequiereAdministrador" es una POLITICA (Program.cs), no un rol, por lo que la forma
    // correcta es [Authorize(Policy = "...")]. Con Roles = "..." nunca autoriza.
    //
    // La politica se aplica accion por accion y no en el controlador a proposito: los
    // atributos [Authorize] de controlador y de accion se ACUMULAN, no se sustituyen, asi
    // que un [Authorize] suelto en la accion no relajaria la politica del controlador
    // (solo [AllowAnonymous] la sobreescribe, y aqui si queremos exigir token).
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<ActionResult<UserResponseDto>> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] UserCreateDto dto)
        {
            try
            {
                var createdUser = await _userService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUser), new { id = createdUser.UserId }, createdUser);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                var updatedUser = await _userService.UpdateUserAsync(id, dto);
                if (updatedUser == null) return NotFound(new { Message = "Usuario no encontrado" });
                return Ok(updatedUser);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Los combos de alta de usuario los consume cualquier usuario autenticado, no solo
        // el administrador: les basta con el [Authorize] del controlador.
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _userService.GetRoles();
            return Ok(roles);
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas()
        {
            var areas = await _userService.GetAreas();
            return Ok(areas);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetUsersStatus()
        {
            var status = await _userService.GetStatuses();
            return Ok(status);
        }

        [HttpPatch("{id}/toggle-status")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _userService.ToggleUserStatusAsync(id);
            if (!result) return NotFound(new { Message = "Usuario no encontrado" });
            return Ok(new { Message = "Estado del usuario actualizado correctamente" });
        }

        /// <summary>
        /// Restablece la contrasena de un usuario sin pedir la anterior (accion de administrador).
        /// El usuario cambia la suya propia desde POST /api/Auth/change-password.
        /// </summary>
        [HttpPatch("{id}/reset-password")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] UserResetPasswordDto dto)
        {
            var result = await _userService.ResetPasswordAsync(id, dto.NewPassword);
            if (!result) return NotFound(new { Message = "Usuario no encontrado" });
            return Ok(new { Message = "Contrasena restablecida correctamente" });
        }
    }
}
