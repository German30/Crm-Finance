using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRMFinaciertoBackend.DTOs.Oportunity;
using CRMFinaciertoBackend.Services;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OportunityController : ControllerBase
    {
        private readonly IOportunityService _oportunityService;

        public OportunityController(IOportunityService oportunityService)
        {
            _oportunityService = oportunityService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OportunityResponseDto>>> GetOportunities(
            [FromQuery] int? clientId,
            [FromQuery] int? userId,
            [FromQuery] int? stageId,
            [FromQuery] int? areaId)
        {
            var oportunities = await _oportunityService.GetAllOportunitiesAsync(clientId, userId, stageId, areaId);
            return Ok(oportunities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OportunityResponseDto>> GetOportunity(int id)
        {
            var oportunity = await _oportunityService.GetOportunityByIdAsync(id);
            if (oportunity == null) return NotFound(new { Message = "Oportunidad no encontrada" });
            return Ok(oportunity);
        }

        [HttpPost]
        public async Task<ActionResult<OportunityResponseDto>> CreateOportunity([FromBody] OportunityCreateDto dto)
        {
            try
            {
                var created = await _oportunityService.CreateOportunityAsync(dto);
                return CreatedAtAction(nameof(GetOportunity), new { id = created.OportunityId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<OportunityResponseDto>> UpdateOportunity(int id, [FromBody] OportunityUpdateDto dto)
        {
            try
            {
                var updated = await _oportunityService.UpdateOportunityAsync(id, dto);
                if (updated == null) return NotFound(new { Message = "Oportunidad no encontrada" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPatch("{id}/stage")]
        public async Task<ActionResult<OportunityResponseDto>> ChangeStage(int id, [FromBody] OportunityStageUpdateDto dto)
        {
            try
            {
                var updated = await _oportunityService.ChangeStageAsync(id, dto);
                if (updated == null) return NotFound(new { Message = "Oportunidad no encontrada" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOportunity(int id)
        {
            var deleted = await _oportunityService.DeleteOportunityAsync(id);
            if (!deleted) return NotFound(new { Message = "Oportunidad no encontrada" });
            return Ok(new { Message = "Oportunidad eliminada correctamente" });
        }
    }
}
