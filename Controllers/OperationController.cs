using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRMFinaciertoBackend.DTOs.Operation;
using CRMFinaciertoBackend.Services;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OperationController : ControllerBase
    {
        private readonly IOperationService _operationService;

        public OperationController(IOperationService operationService)
        {
            _operationService = operationService;
        }

        // ----- Banca: transacciones -----

        [HttpGet("transactions/contract/{contractId}")]
        [Authorize(Policy = "BancaOAdministrador")]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactions(int contractId)
        {
            var transactions = await _operationService.GetTransactionsByContractAsync(contractId);
            return Ok(transactions);
        }

        [HttpGet("transactions/{transactionId}")]
        [Authorize(Policy = "BancaOAdministrador")]
        public async Task<ActionResult<TransactionResponseDto>> GetTransaction(int transactionId)
        {
            var transaction = await _operationService.GetTransactionByIdAsync(transactionId);
            if (transaction == null) return NotFound(new { Message = "Transaccion no encontrada" });
            return Ok(transaction);
        }

        [HttpPost("transactions")]
        [Authorize(Policy = "BancaOAdministrador")]
        public async Task<ActionResult<TransactionResponseDto>> CreateTransaction([FromBody] TransactionCreateDto dto)
        {
            try
            {
                var created = await _operationService.CreateTransactionAsync(dto);
                return CreatedAtAction(nameof(GetTransaction), new { transactionId = created.TransactionId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("transactions/{transactionId}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<IActionResult> DeleteTransaction(int transactionId)
        {
            var deleted = await _operationService.DeleteTransactionAsync(transactionId);
            if (!deleted) return NotFound(new { Message = "Transaccion no encontrada" });
            return Ok(new { Message = "Transaccion eliminada y balance revertido correctamente" });
        }

        // ----- Seguros: siniestros -----

        [HttpGet("claims/contract/{contractId}")]
        [Authorize(Policy = "SegurosOAdministrador")]
        public async Task<ActionResult<IEnumerable<InsuranceClaiResponseDto>>> GetClaims(int contractId)
        {
            var claims = await _operationService.GetClaimsByContractAsync(contractId);
            return Ok(claims);
        }

        [HttpGet("claims/{insuranceId}")]
        [Authorize(Policy = "SegurosOAdministrador")]
        public async Task<ActionResult<InsuranceClaiResponseDto>> GetClaim(int insuranceId)
        {
            var claim = await _operationService.GetClaimByIdAsync(insuranceId);
            if (claim == null) return NotFound(new { Message = "Siniestro no encontrado" });
            return Ok(claim);
        }

        [HttpPost("claims")]
        [Authorize(Policy = "SegurosOAdministrador")]
        public async Task<ActionResult<InsuranceClaiResponseDto>> CreateClaim([FromBody] InsuranceClaimCreateDto dto)
        {
            try
            {
                var created = await _operationService.CreateClaimAsync(dto);
                return CreatedAtAction(nameof(GetClaim), new { insuranceId = created.InsuranceId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("claims/{insuranceId}")]
        [Authorize(Policy = "SegurosOAdministrador")]
        public async Task<ActionResult<InsuranceClaiResponseDto>> UpdateClaim(int insuranceId, [FromBody] InsuranceClaimUpdateDto dto)
        {
            try
            {
                var updated = await _operationService.UpdateClaimAsync(insuranceId, dto);
                if (updated == null) return NotFound(new { Message = "Siniestro no encontrado" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("claims/{insuranceId}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<IActionResult> DeleteClaim(int insuranceId)
        {
            var deleted = await _operationService.DeleteClaimAsync(insuranceId);
            if (!deleted) return NotFound(new { Message = "Siniestro no encontrado" });
            return Ok(new { Message = "Siniestro eliminado correctamente" });
        }
    }
}
