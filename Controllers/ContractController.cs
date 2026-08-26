using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRMFinaciertoBackend.DTOs.Contract;
using CRMFinaciertoBackend.Services;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractGridDto>>> GetContracts(
            [FromQuery] int? areaId,
            [FromQuery] int? clientId,
            [FromQuery] int? userId,
            [FromQuery] int? contractStatusId,
            [FromQuery] string? search)
        {
            var contracts = await _contractService.GetAllContractsAsync(areaId, clientId, userId, contractStatusId, search);
            return Ok(contracts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContract(int id)
        {
            var contract = await _contractService.GetContractDetailAsync(id);
            if (contract == null) return NotFound(new { Message = "Contrato no encontrado" });
            return Ok(contract);
        }

        [HttpGet("bank/{id}")]
        [Authorize(Policy = "BancaOAdministrador")]
        public async Task<ActionResult<BankContractDetailDto>> GetBankContract(int id)
        {
            var contract = await _contractService.GetBankContractAsync(id);
            if (contract == null) return NotFound(new { Message = "Contrato bancario no encontrado" });
            return Ok(contract);
        }

        [HttpGet("insurance/{id}")]
        [Authorize(Policy = "SegurosOAdministrador")]
        public async Task<ActionResult<InsuranceContractDetailDto>> GetInsuranceContract(int id)
        {
            var contract = await _contractService.GetInsuranceContractAsync(id);
            if (contract == null) return NotFound(new { Message = "Contrato de seguro no encontrado" });
            return Ok(contract);
        }

        [HttpPost("bank")]
        [Authorize(Policy = "BancaOAdministrador")]
        public async Task<ActionResult<BankContractDetailDto>> CreateBankContract([FromBody] BankContractCrateDto dto)
        {
            try
            {
                var created = await _contractService.CreateBankContractAsync(dto);
                return CreatedAtAction(nameof(GetBankContract), new { id = created.ContractId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("insurance")]
        [Authorize(Policy = "SegurosOAdministrador")]
        public async Task<ActionResult<InsuranceContractDetailDto>> CreateInsuranceContract([FromBody] InsuranceContranctCreateDto dto)
        {
            try
            {
                var created = await _contractService.CreateInsuranceContractAsync(dto);
                return CreatedAtAction(nameof(GetInsuranceContract), new { id = created.ContractId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("bank/{id}")]
        [Authorize(Policy = "BancaOAdministrador")]
        public async Task<ActionResult<BankContractDetailDto>> UpdateBankContract(int id, [FromBody] BankContractUpdateDto dto)
        {
            try
            {
                var updated = await _contractService.UpdateBankContractAsync(id, dto);
                if (updated == null) return NotFound(new { Message = "Contrato bancario no encontrado" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("insurance/{id}")]
        [Authorize(Policy = "SegurosOAdministrador")]
        public async Task<ActionResult<InsuranceContractDetailDto>> UpdateInsuranceContract(int id, [FromBody] InsuranceContractUpdateDto dto)
        {
            try
            {
                var updated = await _contractService.UpdateInsuranceContractAsync(id, dto);
                if (updated == null) return NotFound(new { Message = "Contrato de seguro no encontrado" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ContractStatusUpdateDto dto)
        {
            try
            {
                var changed = await _contractService.ChangeContractStatusAsync(id, dto.ContractStatusId);
                if (!changed) return NotFound(new { Message = "Contrato no encontrado" });
                return Ok(new { Message = "Estado del contrato actualizado correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<IActionResult> DeleteContract(int id)
        {
            try
            {
                var deleted = await _contractService.DeleteContractAsync(id);
                if (!deleted) return NotFound(new { Message = "Contrato no encontrado" });
                return Ok(new { Message = "Contrato eliminado correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
