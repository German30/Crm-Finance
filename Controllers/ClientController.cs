using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRMFinaciertoBackend.DTOs.Client;
using CRMFinaciertoBackend.Services;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientGridDto>>> GetClients(
            [FromQuery] string? search,
            [FromQuery] int? typePersonId,
            [FromQuery] int? assignedUserId)
        {
            var clients = await _clientService.GetAllClientsAsync(search, typePersonId, assignedUserId);
            return Ok(clients);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClient(int id)
        {
            var client = await _clientService.GetClientDetailAsync(id);
            if (client == null) return NotFound(new { Message = "Cliente no encontrado" });
            return Ok(client);
        }

        [HttpGet("phisic/{id}")]
        public async Task<ActionResult<PhisicClientDetailDto>> GetPhisicClient(int id)
        {
            var client = await _clientService.GetPhisicClientAsync(id);
            if (client == null) return NotFound(new { Message = "Cliente de persona fisica no encontrado" });
            return Ok(client);
        }

        [HttpGet("moral/{id}")]
        public async Task<ActionResult<MoralClientDetailDto>> GetMoralClient(int id)
        {
            var client = await _clientService.GetMoralClientAsync(id);
            if (client == null) return NotFound(new { Message = "Cliente de persona moral no encontrado" });
            return Ok(client);
        }

        [HttpPost("phisic")]
        public async Task<ActionResult<PhisicClientDetailDto>> CreatePhisicClient([FromBody] PhisicClientCreateDto dto)
        {
            try
            {
                var created = await _clientService.CreatePhisicClientAsync(dto);
                return CreatedAtAction(nameof(GetPhisicClient), new { id = created.ClientId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("moral")]
        public async Task<ActionResult<MoralClientDetailDto>> CreateMoralClient([FromBody] MoralClientCreateDto dto)
        {
            try
            {
                var created = await _clientService.CreateMoralClientAsync(dto);
                return CreatedAtAction(nameof(GetMoralClient), new { id = created.ClientId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("phisic/{id}")]
        public async Task<ActionResult<PhisicClientDetailDto>> UpdatePhisicClient(int id, [FromBody] PhisicClientUpdateDto dto)
        {
            try
            {
                var updated = await _clientService.UpdatePhisicClientAsync(id, dto);
                if (updated == null) return NotFound(new { Message = "Cliente de persona fisica no encontrado" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("moral/{id}")]
        public async Task<ActionResult<MoralClientDetailDto>> UpdateMoralClient(int id, [FromBody] MoralClientUpdateDto dto)
        {
            try
            {
                var updated = await _clientService.UpdateMoralClientAsync(id, dto);
                if (updated == null) return NotFound(new { Message = "Cliente de persona moral no encontrado" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            try
            {
                var deleted = await _clientService.DeleteClientAsync(id);
                if (!deleted) return NotFound(new { Message = "Cliente no encontrado" });
                return Ok(new { Message = "Cliente eliminado correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
