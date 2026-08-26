using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRMFinaciertoBackend.Services;

namespace CRMFinaciertoBackend.Controllers
{
    /// <summary>
    /// Catalogos de solo lectura para los combos del frontend.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _catalogService;

        public CatalogController(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas() => Ok(await _catalogService.GetAreasAsync());

        [HttpGet("type-persons")]
        public async Task<IActionResult> GetTypePersons() => Ok(await _catalogService.GetTypePersonsAsync());

        [HttpGet("genders")]
        public async Task<IActionResult> GetGenders() => Ok(await _catalogService.GetGendersAsync());

        [HttpGet("civil-states")]
        public async Task<IActionResult> GetCivilStates() => Ok(await _catalogService.GetCivilStatesAsync());

        [HttpGet("product-status")]
        public async Task<IActionResult> GetProductStatuses() => Ok(await _catalogService.GetProductStatusesAsync());

        [HttpGet("contract-status")]
        public async Task<IActionResult> GetContractStatuses() => Ok(await _catalogService.GetContractStatusesAsync());

        [HttpGet("pay-forms")]
        public async Task<IActionResult> GetPayForms() => Ok(await _catalogService.GetPayFormsAsync());

        [HttpGet("stages")]
        public async Task<IActionResult> GetStages() => Ok(await _catalogService.GetStagesAsync());

        [HttpGet("transaction-types")]
        public async Task<IActionResult> GetTransactionTypes() => Ok(await _catalogService.GetTransactionTypesAsync());

        [HttpGet("disaster-states")]
        public async Task<IActionResult> GetDisasterStates() => Ok(await _catalogService.GetDisasterStatesAsync());
    }
}
